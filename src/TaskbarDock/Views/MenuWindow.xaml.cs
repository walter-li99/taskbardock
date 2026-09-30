using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using TaskbarDock.Models;
using TaskbarDock.Native;
using TaskbarDock.Services;
using static TaskbarDock.Native.Win32;

namespace TaskbarDock.Views;

public partial class MenuWindow : Window
{
    public ObservableCollection<ShortcutItem> Items { get; } = new();
    public double MenuMaxHeight { get; set; } = 520;
    public int ItemIconSize { get; set; } = 20;

    private readonly DockGroup _group;
    private readonly Action _onRefresh;
    private int _edge = ABE_BOTTOM;
    private Rect? _anchorRect;
    private Point _cursor;
    private Rect _taskbar;
    private Rect _workArea;

    // 拖拽状态
    private bool _pressed, _dragging;
    private Point _start;
    private ShortcutItem _dragItem;
    private Window _ghost;

    // 外部点击关闭
    private IntPtr _hookId = IntPtr.Zero;
    private HookProc _hookProc;
    private IntPtr _hwnd = IntPtr.Zero;

    public MenuWindow(DockGroup group, List<ShortcutItem> items, Action onRefresh)
    {
        InitializeComponent();
        _group = group;
        _onRefresh = onRefresh;

        var cfg = ConfigService.Config;
        ItemIconSize = cfg.ShowItemIcons ? cfg.ItemIconSize : 0;
        MenuMaxHeight = cfg.MenuMaxHeight;

        foreach (var it in items)
        {
            it.IconsVisibility = cfg.ShowItemIcons ? Visibility.Visible : Visibility.Collapsed;
            Items.Add(it);
        }

        MaxWidth = cfg.MenuWidth;
        MaxHeight = Math.Min(cfg.MenuMaxHeight + 40, SystemParameters.WorkArea.Height - 24);
        Opacity = 0;
        // 初始放到屏幕外，等 ContentRendered 定位完成后再移回目标位置，
        // 彻底消除点击瞬间在左上角闪现的问题。
        Left = -32000;
        Top = -32000;

        DataContext = this;
        Loaded += OnLoaded;
        ContentRendered += OnContentRendered;
        Closed += OnClosed;
    }

    /// <param name="anchor">任务栏按钮矩形（逻辑像素），可为 null</param>
    /// <param name="edge">任务栏所在边缘</param>
    /// <param name="cursor">鼠标位置（逻辑像素），用于兜底定位</param>
    /// <param name="taskbar">任务栏矩形（逻辑像素）</param>
    /// <param name="workArea">任务栏所在显示器的工作区（逻辑像素）</param>
    public void Anchor(Rect? anchor, int edge, Point cursor, Rect taskbar, Rect workArea)
    {
        _anchorRect = anchor;
        _edge = edge;
        _cursor = cursor;
        _taskbar = taskbar;
        _workArea = workArea;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _hwnd = new WindowInteropHelper(this).Handle;

        try
        {
            if (ConfigService.Config.UseAcrylic)
                SetAcrylic(_hwnd, ((SolidColorBrush)TryFindResource("Br.MenuAcrylic")).Color);
        }
        catch { }

        // 注意：淡入动画放到 OnContentRendered 里、定位之后再做，
        // 否则窗口会先在默认位置（屏幕左上角）闪一下，再跳到图标上方。
        Activate();
        InstallHook();
    }

    private void OnContentRendered(object sender, EventArgs e)
    {
        ContentRendered -= OnContentRendered;
        Position();

        // 先定位、再淡入 + 滑入，彻底消除左上角闪现
        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(120));
        BeginAnimation(OpacityProperty, fade);

        var tr = new TranslateTransform();
        RenderTransform = tr;
        var slide = new DoubleAnimation(_edge == ABE_TOP ? -8 : 8, 0, TimeSpan.FromMilliseconds(160))
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        tr.BeginAnimation(TranslateTransform.YProperty, slide);
    }

    private void Position()
    {
        double mw = ActualWidth, mh = ActualHeight;
        var wa = _workArea;
        const double gap = 6;

        double left, top;
        if (_anchorRect.HasValue)
        {
            var a = _anchorRect.Value;
            switch (_edge)
            {
                case ABE_TOP:
                    left = a.Left + a.Width / 2 - mw / 2; top = a.Bottom + gap; break;
                case ABE_LEFT:
                    left = a.Right + gap; top = a.Top + a.Height / 2 - mh / 2; break;
                case ABE_RIGHT:
                    left = a.Left - mw - gap; top = a.Top + a.Height / 2 - mh / 2; break;
                default:
                    left = a.Left + a.Width / 2 - mw / 2; top = a.Top - mh - gap; break;
            }
        }
        else
        {
            // 兜底：贴着鼠标所在位置展开
            switch (_edge)
            {
                case ABE_TOP:
                    left = _cursor.X - mw / 2; top = _taskbar.Bottom + gap; break;
                case ABE_LEFT:
                    left = _taskbar.Right + gap; top = _cursor.Y - mh / 2; break;
                case ABE_RIGHT:
                    left = _taskbar.Left - mw - gap; top = _cursor.Y - mh / 2; break;
                default:
                    left = _cursor.X - mw / 2; top = _taskbar.Top - mh - gap; break;
            }
        }

        left = Math.Max(wa.Left + 4, Math.Min(left, wa.Right - mw - 4));
        top = Math.Max(wa.Top + 4, Math.Min(top, wa.Bottom - mh - 4));
        Left = left;
        Top = top;
    }

    private void OnClosed(object sender, EventArgs e)
    {
        UninstallHook();
        if (_ghost != null)
        {
            var g = _ghost;
            _ghost = null;
            try { g.Close(); } catch { }
        }
        SaveOrder();
    }

    // ===== 外部点击关闭 =====
    private void InstallHook()
    {
        _hookProc = HookCallback;
        _hookId = SetWindowsHookEx(WH_MOUSE_LL, _hookProc, IntPtr.Zero, 0);
    }

    private void UninstallHook()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == (IntPtr)WM_LBUTTONDOWN || wParam == (IntPtr)WM_RBUTTONDOWN))
        {
            try
            {
                var st = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                double scale = GetDpiForWindow(_hwnd) / 96.0;
                if (scale <= 0) scale = 1.0;
                double x = st.pt.x / scale, y = st.pt.y / scale;

                var r = new Rect(Left, Top, ActualWidth, ActualHeight);
                if (!r.Contains(x, y))
                    Dispatcher.BeginInvoke(new Action(() => Close()));
            }
            catch { }
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private void OnDeactivated(object sender, EventArgs e)
    {
        // 拖拽中的 Popup 可能导致短暂失焦，这里不做关闭，交给鼠标钩子处理
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { Close(); e.Handled = true; }
    }

    // ===== 启动 / 拖拽 =====
    private void OnDown(object sender, MouseButtonEventArgs e)
    {
        var item = ItemAt(e.GetPosition(List));
        if (item == null) return;
        _pressed = true;
        _start = e.GetPosition(List);
        _dragItem = item;
    }

    private void OnMove(object sender, MouseEventArgs e)
    {
        if (!_pressed || _dragItem == null) return;

        var pos = e.GetPosition(List);
        if (!_dragging)
        {
            if ((pos - _start).Length < 6) return;
            _dragging = true;
            _dragItem.IsDragging = true;
            List.CaptureMouse();
            ShowGhost(PointToScreen(e.GetPosition(this)));
        }
        else
        {
            MoveGhost(PointToScreen(e.GetPosition(this)));
            Reorder(e.GetPosition(List));
        }
    }

    private void OnUp(object sender, MouseButtonEventArgs e)
    {
        if (_dragging)
        {
            EndDrag();
            e.Handled = true;
            return;
        }

        _pressed = false;
        var item = ItemAt(e.GetPosition(List));
        if (item != null)
        {
            Launch(item);
            Close();
        }
    }

    private void Launch(ShortcutItem item)
    {
        try { ShortcutService.Launch(item); }
        catch (Exception ex) { ConfigService.Log("启动失败: " + ex.Message); }
    }

    private void Reorder(Point pos)
    {
        var target = ItemAt(pos);
        if (target == null || target == _dragItem) return;
        int oi = Items.IndexOf(_dragItem);
        int ni = Items.IndexOf(target);
        if (oi < 0 || ni < 0 || oi == ni) return;
        Items.Move(oi, ni);
    }

    private ShortcutItem ItemAt(Point pos)
    {
        DependencyObject d = List.InputHitTest(pos) as DependencyObject;
        while (d != null && d is not ListBoxItem)
            d = VisualTreeHelper.GetParent(d);
        return (d as ListBoxItem)?.DataContext as ShortcutItem;
    }

    private void ShowGhost(Point screenPhysical)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, IsHitTestVisible = false };
        if (_dragItem.Icon != null)
            panel.Children.Add(new Image
            {
                Source = _dragItem.Icon,
                Width = ItemIconSize > 0 ? ItemIconSize : 18,
                Height = ItemIconSize > 0 ? ItemIconSize : 18
            });
        panel.Children.Add(new TextBlock
        {
            Text = _dragItem.DisplayName,
            FontSize = 13,
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (Brush)TryFindResource("Br.Fg")
        });

        _ghost = new Window
        {
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            AllowsTransparency = true,
            Background = Brushes.Transparent,
            ShowInTaskbar = false,
            ShowActivated = false,
            Topmost = true,
            IsHitTestVisible = false,
            SizeToContent = SizeToContent.WidthAndHeight,
            Content = new Border
            {
                Child = panel,
                Background = (Brush)TryFindResource("Br.Menu"),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 6, 10, 6),
                Opacity = 0.92
            }
        };
        // PointToScreen 返回物理像素，Window.Left/Top 是逻辑像素（DIP），必须除以 DPI 缩放，
        // 否则高 DPI 屏上跟随框会跑到屏幕右下角。
        MoveGhost(screenPhysical);
        _ghost.Show();
        MoveGhost(screenPhysical);
    }

    private double ScreenScale()
    {
        try { var d = GetDpiForWindow(_hwnd); if (d > 0) return d / 96.0; } catch { }
        return 1.0;
    }

    private void MoveGhost(Point screenPhysical)
    {
        if (_ghost == null) return;
        double s = ScreenScale();
        _ghost.Left = screenPhysical.X / s + 12;
        _ghost.Top = screenPhysical.Y / s + 12;
    }

    private void EndDrag()
    {
        if (_ghost != null)
        {
            var g = _ghost;
            _ghost = null;
            try { g.Close(); } catch { }
        }
        if (_dragItem != null) _dragItem.IsDragging = false;
        if (List.IsMouseCaptured) List.ReleaseMouseCapture();
        _dragging = false;
        _pressed = false;
        _dragItem = null;
        SaveOrder();
    }

    public void SaveOrder()
    {
        try
        {
            _group.Order = Items.Select(x => x.Id).ToList();
            ConfigService.Save();
        }
        catch (Exception ex)
        {
            ConfigService.Log("保存排序失败: " + ex.Message);
        }
    }

    // ===== 单项右键 =====
    protected override void OnPreviewMouseRightButtonUp(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseRightButtonUp(e);
        var item = ItemAt(e.GetPosition(List));
        if (item == null) return;

        var menu = new ContextMenu();
        void Add(string header, Action act)
        {
            var mi = new MenuItem { Header = header, FontSize = 13 };
            mi.Click += (s, a) => act();
            menu.Items.Add(mi);
        }

        Add("打开所在目录", () =>
        {
            try { Process.Start("explorer.exe", "/select,\"" + item.FilePath + "\""); } catch { }
        });
        Add("重命名…", () =>
        {
            var name = PromptWindow.Show("重命名（仅改变菜单中显示的名称）", item.DisplayName, this);
            if (name == null) return;
            name = name.Trim();
            _group.Aliases ??= new Dictionary<string, string>();
            if (name.Length == 0) _group.Aliases.Remove(item.Id);
            else _group.Aliases[item.Id] = name;
            item.DisplayName = name.Length == 0 ? item.Id : name;
            ConfigService.Save();
        });
        Add("从菜单中隐藏", () =>
        {
            _group.Hidden ??= new List<string>();
            if (!_group.Hidden.Contains(item.Id)) _group.Hidden.Add(item.Id);
            Items.Remove(item);
            ConfigService.Save();
        });
        menu.Items.Add(new Separator());
        Add("刷新列表", () =>
        {
            SaveOrder();
            Close();
            _onRefresh?.Invoke();
        });

        menu.PlacementTarget = List;
        menu.IsOpen = true;
        e.Handled = true;
    }
}
