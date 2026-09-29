using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using TaskbarDock.Models;
using TaskbarDock.Native;
using TaskbarDock.Services;
using static TaskbarDock.Native.Win32;

namespace TaskbarDock.Views;

public partial class DockWindow : Window
{
    public ObservableCollection<DockGroupVm> Groups { get; } = new();

    private readonly AppBarManager _bar = new();
    private IntPtr _hwnd = IntPtr.Zero;
    private HwndSource _src;
    private int _edge = ABE_BOTTOM;
    private MenuWindow _menu;
    private DockGroupVm _openVm;
    private readonly System.Collections.Generic.List<FileSystemWatcher> _watchers = new();
    private readonly DispatcherTimer _debounce;
    private bool _closing;

    public DockWindow()
    {
        InitializeComponent();
        DataContext = this;

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _debounce.Tick += (s, e) => { _debounce.Stop(); Rebuild(); };

        ThemeService.Changed += () => Dispatcher.Invoke(ApplyVisual);
        Rebuild();
    }

    // ===== 生命周期 =====
    private void OnSourceInitialized(object sender, EventArgs e)
    {
        _hwnd = new WindowInteropHelper(this).Handle;
        _src = HwndSource.FromHwnd(_hwnd);
        _src.AddHook(WndProc);

        int ex = GetWindowLong(_hwnd, GWL_EXSTYLE);
        ex &= ~WS_EX_APPWINDOW;
        ex |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
        SetWindowLong(_hwnd, GWL_EXSTYLE, ex);

        _edge = AppBarManager.GetTaskbar().edge;
        _bar.Register(_hwnd, _edge);

        ApplyVisual();
        UpdateSize();
        Reposition();
    }

    private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _closing = true;
        foreach (var w in _watchers) { w.EnableRaisingEvents = false; w.Dispose(); }
        _watchers.Clear();
        _menu?.Close();
        _bar.Unregister();
        _src?.RemoveHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (_bar.CallbackMessage != 0 && msg == _bar.CallbackMessage)
        {
            if ((int)wParam == ABN_POSCHANGED)
                Dispatcher.BeginInvoke(new Action(() => { UpdateSize(); Reposition(); }));
            handled = true;
        }
        else if (msg == WM_SETTINGCHANGE)
        {
            Dispatcher.BeginInvoke(new Action(() => ThemeService.Refresh()));
        }
        else if (msg == WM_DPICHANGED || msg == WM_DISPLAYCHANGE)
        {
            Dispatcher.BeginInvoke(new Action(() => { UpdateSize(); Reposition(); }));
        }
        return IntPtr.Zero;
    }

    // ===== 构建 =====
    public void Rebuild()
    {
        if (_closing) return;
        var openId = _openVm?.Id;

        foreach (var w in _watchers) { w.EnableRaisingEvents = false; w.Dispose(); }
        _watchers.Clear();

        Groups.Clear();
        foreach (var g in ConfigService.Config.Groups)
            Groups.Add(new DockGroupVm(g));

        SetupWatchers();
        UpdateSize();
        Reposition();
    }

    private void SetupWatchers()
    {
        foreach (var g in ConfigService.Config.Groups)
        {
            if (string.IsNullOrWhiteSpace(g.Folder) || !Directory.Exists(g.Folder)) continue;
            try
            {
                var w = new FileSystemWatcher(g.Folder)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName,
                    IncludeSubdirectories = false
                };
                w.Created += (s, e) => Debounced();
                w.Deleted += (s, e) => Debounced();
                w.Renamed += (s, e) => Debounced();
                w.Changed += (s, e) => Debounced();
                w.EnableRaisingEvents = true;
                _watchers.Add(w);
            }
            catch (Exception ex) { ConfigService.Log("监控目录失败 " + g.Folder + " : " + ex.Message); }
        }
    }

    private void Debounced()
    {
        Dispatcher.Invoke(() => { _debounce.Stop(); _debounce.Start(); });
    }

    // ===== 尺寸与定位 =====
    private double Scale()
    {
        try
        {
            var dpi = GetDpiForWindow(_hwnd);
            if (dpi > 0) return dpi / 96.0;
        }
        catch { }
        return 1.0;
    }

    public void UpdateSize()
    {
        var (tb, edge) = AppBarManager.GetTaskbar();
        _edge = edge;
        bool horiz = edge == ABE_BOTTOM || edge == ABE_TOP;

        double scale = Scale();
        int thicknessPx = horiz ? tb.Height : tb.Width;
        if (thicknessPx < 16 || thicknessPx > 200) thicknessPx = (int)Math.Round(48 * scale);
        double thicknessDip = thicknessPx / scale;

        double btn = Math.Max(20, Math.Min(ConfigService.Config.IconSize, thicknessDip - 4));
        foreach (var vm in Groups)
        {
            vm.ButtonSize = btn;
            vm.IconSize = Math.Max(14, btn * 0.66);
        }

        int count = Math.Max(1, Groups.Count);
        double len = count * (btn + 2) + 4;

        var panel = FindChild<StackPanel>(Bar);
        if (panel != null) panel.Orientation = horiz ? Orientation.Horizontal : Orientation.Vertical;

        if (horiz) { Width = len; Height = thicknessDip; }
        else { Width = thicknessDip; Height = len; }

        ApplyVisual();
    }

    public void Reposition()
    {
        if (_hwnd == IntPtr.Zero) return;

        var (tb, edge) = AppBarManager.GetTaskbar();
        _edge = edge;
        bool horiz = edge == ABE_BOTTOM || edge == ABE_TOP;
        double scale = Scale();

        int wPx = (int)Math.Round(Width * scale);
        int hPx = (int)Math.Round(Height * scale);
        int len = horiz ? wPx : hPx;
        int thick = horiz ? hPx : wPx;

        int longStart = horiz ? tb.left : tb.top;
        int longEnd = horiz ? tb.right : tb.bottom;

        int start = ConfigService.Config.Align switch
        {
            "Center" => (longStart + longEnd) / 2 - len / 2,
            "Right" => longEnd - len - 6,
            _ => longStart + 6
        };
        start = Math.Max(longStart + 2, Math.Min(start, Math.Max(longStart + 2, longEnd - len - 2)));

        RECT rc = edge switch
        {
            ABE_TOP => new RECT { left = start, right = start + len, top = tb.bottom, bottom = tb.bottom + thick },
            ABE_LEFT => new RECT { left = tb.right, right = tb.right + thick, top = start, bottom = start + len },
            ABE_RIGHT => new RECT { left = tb.left - thick, right = tb.left, top = start, bottom = start + len },
            _ => new RECT { left = start, right = start + len, top = tb.top - thick, bottom = tb.top }
        };

        _bar.SetPos(rc);
    }

    public void ApplyVisual()
    {
        bool acrylic = ConfigService.Config.UseAcrylic;
        Root.Background = (Brush)TryFindResource(acrylic ? "Br.TaskbarAcrylic" : "Br.Taskbar");

        if (_hwnd != IntPtr.Zero)
        {
            if (acrylic)
            {
                var c = ((SolidColorBrush)TryFindResource("Br.TaskbarAcrylic")).Color;
                SetAcrylic(_hwnd, c);
            }
            else
            {
                ClearAccent(_hwnd);
                var c = ((SolidColorBrush)TryFindResource("Br.Taskbar")).Color;
                SetAcrylic(_hwnd, Color.FromArgb(0xFF, c.R, c.G, c.B));
            }
        }
    }

    public int Edge => _edge;

    // ===== 交互 =====
    private void OnGroupClick(object sender, RoutedEventArgs e)
    {
        ConfigService.Log("OnGroupClick fired");
        var btn = (Button)sender;
        var vm = btn.Tag as DockGroupVm;
        if (vm == null) { ConfigService.Log("OnGroupClick: vm null"); return; }
        ConfigService.Log("OnGroupClick group=" + vm.Name + " open=" + (_openVm == vm));

        if (_openVm == vm && _menu != null)
        {
            CloseMenu();
            return;
        }

        CloseMenu();

        var items = ShortcutService.Scan(vm.Group.Folder, vm.Group);
        if (items.Count == 0)
        {
            var r = MessageBox.Show(
                $"「{vm.Group.Name}」里还没有可用的快捷方式。\n\n目录：{vm.Group.Folder}\n\n现在打开设置选择一个文件夹？",
                "任务栏整合", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r == MessageBoxResult.Yes) App.Instance.OpenSettings();
            return;
        }

        _menu = new MenuWindow(vm.Group, items, this);
        _menu.Anchor(btn, _edge);
        _menu.Closed += (s, a) =>
        {
            vm.IsOpen = false;
            if (_openVm == vm) _openVm = null;
            _menu = null;
        };
        _openVm = vm;
        vm.IsOpen = true;
        _menu.Show();
    }

    public void CloseMenu()
    {
        if (_menu != null)
        {
            _menu.Close();
            _menu = null;
        }
        if (_openVm != null) _openVm.IsOpen = false;
        _openVm = null;
    }

    private void OnGroupRightClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var btn = (Button)sender;
        var vm = btn.Tag as DockGroupVm;
        if (vm == null) return;

        var menu = new ContextMenu();
        void Add(string header, Action act)
        {
            var mi = new MenuItem { Header = header, FontSize = 13 };
            mi.Click += (s, a) => act();
            menu.Items.Add(mi);
        }
        Add("打开文件夹", () =>
        {
            try { Process.Start("explorer.exe", vm.Folder); } catch { }
        });
        Add("刷新", () => Rebuild());
        Add("设置…", () => App.Instance.OpenSettings());
        menu.Items.Add(new Separator());
        Add("退出", () => App.Instance.ShutdownApp());

        menu.PlacementTarget = btn;
        menu.IsOpen = true;
        e.Handled = true;
    }

    // ===== 工具 =====
    private static T FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null) return null;
        int n = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < n; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T t) return t;
            var r = FindChild<T>(child);
            if (r != null) return r;
        }
        return null;
    }
}
