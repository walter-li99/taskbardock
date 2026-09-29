using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using Microsoft.Win32;
using TaskbarDock.Services;
using TaskbarDock.Views;
using WpfForms = System.Windows.Forms;

namespace TaskbarDock;

public partial class App : Application
{
    private Mutex _mutex;
    private WpfForms.NotifyIcon _tray;
    public static DockWindow Dock { get; private set; }
    public static App Instance => (App)Current;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, @"Global\TaskbarDock.SingleInstance", out var created);
        if (!created)
        {
            Shutdown();
            return;
        }

        base.OnStartup(e);

        DispatcherUnhandledException += (s, args) =>
        {
            ConfigService.Log("未处理异常: " + args.Exception);
            args.Handled = true;
        };

        ConfigService.Load();
        ConfigService.Log("启动 " + (Environment.ProcessPath ?? ""));
        ThemeService.Refresh();

        Dock = new DockWindow();
        Dock.Show();

        SetupTray();
        ApplyAutoStart();

        Microsoft.Win32.SystemEvents.UserPreferenceChanged += (s, args) =>
        {
            if (args.Category is Microsoft.Win32.UserPreferenceCategory.General
                or Microsoft.Win32.UserPreferenceCategory.Color
                or Microsoft.Win32.UserPreferenceCategory.VisualStyle)
            {
                Dispatcher.Invoke(() => ThemeService.Refresh());
            }
        };
    }

    public void SetupTray()
    {
        if (_tray != null) return;
        if (!ConfigService.Config.ShowTrayIcon) return;

        System.Drawing.Icon icon = null;
        try { icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath); } catch { }
        icon ??= System.Drawing.SystemIcons.Application;

        _tray = new WpfForms.NotifyIcon
        {
            Icon = icon,
            Text = "任务栏整合",
            Visible = true
        };

        var menu = new WpfForms.ContextMenuStrip();
        menu.Items.Add("设置…", null, (s, e) => Dispatcher.Invoke(OpenSettings));
        menu.Items.Add("重新加载", null, (s, e) => Dispatcher.Invoke(() => Dock?.Rebuild()));
        menu.Items.Add("打开配置目录", null, (s, e) =>
        {
            try { Process.Start("explorer.exe", ConfigService.AppDir); } catch { }
        });
        menu.Items.Add(new WpfForms.ToolStripSeparator());
        menu.Items.Add("退出", null, (s, e) => Dispatcher.Invoke(ShutdownApp));
        _tray.ContextMenuStrip = menu;
        _tray.DoubleClick += (s, e) => Dispatcher.Invoke(OpenSettings);
    }

    public void RefreshTray()
    {
        if (ConfigService.Config.ShowTrayIcon)
        {
            if (_tray == null) SetupTray();
        }
        else if (_tray != null)
        {
            _tray.Visible = false;
            _tray.Dispose();
            _tray = null;
        }
    }

    public void OpenSettings()
    {
        var w = Application.Current.Windows.OfType<SettingsWindow>().FirstOrDefault();
        if (w == null)
        {
            w = new SettingsWindow();
            w.Show();
        }
        w.Activate();
    }

    public void ApplyAutoStart()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (key == null) return;
            var path = Environment.ProcessPath;
            if (ConfigService.Config.StartWithWindows && !string.IsNullOrEmpty(path))
                key.SetValue("TaskbarDock", "\"" + path + "\"");
            else
                key.DeleteValue("TaskbarDock", false);
        }
        catch (Exception ex)
        {
            ConfigService.Log("设置开机自启失败: " + ex.Message);
        }
    }

    public void ShutdownApp()
    {
        try
        {
            Dock?.Close();
            _tray?.Dispose();
        }
        catch { }
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _tray?.Dispose();
            ConfigService.Save();
        }
        catch { }
        base.OnExit(e);
    }
}
