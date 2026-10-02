using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using TaskbarDock.Services;
using static TaskbarDock.Native.Win32;

namespace TaskbarDock.Views;

/// <summary>
/// 隐藏的「命令接收窗口」。第二个进程实例通过它把（命令 + 分组 Id）精确交给已运行的主实例。
///
/// 为什么需要它：HWND_BROADCAST 只能带两个整数参数，分组 Id（GUID 字符串）传不过去，
/// 于是点击第二个任务栏图标时主实例不知道该弹哪个分组，只能退化成弹第一个分组的菜单。
/// 这里用 WM_COPYDATA 把 "cmd|groupId" 整串发过来，多图标才能各弹各的。
/// </summary>
public class CommandWindow : Window
{
    public const string WindowTitle = "TaskbarDock.CommandReceiver.v1";

    private HwndSource _src;
    private IntPtr _hwnd;

    public CommandWindow()
    {
        Title = WindowTitle;
        Width = 0;
        Height = 0;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        ShowActivated = false;
        Visibility = Visibility.Hidden;
        SourceInitialized += OnSourceInitialized;
    }

    /// <summary>建好 hwnd 后立刻隐藏：这个窗口只用来收消息。</summary>
    public void Start()
    {
        Show();
        Hide();
    }

    private void OnSourceInitialized(object sender, EventArgs e)
    {
        _hwnd = new WindowInteropHelper(this).Handle;
        _src = HwndSource.FromHwnd(_hwnd);
        _src.AddHook(WndProc);
        SetWindowPos(_hwnd, IntPtr.Zero, -32000, -32000, 0, 0,
            SWP_NOZORDER | SWP_NOACTIVATE | SWP_HIDEWINDOW);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_COPYDATA)
        {
            handled = true;
            try
            {
                var cds = Marshal.PtrToStructure<COPYDATASTRUCT>(lParam);
                if (cds.cbData > 0 && cds.lpData != IntPtr.Zero)
                {
                    var text = Marshal.PtrToStringUni(cds.lpData, cds.cbData / 2);
                    if (!string.IsNullOrEmpty(text))
                    {
                        text = text.TrimEnd('\0');
                        var i = text.IndexOf('|');
                        var cmd = int.TryParse(i >= 0 ? text.Substring(0, i) : text, out var c) ? c : App.CmdMenu;
                        var gid = i >= 0 ? text.Substring(i + 1) : null;
                        if (string.IsNullOrEmpty(gid)) gid = null;
                        Dispatcher.BeginInvoke(new Action(() => App.Instance?.HandleCommand(cmd, gid)));
                        return (IntPtr)1;
                    }
                }
            }
            catch (Exception ex)
            {
                ConfigService.Log("命令转发解析失败: " + ex.Message);
            }
            return IntPtr.Zero;
        }

        // 兜底：广播过来的旧式命令（没有分组 Id）
        if (App.CmdMessage != 0 && msg == App.CmdMessage)
        {
            handled = true;
            int cmd = (int)wParam;
            Dispatcher.BeginInvoke(new Action(() => App.Instance?.HandleCommand(cmd, null)));
            return IntPtr.Zero;
        }

        return IntPtr.Zero;
    }

    /// <summary>把命令发给已运行的实例。成功返回 true（对方已收到）。</summary>
    public static bool TryForward(int cmd, string groupId)
    {
        try
        {
            var hwnd = FindWindow(null, WindowTitle);
            if (hwnd == IntPtr.Zero) return false;

            var text = cmd + "|" + (groupId ?? "");
            var ptr = Marshal.StringToHGlobalUni(text);
            try
            {
                var cds = new COPYDATASTRUCT
                {
                    dwData = (IntPtr)0x5444,
                    cbData = (text.Length + 1) * 2,
                    lpData = ptr
                };
                return SendMessage(hwnd, WM_COPYDATA, IntPtr.Zero, ref cds) != IntPtr.Zero;
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
        catch (Exception ex)
        {
            Services.ConfigService.Log("命令转发失败: " + ex.Message);
            return false;
        }
    }
}
