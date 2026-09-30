using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TaskbarDock.Icons;

/// <summary>
/// 手机 UI 风格图标：圆角渐变“应用方块” + 白色实心图形。
/// 与现有线性 / 单色 / Emoji 图标完全不同——彩色、实心、非线稿。
/// </summary>
public static class PhoneIconFactory
{
    // key -> (左上色, 右下色, 白色图形路径)
    private static readonly Dictionary<string, (Color Top, Color Bottom, string Glyph)> Map = new()
    {
        ["folder"]    = (C(0xFF, 0xA7, 0x26), C(0xF5, 0x7C, 0x00), Blocks("folder")),
        ["box"]       = (C(0x26, 0xA6, 0x9A), C(0x00, 0x89, 0x7B), Blocks("box")),
        ["layers"]    = (C(0x5C, 0x6B, 0xC0), C(0x3F, 0x51, 0xB5), Fluent("layers")),
        ["grid"]      = (C(0xAB, 0x47, 0xBC), C(0x8E, 0x24, 0xAA), Blocks("grid")),
        ["globe"]     = (C(0x29, 0xB6, 0xF6), C(0x02, 0x88, 0xD1), Fluent("globe")),
        ["home"]      = (C(0xFF, 0x8A, 0x80), C(0xFF, 0x52, 0x52), Blocks("home")),
        ["code"]      = (C(0x78, 0x90, 0x9C), C(0x37, 0x47, 0x4F), Fluent("code")),
        ["terminal"]  = (C(0x66, 0xBB, 0x6A), C(0x2E, 0x7D, 0x32), Fluent("terminal")),
        ["document"]  = (C(0x4D, 0xD0, 0xE1), C(0x00, 0xAC, 0xC1), Fluent("document")),
        ["palette"]   = (C(0xFF, 0x70, 0x43), C(0xF4, 0x51, 0x1E), Fluent("palette")),
        ["camera"]    = (C(0x8D, 0x9B, 0xA8), C(0x54, 0x6E, 0x7A), Blocks("camera")),
        ["music"]     = (C(0xEC, 0x40, 0x7A), C(0xC2, 0x18, 0x5B), Fluent("music")),
        ["game"]      = (C(0x95, 0x75, 0xCD), C(0x5E, 0x35, 0xB1), Fluent("game")),
        ["chat"]      = (C(0x1D, 0xE9, 0xB6), C(0x00, 0xBF, 0xA5), Fluent("chat")),
        ["cloud"]     = (C(0xB0, 0xBE, 0xC5), C(0x78, 0x90, 0x9C), Shared("cloud")),
        ["download"]  = (C(0x9C, 0xCC, 0x65), C(0x55, 0x8B, 0x2F), Fluent("download")),
        ["lightning"] = (C(0xFF, 0xEE, 0x58), C(0xFB, 0xC0, 0x2D), Shared("lightning")),
        ["clock"]     = (C(0x90, 0xA4, 0xAE), C(0x45, 0x5A, 0x64), Blocks("clock")),
        ["heart"]     = (C(0xE5, 0x39, 0x35), C(0xB7, 0x1C, 0x1C), Shared("heart")),
        ["star"]      = (C(0xFF, 0xCA, 0x28), C(0xFF, 0x8F, 0x00), Fluent("star")),
        ["shield"]    = (C(0x42, 0xA5, 0xF5), C(0x1E, 0x88, 0xE5), Blocks("shield")),
        ["bulb"]      = (C(0xFF, 0xD5, 0x4F), C(0xFF, 0xB3, 0x00), Blocks("bulb")),
        ["tag"]       = (C(0xB3, 0x9D, 0xDB), C(0x7E, 0x57, 0xC2), Blocks("tag")),
        ["settings"]  = (C(0x9E, 0x9E, 0x9E), C(0x61, 0x61, 0x61), Fluent("settings")),
    };

    // 复用已有矢量图形的白色填充版本，保证线条干净
    private static readonly Dictionary<string, string> FluentGlyphs = new()
    {
        ["globe"]     = "M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20zm0 3.2c1 0 1.9 1.9 2.2 4.4H9.8c.3-2.5 1.2-4.4 2.2-4.4zM12 18.8c-1 0-1.9-1.9-2.2-4.4h4.4c-.3 2.5-1.2 4.4-2.2 4.4zM4.6 9.6h4.7c.1.8.1 1.6.1 2.4s0 1.6-.1 2.4H4.6a7.9 7.9 0 0 1 0-4.8zM14.7 9.6h4.7a7.9 7.9 0 0 1 0 4.8h-4.7c.1-.8.1-1.6.1-2.4s0-1.6-.1-2.4z",
        ["code"]      = "M7.6 5 2 12l5.6 7 1.6-1.3L5.2 12l4-5.7L7.6 5zM16.4 5 22 12l-5.6 7-1.6-1.3L18.8 12l-4-5.7L16.4 5z",
        ["document"]  = "M13.5 2H7a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V7.5L13.5 2zM13 3.5L17.5 8H13V3.5zM8.5 12h7v1.6h-7V12zm0 4h7v1.6h-7V16z",
        ["palette"]   = "M12 3a9 9 0 0 0 0 18c1.1 0 2-.9 2-2 0-.5-.2-1-.5-1.3-.3-.4-.5-.8-.5-1.2 0-.9.7-1.6 1.6-1.6H16a5 5 0 0 0 5-5c0-4.1-4-7-9-7zM8 10a1.4 1.4 0 1 1 0 2.8A1.4 1.4 0 0 1 8 10zm3-3.5a1.4 1.4 0 1 1 0 2.8 1.4 1.4 0 0 1 0-2.8zm5 1a1.4 1.4 0 1 1 0 2.8 1.4 1.4 0 0 1 0-2.8z",
        ["game"]      = "M8 9h2.5v2.5H13V9h2.5V6.5H13V4h-2.5v2.5H8V9zM6.8 12H5.2a4.8 4.8 0 0 0-4.7 4.1c-.2 1.3-.3 3.6-.3 4.6 0 1.5 1 2.3 2.4 2.3 1 0 1.8-.5 2.4-1.2l1.3-1.4c.4-.4.9-.6 1.5-.6h5.4c.6 0 1.1.2 1.5.6l1.3 1.4c.6.7 1.4 1.2 2.4 1.2 1.4 0 2.4-.8 2.4-2.3 0-1-.1-3.3-.3-4.6A4.8 4.8 0 0 0 18.8 12h-1.6a1.2 1.2 0 0 1 0-2.4h1.6a2.4 2.4 0 0 1 .2 0h.1a4.4 4.4 0 0 0-2.9-4A12 12 0 0 0 14 5.2V5a2 2 0 0 0-2-2h-1a1 1 0 0 0-1 1v1.2a12 12 0 0 0-2.2.4 4.4 4.4 0 0 0-2.9 4 4.4 4.4 0 0 0 .1 1 2.4 2.4 0 0 1 .2 0h1.6a1.2 1.2 0 0 1 0 2.4z",
        ["music"]     = "M20 3.5v13a3.5 3.5 0 1 1-2-3.16V7.2l-7 1.4v8.3a3.5 3.5 0 1 1-2-3.16V6.7l11-2.2z",
        ["chat"]      = "M12 3c5 0 9 3.4 9 7.6 0 4.2-4 7.6-9 7.6-.9 0-1.8-.1-2.6-.3l-4.2 2a.6.6 0 0 1-.9-.7l1-3.7A7.3 7.3 0 0 1 3 10.6C3 6.4 7 3 12 3z",
        ["terminal"]  = "M3.7 4.3A2 2 0 0 0 2 6.2v11.6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2V6.2a2 2 0 0 0-2-2H3.7zM6.4 8.6l3.6 3.4-3.6 3.4-.9-1 2.6-2.4-2.6-2.4.9-1zM12 14.6h6v1.6h-6v-1.6z",
        ["settings"]  = "M12 9a3 3 0 1 0 0 6 3 3 0 0 0 0-6zM10.7 2.5a1 1 0 0 1 .9.6l.6 1.5 1.7.5a1 1 0 0 1 .6 1.3l-.7 1.8 1 1.5a1 1 0 0 1-.2 1.3l-1.5 1.1-.1 1.8a1 1 0 0 1-1 1h-2.5a1 1 0 0 1-1-1l-.1-1.8-1.5-1.1a1 1 0 0 1-.2-1.3l1-1.5-.7-1.8a1 1 0 0 1 .6-1.3l1.7-.5.6-1.5a1 1 0 0 1 .9-.6h.9z",
        ["star"]      = "M12 2.6l2.9 5.9 6.5.9-4.7 4.6 1.1 6.4-5.8-3-5.8 3 1.1-6.4L2.6 9.4l6.5-.9L12 2.6z",
        ["download"]  = "M12 2a1.2 1.2 0 0 1 1.2 1.2v9.2l3-3 1.2 1.2-5.1 5.1a1.2 1.2 0 0 1-1.7 0L5.5 10.6l1.2-1.2 3 3V3.2A1.2 1.2 0 0 1 12 2zM4.2 17.4h15.6v3.2H4.2v-3.2z",
        ["layers"]    = "M12 3 2.5 8l9.5 5 9.5-5L12 3ZM4.8 11.7 2.5 12.9 12 17.9l9.5-5-2.3-1.2L12 15.6l-7.2-3.9ZM4.8 16.2l-2.3 1.2L12 22.4l9.5-5-2.3-1.2L12 20.1l-7.2-3.9Z",
    };

    private static readonly Dictionary<string, string> BlocksGlyphs = new()
    {
        ["folder"]   = "M5 3h14a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2Z",
        ["box"]      = "M12 2 3 7v10l9 5 9-5V7l-9-5Z",
        ["grid"]     = "M7.5 3.5a4 4 0 1 0 0 8 4 4 0 0 0 0-8Zm9 0a4 4 0 1 0 0 8 4 4 0 0 0 0-8Zm-9 9a4 4 0 1 0 0 8 4 4 0 0 0 0-8Zm9 0a4 4 0 1 0 0 8 4 4 0 0 0 0-8Z",
        ["home"]     = "M12 2 22 9.5 18.2 21H5.8L2 9.5 12 2Z",
        ["camera"]   = "M12 6.5a5.5 5.5 0 1 0 0 11 5.5 5.5 0 0 0 0-11Z",
        ["clock"]    = "M12 2a10 10 0 1 0 10 10H12V2Z",
        ["shield"]   = "M12 2l8 3v8c0 5-4 8-8 9-4-1-8-4-8-9V5l8-3Z",
        ["bulb"]     = "M12 2a8 8 0 0 1 8 8c0 3.1-2.1 5-4 7.2H8C6.1 15 4 13.1 4 10a8 8 0 0 1 8-8Z",
        ["tag"]      = "M8.5 4H21l-5.5 16H3L8.5 4Z",
    };

    private static readonly Dictionary<string, string> SharedGlyphs = new()
    {
        ["cloud"]     = "M6.5 19a4.5 4.5 0 0 1-.36-8.99 6 6 0 0 1 11.7-1.2A4.25 4.25 0 0 1 17.75 19H6.5Z",
        ["lightning"] = "M13 2 4.5 13.5H10L9 22l8.5-11.5H12L13 2Z",
        ["heart"]     = "M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z",
    };

    private static string Fluent(string k) => FluentGlyphs[k];
    private static string Blocks(string k) => BlocksGlyphs[k];
    private static string Shared(string k) => SharedGlyphs[k];
    private static Color C(byte r, byte g, byte b) => Color.FromRgb(r, g, b);

    public static UIElement Build(string key, double size)
    {
        if (!Map.TryGetValue(key, out var e)) e = Map["folder"];
        var (top, bottom, glyph) = e;

        var grid = new Grid
        {
            Width = size,
            Height = size,
            SnapsToDevicePixels = true,
            UseLayoutRounding = true
        };

        // 圆角渐变应用方块
        var tile = new Border
        {
            CornerRadius = new CornerRadius(size * 0.24),
            Background = new LinearGradientBrush(top, bottom, new Point(0, 0), new Point(1, 1))
        };
        grid.Children.Add(tile);

        // 顶部高光，营造手机应用图标的玻璃质感
        var sheen = new Border
        {
            CornerRadius = new CornerRadius(size * 0.24),
            Background = new LinearGradientBrush(
                Color.FromArgb(0x3C, 0xFF, 0xFF, 0xFF), Colors.Transparent,
                new Point(0, 0), new Point(0, 0.55)),
            IsHitTestVisible = false
        };
        grid.Children.Add(sheen);

        // 白色实心图形
        var path = new Path
        {
            Data = Geometry.Parse(glyph),
            Fill = Brushes.White,
            Stretch = Stretch.Uniform,
            Width = size * 0.52,
            Height = size * 0.52,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            SnapsToDevicePixels = true,
            IsHitTestVisible = false
        };
        grid.Children.Add(path);

        return grid;
    }
}
