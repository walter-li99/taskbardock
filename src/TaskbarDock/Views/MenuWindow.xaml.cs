using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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
    public string TitleText { get; set; } = "";
    public double MenuMaxHeight { get; set; } = 520;
    public int ItemIconSize { get; set; } = 20;

    private readonly DockGroup _group;
    private readonly DockWindow _dock;
    private Button _anchor;
    private int _edge = ABE_BOTTOM;
    private Rect _anchorRect;

    // 拖拽状态
    private bool _pressed, _dragging, _moved;
    private Point _start;
    private ShortcutItem _dragItem;
    private Popup _ghost;

    // 外部点击关闭
    private IntPtr _hookId = IntPtr.Zero;
    private HookProc _hookProc;
    private IntPtr _hwnd = IntPtr.Zero;

    public MenuWindow(DockGroup group, List<ShortcutItem> items, DockWindow dock)
    {
        InitializeComponent();
        _group = group;
        _dock = dock;

        var cfg = ConfigService.Config;
        ItemIconSize = cfg.ShowItemIcons ? cfg.ItemIconSize : 0;
        MenuMaxHeight = cfg.MenuMaxHeight;
        TitleText = group.Name;

        foreach (var it in items)
        {
            it.IconsVisibility = cfg.ShowItemIcons ? Visibility.Visible : Visibility.Collapsed;
            Items.Add(it);
        }

        MaxWidth = cfg.MenuWidth;
        MaxHeight = Math.Min(cfg.MenuMaxHeight + 120, SystemParameters.WorkArea.Height - 24);
        Opacity = 0;

        DataContext = this;
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    public void Anchor(Button btn, int edge)
    {
        _anchor = btn;
        _edge = edge;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _hwnd = new WindowInteropHelper(this).Handle;

        if (_anchor != null)
        {
            var p = _anchor.PointToScreen(new Point(0, 0));
            _anchorRect = new Rect(p, new Size(_anchor.ActualWidth, _anchor.ActualHeight));
        }

        Position();

        try
        {
            if (ConfigService.Config.UseAcrylic)
                SetAcrylic(_hwnd, ((SolidColorBrush)TryFindResource("Br.MenuAcrylic")).Color);
        }
        catch { }

        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(120));
        BeginAnimation(OpacityProperty, fade);

        var tr = new TranslateTransform();
        RenderTransform = tr;
        var slide = new DoubleAnimation(_edge == ABE_TOP ? -8 : 8, 0, TimeSpan.FromMilliseconds(160))
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        tr.BeginAnimation(TranslateTransform.YProperty, slide);

        Activate();
        InstallHook();
    }

    private void Position()
    {
        if (_anchor == null) return;

        var p = _anchor.PointToScreen(new Point(0, 0));
        double bw = _anchor.ActualWidth, bh = _anchor.ActualHeight;
        double mw = ActualWidth, mh = ActualHeight;
        var wa = SystemParameters.WorkArea;

        double left, top;
        switch (_edge)
        {
            case ABE_TOP:
                left = p.X; top = p.Y + bh; break;
            case ABE_LEFT:
                left = p.X + bw; top = p.Y; break;
            case ABE_RIGHT:
                left = p.X - mw; top = p.Y; break;
            default:
                left = p.X; top = p.Y - mh; break;
        }

        left = Math.Max(wa.Left + 4, Math.Min(left, Math.Max(wa.Left + 4, wa.Right - mw - 4)));
        top = Math.Max(wa.Top + 4, Math.Min(top, Math.Max(wa.Top + 4, wa.Bottom - mh - 4)));
        Left = left;
        Top = top;
    }

    private void OnClosed(object sender, EventArgs e)
    {
        UninstallHook();
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
                if (!r.Contains(x, y) && !_anchorRect.Contains(x, y))
                    Dispatcher.BeginInvoke(new Action(() => Close()));
            }
            catch { }
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private void OnDeactivated(object sender, EventArgs e)
    {
        // 拖拽中的 Popup 或子窗口可能导致短暂失焦，这里不做关闭，交给鼠标钩子处理
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
        _moved = false;
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
            _moved = true;
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
        try
        {
            ShortcutService.Launch(item);
        }
        catch (Exception ex)
        {
            ConfigService.Log("启动失败: " + ex.Message);
        }
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

    private void ShowGhost(Point screen)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
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
            VerticalAlignment = VerticalAlignment.Center
        });

        _ghost = new Popup
        {
            Child = new Border
            {
                Child = panel,
                Background = (Brush)TryFindResource("Br.Menu"),
                BorderBrush = (Brush)TryFindResource("Br.Border"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 6, 10, 6),
                Opacity = 0.92
            },
            Placement = PlacementMode.Absolute,
            IsHitTestVisible = false,
            AllowsTransparency = true,
            HorizontalOffset = screen.X + 8,
            VerticalOffset = screen.Y + 8
        };
        _ghost.IsOpen = true;
    }

    private void MoveGhost(Point screen)
    {
        if (_ghost == null) return;
        _ghost.HorizontalOffset = screen.X + 8;
        _ghost.VerticalOffset = screen.Y + 8;
    }

    private void EndDrag()
    {
        if (_ghost != null) { _ghost.IsOpen = false; _ghost = null; }
        if (_dragItem != null) _dragItem.IsDragging = false;
        if (List.IsMouseCaptured) List.ReleaseMouseCapture();
        _dragging = false;
        _pressed = false;
        _moved = false;
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

    // ===== 底部按钮 =====
    private void OnOpenFolder(object sender, RoutedEventArgs e)
    {
        try { Process.Start("explorer.exe", _group.Folder); } catch { }
        Close();
    }

    private void OnSettings(object sender, RoutedEventArgs e)
    {
        Close();
        App.Instance.OpenSettings();
    }

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
        Add(item.IconsVisibility == Visibility.Visible ? "隐藏图标" : "显示图标", () =>
        {
            item.IconsVisibility = item.IconsVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        });
        menu.Items.Add(new Separator());
        Add("刷新列表", () =>
        {
            SaveOrder();
            Close();
            _dock?.Rebuild();
        });

        menu.PlacementTarget = List;
        menu.IsOpen = true;
        e.Handled = true;
    }
}
