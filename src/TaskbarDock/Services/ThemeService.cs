using System;
using System.Windows.Media;
using Microsoft.Win32;
using TaskbarDock.Native;

namespace TaskbarDock.Services;

public static class ThemeService
{
    public static bool IsLight { get; private set; } = true;
    public static Color Accent { get; private set; } = Color.FromRgb(0, 120, 212);

    public static event Action Changed;

    public static bool SystemIsLight()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var v = k?.GetValue("AppsUseLightTheme");
            if (v is int i) return i != 0;
        }
        catch { }
        return true;
    }

    public static Color SystemAccent()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent");
            if (k?.GetValue("AccentColorMenu") is int abgr)
            {
                return Color.FromRgb((byte)(abgr & 0xFF), (byte)((abgr >> 8) & 0xFF), (byte)((abgr >> 16) & 0xFF));
            }
        }
        catch { }
        try
        {
            Win32.DwmGetColorizationColor(out var c, out _);
            return Color.FromRgb((byte)(c & 0xFF), (byte)((c >> 8) & 0xFF), (byte)((c >> 16) & 0xFF));
        }
        catch { }
        return Color.FromRgb(0, 120, 212);
    }

    public static void Refresh()
    {
        bool light = Services.ConfigService.Config?.ThemeMode switch
        {
            "Light" => true,
            "Dark" => false,
            _ => SystemIsLight()
        };
        IsLight = light;
        Accent = SystemAccent();
        Apply();
        Changed?.Invoke();
    }

    static Color Mix(Color a, Color b, double t) =>
        Color.FromRgb((byte)(a.R + (b.R - a.R) * t), (byte)(a.G + (b.G - a.G) * t), (byte)(a.B + (b.B - a.B) * t));

    public static void Apply()
    {
        var res = System.Windows.Application.Current.Resources;
        // XAML 里的画刷是冻结的，只能整体替换（替换会触发 DynamicResource 重新求值）
        var target = res.MergedDictionaries.Count > 0 ? res.MergedDictionaries[0] : res;
        void Set(string key, Color c) => target[key] = new SolidColorBrush(c);

        // 任务栏 / Dock 颜色（贴近 Windows 11 任务栏实际用色）
        Color taskbar = IsLight ? Color.FromRgb(0xF3, 0xF3, 0xF3) : Color.FromRgb(0x20, 0x20, 0x20);
        // 菜单颜色（贴近 Win11 上下文菜单）
        Color menu = IsLight ? Color.FromRgb(0xFF, 0xFF, 0xFF) : Color.FromRgb(0x2B, 0x2B, 0x2B);
        Color fg = IsLight ? Color.FromRgb(0x1B, 0x1B, 0x1B) : Color.FromRgb(0xF3, 0xF3, 0xF3);
        Color fg2 = IsLight ? Color.FromRgb(0x5C, 0x5C, 0x5C) : Color.FromRgb(0xB3, 0xB3, 0xB3);

        // 半透明叠加层（类似 Win11 的 subtle 分层）
        Color hover = IsLight
            ? Color.FromArgb(0x14, 0x00, 0x00, 0x00)
            : Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF);
        Color pressed = IsLight
            ? Color.FromArgb(0x1F, 0x00, 0x00, 0x00)
            : Color.FromArgb(0x24, 0xFF, 0xFF, 0xFF);
        Color border = IsLight
            ? Color.FromArgb(0x18, 0x00, 0x00, 0x00)
            : Color.FromArgb(0x1A, 0xFF, 0xFF, 0xFF);
        Color sep = IsLight
            ? Color.FromArgb(0x12, 0x00, 0x00, 0x00)
            : Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF);
        Color shadow = Color.FromArgb(0x40, 0x00, 0x00, 0x00);

        Set("Br.Taskbar", taskbar);
        Set("Br.Menu", menu);
        Set("Br.Fg", fg);
        Set("Br.Fg2", fg2);
        Set("Br.Hover", hover);
        Set("Br.Pressed", pressed);
        Set("Br.Border", border);
        Set("Br.Sep", sep);
        Set("Br.Shadow", shadow);
        Set("Br.Accent", Accent);

        // 亚克力底色：菜单用更淡的半透明，使背景虚化可见
        Set("Br.MenuAcrylic", IsLight
            ? Color.FromArgb(0xE6, 0xFC, 0xFC, 0xFC)
            : Color.FromArgb(0xE6, 0x2B, 0x2B, 0x2B));
        Set("Br.TaskbarAcrylic", IsLight
            ? Color.FromArgb(0xE6, 0xF3, 0xF3, 0xF3)
            : Color.FromArgb(0xE6, 0x20, 0x20, 0x20));

        SetImmersiveDark();
    }

    static void SetImmersiveDark()
    {
        try
        {
            foreach (var w in System.Windows.Application.Current.Windows)
            {
                if (w is System.Windows.Window win)
                {
                    var hwnd = new System.Windows.Interop.WindowInteropHelper(win).Handle;
                    if (hwnd != IntPtr.Zero)
                    {
                        int dark = IsLight ? 0 : 1;
                        Win32.DwmSetWindowAttribute(hwnd, Win32.DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
                        int round = Win32.DWMWCP_ROUND;
                        Win32.DwmSetWindowAttribute(hwnd, Win32.DWMWA_WINDOW_CORNER_PREFERENCE, ref round, sizeof(int));
                    }
                }
            }
        }
        catch { }
    }
}
