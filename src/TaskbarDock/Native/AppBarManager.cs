using System;
using System.Runtime.InteropServices;
using System.Windows;
using static TaskbarDock.Native.Win32;

namespace TaskbarDock.Native;

/// <summary>把窗口注册为 Windows AppBar，使其紧贴任务栏并让系统为其保留空间。</summary>
public class AppBarManager : IDisposable
{
    private IntPtr _hwnd;
    private uint _callback;
    private bool _registered;

    public int Edge { get; set; } = ABE_BOTTOM;
    public uint CallbackMessage => _callback;

    public void Register(IntPtr hwnd, int edge)
    {
        if (_registered) return;
        _hwnd = hwnd;
        Edge = edge;
        _callback = RegisterWindowMessage("TaskbarDockAppBarMsg");

        var abd = new APPBARDATA
        {
            cbSize = Marshal.SizeOf<APPBARDATA>(),
            hWnd = hwnd,
            uCallbackMessage = _callback
        };
        SHAppBarMessage(ABM_NEW, ref abd);
        _registered = true;
    }

    public void Unregister()
    {
        if (!_registered) return;
        var abd = new APPBARDATA { cbSize = Marshal.SizeOf<APPBARDATA>(), hWnd = _hwnd };
        SHAppBarMessage(ABM_REMOVE, ref abd);
        _registered = false;
    }

    /// <summary>提交期望位置，返回系统核准后的最终矩形（设备像素）。</summary>
    public RECT SetPos(RECT desired)
    {
        var abd = new APPBARDATA
        {
            cbSize = Marshal.SizeOf<APPBARDATA>(),
            hWnd = _hwnd,
            uEdge = (uint)Edge,
            rc = desired
        };
        SHAppBarMessage(ABM_QUERYPOS, ref abd);

        int h = desired.bottom - desired.top;
        int w = desired.right - desired.left;
        switch (Edge)
        {
            case ABE_BOTTOM: abd.rc.top = abd.rc.bottom - h; break;
            case ABE_TOP: abd.rc.bottom = abd.rc.top + h; break;
            case ABE_LEFT: abd.rc.right = abd.rc.left + w; break;
            case ABE_RIGHT: abd.rc.left = abd.rc.right - w; break;
        }

        SHAppBarMessage(ABM_SETPOS, ref abd);

        SetWindowPos(_hwnd, HWND_TOPMOST, abd.rc.left, abd.rc.top,
            abd.rc.right - abd.rc.left, abd.rc.bottom - abd.rc.top,
            SWP_NOACTIVATE | SWP_SHOWWINDOW);

        return abd.rc;
    }

    public static (RECT rc, int edge) GetTaskbar()
    {
        var abd = new APPBARDATA { cbSize = Marshal.SizeOf<APPBARDATA>() };
        var r = SHAppBarMessage(ABM_GETTASKBARPOS, ref abd);
        if (r == IntPtr.Zero)
        {
            // 兜底：Explorer 的任务栏窗口
            var h = FindWindow("Shell_TrayWnd", null);
            if (h != IntPtr.Zero && GetWindowRect(h, out var rect))
                return (rect, ABE_BOTTOM);
            int sw = (int)System.Windows.SystemParameters.PrimaryScreenWidth;
            int sh = (int)System.Windows.SystemParameters.PrimaryScreenHeight;
            return (new RECT { left = 0, top = sh - 48, right = sw, bottom = sh }, ABE_BOTTOM);
        }
        return (abd.rc, (int)abd.uEdge);
    }

    public static RECT GetMonitorRect(IntPtr hwnd)
    {
        var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        var h = MonitorFromWindow(hwnd, MONITOR_DEFAULTTOPRIMARY);
        if (h != IntPtr.Zero && GetMonitorInfo(h, ref mi)) return mi.rcMonitor;
        return new RECT
        {
            left = 0, top = 0,
            right = (int)SystemParameters.PrimaryScreenWidth,
            bottom = (int)SystemParameters.PrimaryScreenHeight
        };
    }

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    public void Dispose() => Unregister();
}
