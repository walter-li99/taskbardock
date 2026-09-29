using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using TaskbarDock.Icons;
using TaskbarDock.Models;
using TaskbarDock.Services;

namespace TaskbarDock.Views;

public partial class SettingsWindow : Window
{
    public ObservableCollection<DockGroupVm> Groups { get; } = new();
    private DockGroup _cur;
    private bool _loading;

    public SettingsWindow()
    {
        InitializeComponent();
        DataContext = this;
        ConfigDirText.Text = ConfigService.ConfigPath;
        VersionText.Text = "版本 " + GetType().Assembly.GetName().Version;

        PackCombo.ItemsSource = IconPacks.All;
        PackCombo.DisplayMemberPath = "DisplayName";
        PackCombo.SelectedValuePath = "Id";

        RebuildGroupList();
        LoadGlobals();
    }

    private void RebuildGroupList()
    {
        Groups.Clear();
        foreach (var g in ConfigService.Config.Groups)
            Groups.Add(new DockGroupVm(g) { IconSize = 20, ButtonSize = 24 });

        if (Groups.Count > 0)
            GroupList.SelectedIndex = Math.Min(Groups.Count - 1, Math.Max(0, GroupList.SelectedIndex));
    }

    // ===== 全局设置 =====
    private void LoadGlobals()
    {
        _loading = true;
        var c = ConfigService.Config;
        SelectByTag(ThemeCombo, c.ThemeMode);
        AcrylicCheck.IsChecked = c.UseAcrylic;
        IconSizeBox.Text = c.IconSize.ToString();
        MenuWidthBox.Text = ((int)c.MenuWidth).ToString();
        MenuHeightBox.Text = ((int)c.MenuMaxHeight).ToString();
        ShowIconsCheck.IsChecked = c.ShowItemIcons;
        ItemIconSizeBox.Text = c.ItemIconSize.ToString();
        AutoStartCheck.IsChecked = c.StartWithWindows;
        TrayCheck.IsChecked = c.ShowTrayIcon;
        _loading = false;
    }

    private void ApplyGlobalsFromUi()
    {
        var c = ConfigService.Config;
        c.ThemeMode = TagOf(ThemeCombo) ?? "System";
        c.UseAcrylic = AcrylicCheck.IsChecked == true;

        if (int.TryParse(IconSizeBox.Text, out var s)) c.IconSize = Math.Clamp(s, 20, 64);
        if (int.TryParse(MenuWidthBox.Text, out var mw)) c.MenuWidth = Math.Clamp(mw, 160, 600);
        if (int.TryParse(MenuHeightBox.Text, out var mh)) c.MenuMaxHeight = Math.Clamp(mh, 160, 1200);
        c.ShowItemIcons = ShowIconsCheck.IsChecked == true;
        if (int.TryParse(ItemIconSizeBox.Text, out var iis)) c.ItemIconSize = Math.Clamp(iis, 12, 48);
        c.StartWithWindows = AutoStartCheck.IsChecked == true;
        c.ShowTrayIcon = TrayCheck.IsChecked == true;
    }

    private static void SelectByTag(ComboBox cb, string tag)
    {
        foreach (ComboBoxItem item in cb.Items)
            if ((string)item.Tag == tag) { cb.SelectedItem = item; return; }
        if (cb.Items.Count > 0) cb.SelectedIndex = 0;
    }

    private static string TagOf(ComboBox cb) => (cb.SelectedItem as ComboBoxItem)?.Tag as string;

    // ===== 分组 =====
    private void OnGroupSelected(object sender, SelectionChangedEventArgs e)
    {
        _cur = (GroupList.SelectedItem as DockGroupVm)?.Group;
        Detail.IsEnabled = _cur != null;
        if (_cur == null) { IconGrid.Children.Clear(); return; }

        _loading = true;
        NameBox.Text = _cur.Name;
        FolderBox.Text = _cur.Folder;
        CustomIconBox.Text = _cur.CustomIconPath;
        PackCombo.SelectedValue = _cur.IconPack;
        _loading = false;

        BuildIconGrid();
    }

    private void BuildIconGrid()
    {
        IconGrid.Children.Clear();
        if (_cur == null) return;

        var fg = (Brush)TryFindResource("Br.Fg");
        foreach (var (key, label) in IconPacks.Keys)
        {
            var icon = new PackIcon { Pack = _cur.IconPack, IconKey = key, IconSize = 22, Foreground = fg };
            var btn = new Button
            {
                Style = (Style)TryFindResource("IconPickButton"),
                Tag = key,
                ToolTip = label,
                Margin = new Thickness(0, 0, 8, 8),
                Background = key == _cur.IconKey ? (Brush)TryFindResource("Br.Pressed") : Brushes.Transparent,
                Content = icon
            };
            icon.SetResourceReference(ForegroundProperty, "Br.Fg");
            btn.Click += (s, e) =>
            {
                _cur.IconKey = (string)((Button)s).Tag;
                BuildIconGrid();
                ApplyLive();
            };
            IconGrid.Children.Add(btn);
        }
    }

    private void OnNameChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading || _cur == null) return;
        _cur.Name = NameBox.Text;
        var vm = GroupList.SelectedItem as DockGroupVm;
        if (vm != null) vm.OnPropertyChanged(nameof(vm.Name));
        ApplyLive();
    }

    private void OnFolderChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading || _cur == null) return;
        _cur.Folder = FolderBox.Text;
        ApplyLive();
    }

    private void OnPackChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || _cur == null) return;
        if (PackCombo.SelectedValue is string id)
        {
            _cur.IconPack = id;
            BuildIconGrid();
            ApplyLive();
        }
    }

    private void OnCustomIconChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading || _cur == null) return;
        _cur.CustomIconPath = CustomIconBox.Text;
        ApplyLive();
    }

    private void OnBrowseFolder(object sender, RoutedEventArgs e)
    {
        using var dlg = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "选择存放快捷方式的文件夹",
            SelectedPath = Directory.Exists(_cur?.Folder) ? _cur.Folder : Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            UseDescriptionForTitle = true
        };
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            FolderBox.Text = dlg.SelectedPath;
        }
    }

    private void OnBrowseIcon(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择图标文件",
            Filter = "图片与图标|*.png;*.jpg;*.jpeg;*.bmp;*.ico;*.exe;*.dll|所有文件|*.*"
        };
        if (dlg.ShowDialog() == true)
            CustomIconBox.Text = dlg.FileName;
    }

    private void OnClearIcon(object sender, RoutedEventArgs e)
    {
        CustomIconBox.Text = "";
    }

    private void OnAddGroup(object sender, RoutedEventArgs e)
    {
        var g = new DockGroup
        {
            Name = "新分组 " + (ConfigService.Config.Groups.Count + 1),
            Folder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        };
        ConfigService.Config.Groups.Add(g);
        RebuildGroupList();
        GroupList.SelectedIndex = Groups.Count - 1;
        ApplyLive();
    }

    private void OnDelGroup(object sender, RoutedEventArgs e)
    {
        if (_cur == null) return;
        if (ConfigService.Config.Groups.Count <= 1)
        {
            MessageBox.Show("至少保留一个分组。", "任务栏整合", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        ConfigService.Config.Groups.Remove(_cur);
        _cur = null;
        RebuildGroupList();
        ApplyLive();
    }

    // ===== 应用 =====
    private void ApplyLive()
    {
        ApplyGlobalsFromUi();
        ConfigService.Save();
        ThemeService.Refresh();
        App.SyncHosts();
        App.Instance?.RefreshTray();
        App.Instance?.ApplyAutoStart();
    }

    private void OnVisualChanged(object sender, RoutedEventArgs e) { if (!_loading) ApplyLive(); }
    private void OnGeneralChanged(object sender, RoutedEventArgs e) { if (!_loading) ApplyLive(); }

    private void OnApply(object sender, RoutedEventArgs e) => ApplyLive();

    private void OnSave(object sender, RoutedEventArgs e)
    {
        ApplyLive();
        Close();
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();

    private void OnOpenConfigDir(object sender, RoutedEventArgs e)
    {
        try { Process.Start("explorer.exe", ConfigService.AppDir); } catch { }
    }

    private void OnRebuildIcons(object sender, RoutedEventArgs e)
    {
        App.Instance?.RefreshAll();
    }

    private void OnDragMove(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
        {
            try { DragMove(); } catch { }
        }
    }

    /// <summary>窗口任意空白处都能拖动。</summary>
    private void OnWindowDrag(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton != System.Windows.Input.MouseButtonState.Pressed) return;

        var src = e.OriginalSource as DependencyObject;
        while (src != null)
        {
            if (src is TextBox || src is ButtonBase || src is ComboBox || src is ListBox
                || src is CheckBox || src is ScrollBar || src is TabItem || src is Slider
                || src is MenuBase || src is PasswordBox)
                return;
            src = VisualTreeHelper.GetParent(src);
        }

        try { DragMove(); } catch { }
    }
}
