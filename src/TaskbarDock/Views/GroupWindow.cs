using System;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using TaskbarDock.Models;
using TaskbarDock.Native;
using TaskbarDock.Services;
using static TaskbarDock.Native.Win32;

namespace TaskbarDock.Views;

/// <summary>
/// 每个分组对应一个常驻最小化窗口 —— 它就是一个标准的任务栏按钮：
/// 可以固定到任务栏 / 取消固定，右键有和其它程序一样的菜单，点击展开快捷方式菜单。
/// </summary>
public class GroupWindow : Window
{
    public DockGroup Group { get; }

    private IntPtr _hwnd = IntPtr.Zero;
    private HwndSource _src;
    private MenuWindow _menu;
    private IntPtr _iconSmall = IntPtr.Zero, _iconBig = IntPtr.Zero;
    private bool _suppress;
    private bool _closed;
    private DateTimeOffset _lastClose = DateTimeOffset.MinValue;
    private const int CloseCooldownMs = 350;
    public string AppId => "TaskbarDock.Group." + Group.Id;

    public GroupWindow(DockGroup group)
    {
        Group = group;
        Title = group.Name;
        Width = 2;
        Height = 2;
        Left = -32000;
        Top = -32000;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = true;
        ShowActivated = false;
        Background = Brushes.Transparent;
        Content = new System.Windows.Controls.Grid();

        SourceInitialized += OnSourceInitialized;
        StateChanged += OnStateChanged;
        Closing += OnClosing;
    }

    // ===== 初始化 =====
    private void OnSourceInitialized(object sender, EventArgs e)
    {
        _hwnd = new WindowInteropHelper(this).Handle;
        _src = HwndSource.FromHwnd(_hwnd);
        _src.AddHook(WndProc);

        int ex = GetWindowLong(_hwnd, GWL_EXSTYLE);
        ex |= WS_EX_APPWINDOW;
        ex &= ~WS_EX_TOOLWINDOW;
        SetWindowLong(_hwnd, GWL_EXSTYLE, ex);
        SetWindowPos(_hwnd, IntPtr.Zero, -32000, -32000, 2, 2,
            SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);

        ApplyAppId();
        UpdateIcon();
        ThemeService.Changed += OnThemeChanged;
        ConfigService.Log("任务栏按钮已创建: " + Title + " (" + AppId + ")");
    }

    private void OnThemeChanged()
    {
        Dispatcher.Invoke(() => { if (!_closed) UpdateIcon(); });
    }

    private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _closed = true;
        ThemeService.Changed -= OnThemeChanged;
        _src?.RemoveHook(WndProc);
        _src = null;
        CloseMenu();
        if (_iconSmall != IntPtr.Zero) { DestroyIcon(_iconSmall); _iconSmall = IntPtr.Zero; }
        if (_iconBig != IntPtr.Zero) { DestroyIcon(_iconBig); _iconBig = IntPtr.Zero; }
    }

    /// <summary>刷新标题 / 图标 / 启动命令（设置改动后调用）。</summary>
    public void Refresh()
    {
        if (_closed) return;
        Title = Group.Name;
        ApplyAppId();
        UpdateIcon();
    }

    private void ApplyAppId()
    {
        if (_hwnd == IntPtr.Zero) return;
        WindowProperties.Set(_hwnd, WindowProperties.Id, AppId);
        var exe = Environment.ProcessPath ?? "";
        var id = Group.Id;
        WindowProperties.Set(_hwnd, WindowProperties.RelaunchCommand,
            "\"" + exe + "\" --menu " + id);
    }

    public void UpdateIcon()
    {
        if (_hwnd == IntPtr.Zero || _closed) return;
        var fg = ThemeService.IsLight
            ? Color.FromRgb(0x1B, 0x1B, 0x1B)
            : Color.FromRgb(0xF3, 0xF3, 0xF3);

        var small = IconHelper.Build(Group.IconPack, Group.IconKey, Group.CustomIconPath, fg, 32);
        var big = IconHelper.Build(Group.IconPack, Group.IconKey, Group.CustomIconPath, fg, 48);
        if (small == IntPtr.Zero && big == IntPtr.Zero) return;

        if (small != IntPtr.Zero)
        {
            SendMessage(_hwnd, WM_SETICON, (IntPtr)ICON_SMALL, small);
            if (_iconSmall != IntPtr.Zero) DestroyIcon(_iconSmall);
            _iconSmall = small;
        }
        if (big != IntPtr.Zero)
        {
            SendMessage(_hwnd, WM_SETICON, (IntPtr)ICON_BIG, big);
            if (_iconBig != IntPtr.Zero) DestroyIcon(_iconBig);
            _iconBig = big;
        }
    }

    // ===== 消息：把「点击任务栏按钮」转成弹出菜单 =====
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (App.CmdMessage != 0 && msg == App.CmdMessage)
        {
            handled = true;
            int cmd = (int)wParam;
            Dispatcher.BeginInvoke(new Action(() => App.Instance?.HandleCommand(cmd, null)));
            return IntPtr.Zero;
        }

        if (msg == WM_SYSCOMMAND)
        {
            int cmd = (int)wParam & 0xFFF0;
            if (cmd == SC_RESTORE || cmd == SC_MAXIMIZE || cmd == SC_MOVE)
            {
                handled = true;
                Dispatcher.BeginInvoke(new Action(ToggleMenu));
                return IntPtr.Zero;
            }
            if (cmd == SC_CLOSE)
            {
                handled = true;
                Dispatcher.BeginInvoke(new Action(() => App.Instance?.ShutdownApp()));
                return IntPtr.Zero;
            }
        }
        else if (msg == WM_SETTINGCHANGE)
        {
            Dispatcher.BeginInvoke(new Action(ThemeService.Refresh));
        }
        else if (msg == WM_DPICHANGED || msg == WM_DISPLAYCHANGE)
        {
            Dispatcher.BeginInvoke(new Action(UpdateIcon));
        }
        return IntPtr.Zero;
    }

    private void OnStateChanged(object sender, EventArgs e)
    {
        if (_suppress || _closed) return;
        if (WindowState != WindowState.Minimized)
        {
            _suppress = true;
            try { WindowState = WindowState.Minimized; } finally { _suppress = false; }
            Dispatcher.BeginInvoke(new Action(ToggleMenu));
        }
    }

    // ===== 菜单 =====
    public void ToggleMenu()
    {
        if (_menu != null) { CloseMenu(); return; }
        if ((DateTimeOffset.UtcNow - _lastClose).TotalMilliseconds < CloseCooldownMs) return;
        OpenMenu();
    }

    public void OpenMenu()
    {
        if (_closed) return;
        var items = ShortcutService.Scan(Group.Folder, Group);
        if (items.Count == 0)
        {
            ConfigService.Log("分组「" + Group.Name + "」没有可用快捷方式: " + Group.Folder);
            App.Instance?.OpenSettings();
            return;
        }

        double scale = 1.0;
        try { var d = GetDpiForWindow(_hwnd); if (d > 0) scale = d / 96.0; } catch { }

        var (tb, edge) = Taskbar.GetRect();
        var tbDip = new Rect(tb.left / scale, tb.top / scale, tb.Width / scale, tb.Height / scale);

        Rect? anchor = null;
        var btn = Taskbar.FindButton(AppId, Title);
        if (btn.HasValue)
            anchor = new Rect(btn.Value.X / scale, btn.Value.Y / scale,
                              btn.Value.Width / scale, btn.Value.Height / scale);

        var cur = Taskbar.Cursor();
        var cursorDip = new Point(cur.X / scale, cur.Y / scale);

        _menu = new MenuWindow(Group, items, () => App.Instance?.RefreshAll());
        _menu.Anchor(anchor, edge, cursorDip, tbDip);
        _menu.Closed += (s, a) =>
        {
            _menu = null;
            _lastClose = DateTimeOffset.UtcNow;
        };
        _menu.Show();
    }

    public void CloseMenu()
    {
        if (_menu != null)
        {
            var m = _menu;
            _menu = null;
            m.Close();
        }
    }

    public bool IsMenuOpen => _menu != null;
}
