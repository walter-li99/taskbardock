using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using static TaskbarDock.Native.Win32;

namespace TaskbarDock.Native;

/// <summary>任务栏几何信息与「按标题定位任务栏按钮」。</summary>
public static class Taskbar
{
    /// <summary>返回任务栏矩形（物理像素）与所在屏幕边缘。</summary>
    public static (RECT rect, int edge) GetRect()
    {
        var hwnd = FindWindow("Shell_TrayWnd", null);
        if (hwnd != IntPtr.Zero)
        {
            var abd = new APPBARDATA
            {
                cbSize = Marshal.SizeOf<APPBARDATA>(),
                hWnd = hwnd
            };
            SHAppBarMessage(ABM_GETTASKBARPOS, ref abd);
            if (abd.rc.Width > 0 && abd.rc.Height > 0)
                return (abd.rc, (int)abd.uEdge);

            if (GetWindowRect(hwnd, out var r) && r.Width > 0)
            {
                int edge = ABE_BOTTOM;
                var wa = SystemParameters.WorkArea;
                var pr = SystemParameters.PrimaryScreenWidth;
                if (r.top > 0) edge = ABE_BOTTOM;
                else if (r.left > 0) edge = ABE_RIGHT;
                else if (r.Width < SystemParameters.PrimaryScreenWidth - 1) edge = ABE_LEFT;
                _ = wa; _ = pr;
                return (r, edge);
            }
        }

        var s = new RECT
        {
            left = 0,
            top = (int)Math.Round(SystemParameters.PrimaryScreenHeight) - 48,
            right = (int)Math.Round(SystemParameters.PrimaryScreenWidth),
            bottom = (int)Math.Round(SystemParameters.PrimaryScreenHeight)
        };
        return (s, ABE_BOTTOM);
    }

    /// <summary>
    /// 用 UI 自动化找到任务栏按钮。优先按 AppUserModelID 匹配（Win11 的 AutomationId 形如
    /// "Appid: TaskbarDock.Group.xxx"），其次按窗口标题匹配。返回屏幕矩形（物理像素）。
    /// </summary>
    public static Rect? FindButton(string appId, string title = null)
    {
        if (string.IsNullOrEmpty(appId) && string.IsNullOrEmpty(title)) return null;
        AutomationElement tray = null;
        try
        {
            var root = AutomationElement.RootElement;
            tray = root.FindFirst(TreeScope.Children,
                new PropertyCondition(AutomationElement.ClassNameProperty, "Shell_TrayWnd"));
        }
        catch (Exception ex)
        {
            Services.ConfigService.Log("定位任务栏失败: " + ex.Message);
            return null;
        }
        if (tray == null) return null;

        try
        {
            if (!string.IsNullOrEmpty(appId))
            {
                var byId = tray.FindFirst(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.AutomationIdProperty, "Appid: " + appId));
                if (byId != null)
                {
                    var r = byId.Current.BoundingRectangle;
                    if (r.Width > 1 && r.Height > 1) return r;
                }
            }

            var all = tray.FindAll(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
            foreach (AutomationElement el in all)
            {
                string name = null, id = null;
                try { name = el.Current.Name; } catch { }
                try { id = el.Current.AutomationId; } catch { }
                bool hit = (!string.IsNullOrEmpty(appId) && id != null && id.Contains(appId))
                           || (!string.IsNullOrEmpty(title) && name != null
                               && (name.Equals(title, StringComparison.OrdinalIgnoreCase) || name.Contains(title)));
                if (!hit) continue;
                var r = el.Current.BoundingRectangle;
                if (r.Width > 1 && r.Height > 1) return r;
            }
        }
        catch (Exception ex)
        {
            Services.ConfigService.Log("定位任务栏按钮失败: " + ex.Message);
        }
        return null;
    }

    /// <summary>当前鼠标位置（WPF 逻辑像素）。</summary>
    public static Point Cursor()
    {
        GetCursorPos(out var pt);
        return new Point(pt.x, pt.y);
    }
}
