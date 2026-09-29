using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using Microsoft.Win32;
using TaskbarDock.Models;
using TaskbarDock.Services;
using TaskbarDock.Views;
using WpfForms = System.Windows.Forms;
using static TaskbarDock.Native.Win32;

namespace TaskbarDock;

public partial class App : Application
{
    // 进程间命令（第二个实例转发给已运行的实例）
    public static int CmdMessage { get; private set; }
    public const int CmdMenu = 1;
    public const int CmdSettings = 2;
    public const int CmdExit = 3;
    public const int CmdRefresh = 4;

    private Mutex _mutex;
    private WpfForms.NotifyIcon _tray;

    public static readonly List<GroupWindow> Hosts = new();
    public static App Instance => (App)Current;

    protected override void OnStartup(StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        CmdMessage = unchecked((int)RegisterWindowMessage("TaskbarDock.Command.v1"));

        var args = e.Args.ToList();
        var (cmd, groupId) = ParseArgs(args);

        _mutex = new Mutex(true, @"Global\TaskbarDock.SingleInstance", out var created);
        if (!created)
        {
            if (cmd != 0)
                PostMessage(HWND_BROADCAST, CmdMessage, (IntPtr)cmd, IntPtr.Zero);
            Shutdown();
            return;
        }

        base.OnStartup(e);

        DispatcherUnhandledException += (s, a) =>
        {
            ConfigService.Log("未处理异常: " + a.Exception);
            a.Handled = true;
        };

        ConfigService.Load();
        ConfigService.Log("启动 " + (Environment.ProcessPath ?? "") + " args=" + string.Join(" ", args));
        ThemeService.Refresh();

        SyncHosts();
        SetupTray();
        ApplyAutoStart();

        Microsoft.Win32.SystemEvents.UserPreferenceChanged += (s, a) =>
        {
            if (a.Category is Microsoft.Win32.UserPreferenceCategory.General
                or Microsoft.Win32.UserPreferenceCategory.Color
                or Microsoft.Win32.UserPreferenceCategory.VisualStyle)
            {
                Dispatcher.Invoke(() => ThemeService.Refresh());
            }
        };

        if (cmd == CmdSettings) OpenSettings();
        else if (cmd == CmdExit) ShutdownApp();
        else if (cmd == CmdRefresh) RefreshAll();
        else if (cmd == CmdMenu) DelayedOpenMenu(groupId);
    }

    /// <summary>解析命令行，返回（命令, 分组 Id）。返回 0 表示开机静默启动。</summary>
    private static (int, string) ParseArgs(List<string> args)
    {
        if (args.Contains("--startup")) return (0, null);
        if (args.Contains("--settings")) return (CmdSettings, null);
        if (args.Contains("--exit")) return (CmdExit, null);
        if (args.Contains("--refresh")) return (CmdRefresh, null);

        int i = args.IndexOf("--menu");
        if (i >= 0)
            return (CmdMenu, i + 1 < args.Count ? args[i + 1] : null);

        // 双击 exe / 点击已固定的任务栏图标（程序未运行时）
        return (CmdMenu, null);
    }

    private void DelayedOpenMenu(string groupId)
    {
        // 等任务栏按钮创建完成后再展开，便于定位到按钮位置
        var t = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
        t.Tick += (s, a) => { t.Stop(); OpenMenu(groupId); };
        t.Start();
    }

    public void HandleCommand(int cmd, string groupId)
    {
        switch (cmd)
        {
            case CmdSettings: OpenSettings(); break;
            case CmdExit: ShutdownApp(); break;
            case CmdRefresh: RefreshAll(); break;
            case CmdMenu: OpenMenu(groupId); break;
        }
    }

    public void OpenMenu(string groupId = null)
    {
        var host = string.IsNullOrEmpty(groupId)
            ? Hosts.FirstOrDefault()
            : Hosts.FirstOrDefault(h => h.Group.Id == groupId) ?? Hosts.FirstOrDefault();
        host?.OpenMenu();
    }

    // ===== 任务栏按钮（每个分组一个） =====
    public static void SyncHosts()
    {
        var cfg = ConfigService.Config.Groups;

        for (int i = Hosts.Count - 1; i >= 0; i--)
        {
            if (!cfg.Any(g => g.Id == Hosts[i].Group.Id))
            {
                Hosts[i].Close();
                Hosts.RemoveAt(i);
            }
        }

        foreach (var g in cfg)
        {
            if (Hosts.Any(h => h.Group.Id == g.Id)) continue;
            var host = new GroupWindow(g);
            Hosts.Add(host);
            host.WindowState = WindowState.Minimized;
            host.Show();
        }

        foreach (var h in Hosts) h.Refresh();
    }

    public void RefreshAll()
    {
        foreach (var h in Hosts) h.CloseMenu();
        SyncHosts();
    }

    // ===== 托盘 =====
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
        menu.Items.Add("重建任务栏图标", null, (s, e) => Dispatcher.Invoke(RefreshAll));
        menu.Items.Add("打开配置目录", null, (s, e) =>
        {
            try { Process.Start("explorer.exe", ConfigService.AppDir); } catch { }
        });
        menu.Items.Add(new WpfForms.ToolStripSeparator());
        menu.Items.Add("退出", null, (s, e) => Dispatcher.Invoke(ShutdownApp));
        _tray.ContextMenuStrip = menu;
        _tray.MouseClick += (s, e) =>
        {
            if (e.Button == WpfForms.MouseButtons.Left)
                Dispatcher.Invoke(() => OpenMenu());
        };
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
        var w = Current.Windows.OfType<SettingsWindow>().FirstOrDefault();
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
                key.SetValue("TaskbarDock", "\"" + path + "\" --startup");
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
            foreach (var h in Hosts.ToList()) h.Close();
            Hosts.Clear();
            _tray?.Dispose();
            _tray = null;
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
