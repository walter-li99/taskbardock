using System.Collections.Generic;

namespace TaskbarDock.Icons;

public class IconPackDef
{
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public string Kind { get; set; }          // Font | Path | Emoji
    public string FontFamily { get; set; }    // Kind=Font
    public double StrokeThickness { get; set; } = 2;   // Kind=Path, 0 表示填充
    public bool Filled { get; set; }
    public bool Badge { get; set; }           // 彩色徽章：强调色圆角底 + 对比色图形
    public string Base { get; set; }          // 自己没有的图形时回退到的包
    public Dictionary<string, string> Glyphs { get; set; } = new();
}

public static class IconPacks
{
    // 全部图标键（24 种）。新增的图形放在 Shared 中，各包可按需覆盖。
    public static readonly List<(string Key, string Label)> Keys = new()
    {
        ("folder", "文件夹"),
        ("box", "收纳箱"),
        ("layers", "图层"),
        ("grid", "网格"),
        ("globe", "浏览器"),
        ("home", "主页"),
        ("code", "开发"),
        ("terminal", "终端"),
        ("document", "文档"),
        ("palette", "设计"),
        ("camera", "相机"),
        ("music", "影音"),
        ("game", "游戏"),
        ("chat", "通讯"),
        ("cloud", "云盘"),
        ("download", "下载"),
        ("lightning", "快捷"),
        ("clock", "时钟"),
        ("heart", "喜爱"),
        ("star", "收藏"),
        ("shield", "安全"),
        ("bulb", "灵感"),
        ("tag", "标签"),
        ("settings", "工具"),
    };

    /// <summary>各包通用图形（实心 / 描边渲染都成立）。</summary>
    public static readonly Dictionary<string, string> Shared = new()
    {
        ["folder"] = "M3 6a3 3 0 0 1 3-3h3.2a2 2 0 0 1 1.63.84L12 5.6h6a3 3 0 0 1 3 3V18a3 3 0 0 1-3 3H6a3 3 0 0 1-3-3V6z",
        ["lightning"] = "M13 2 4.5 13.5H10L9 22l8.5-11.5H12L13 2Z",
        ["shield"] = "M12 2.5 4.5 5.4v6.1c0 4.8 3.2 8.1 7.5 9.9 4.3-1.8 7.5-5.1 7.5-9.9V5.4L12 2.5Z",
        ["grid"] = "M4 4h6v6H4V4Zm10 0h6v6h-6V4ZM4 14h6v6H4v-6Zm10 0h6v6h-6v-6Z",
        ["heart"] = "M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z",
        ["clock"] = "M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20Zm0 2.5a7.5 7.5 0 1 1 0 15 7.5 7.5 0 0 1 0-15ZM11 6.5h2v6l4 2.4-1 1.7-5-3V6.5Z",
        ["camera"] = "M9.2 4 7.6 6H4.5A2.5 2.5 0 0 0 2 8.5v9A2.5 2.5 0 0 0 4.5 20h15a2.5 2.5 0 0 0 2.5-2.5v-9A2.5 2.5 0 0 0 19.5 6h-3.1L14.8 4H9.2Zm2.8 4.5a4.8 4.8 0 1 1 0 9.6 4.8 4.8 0 0 1 0-9.6Zm0 2.2a2.6 2.6 0 1 0 0 5.2 2.6 2.6 0 0 0 0-5.2Z",
        ["home"] = "M12 3 2.5 11h2.7v9h5.3v-5.5h3V20h5.3v-9h2.7L12 3Z",
        ["bulb"] = "M12 2a7 7 0 0 0-4.2 12.6c.7.55 1.2 1.3 1.2 2.15V18h6v-1.25c0-.85.5-1.6 1.2-2.15A7 7 0 0 0 12 2ZM9.5 19.5h5V21a2.5 2.5 0 0 1-5 0v-1.5Z",
        ["cloud"] = "M6.5 19a4.5 4.5 0 0 1-.36-8.99 6 6 0 0 1 11.7-1.2A4.25 4.25 0 0 1 17.75 19H6.5Z",
        ["box"] = "M12 2.7 3 7v10l9 4.3L21 17V7l-9-4.3ZM12 5l6.3 3L12 11 5.7 8 12 5ZM5 9.9l6 2.8v6.3l-6-2.9V9.9Zm8 9.1v-6.3l6-2.8v6.2l-6 2.9Z",
        ["tag"] = "M12.6 2.6 21.4 11.4a2 2 0 0 1 0 2.8l-7.2 7.2a2 2 0 0 1-2.8 0L2.6 12.6A2 2 0 0 1 2 11.2V4a2 2 0 0 1 2-2h7.2c.53 0 1.04.21 1.4.6ZM7.5 8.8a1.8 1.8 0 1 0 0-3.6 1.8 1.8 0 0 0 0 3.6Z",
    };

    public static readonly List<IconPackDef> All = new()
    {
        new IconPackDef
        {
            Id = "system", DisplayName = "System (Segoe Fluent)", Kind = "Font",
            FontFamily = "Segoe Fluent Icons",
            Glyphs = new Dictionary<string, string>
            {
                ["folder"] = "\uE8B7", ["code"] = "\uE943", ["globe"] = "\uE774",
                ["document"] = "\uE8A5", ["palette"] = "\uE790", ["game"] = "\uE7FC",
                ["music"] = "\uE8D6", ["chat"] = "\uE8F2", ["terminal"] = "\uE756",
                ["settings"] = "\uE713", ["star"] = "\uE735", ["download"] = "\uE896",
                ["box"] = "\uE7B8", ["layers"] = "\uE81E", ["grid"] = "\uE80A",
                ["lightning"] = "\uE945", ["shield"] = "\uE83D", ["clock"] = "\uE823",
                ["camera"] = "\uE722", ["home"] = "\uE80F", ["bulb"] = "\uEA80",
                ["cloud"] = "\uE753", ["heart"] = "\uEB51", ["tag"] = "\uE8EC",
            }
        },

        new IconPackDef
        {
            Id = "fluent", DisplayName = "Fluent (实心)", Kind = "Path", Filled = true, StrokeThickness = 0,
            Glyphs = new Dictionary<string, string>
            {
                ["code"] = "M7.6 5 2 12l5.6 7 1.6-1.3L5.2 12l4-5.7L7.6 5zM16.4 5 22 12l-5.6 7-1.6-1.3L18.8 12l-4-5.7L16.4 5z",
                ["globe"] = "M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20zm0 3.2c1 0 1.9 1.9 2.2 4.4H9.8c.3-2.5 1.2-4.4 2.2-4.4zM12 18.8c-1 0-1.9-1.9-2.2-4.4h4.4c-.3 2.5-1.2 4.4-2.2 4.4zM4.6 9.6h4.7c.1.8.1 1.6.1 2.4s0 1.6-.1 2.4H4.6a7.9 7.9 0 0 1 0-4.8zM14.7 9.6h4.7a7.9 7.9 0 0 1 0 4.8h-4.7c.1-.8.1-1.6.1-2.4s0-1.6-.1-2.4z",
                ["document"] = "M13.5 2H7a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V7.5L13.5 2zM13 3.5L17.5 8H13V3.5zM8.5 12h7v1.6h-7V12zm0 4h7v1.6h-7V16z",
                ["palette"] = "M12 3a9 9 0 0 0 0 18c1.1 0 2-.9 2-2 0-.5-.2-1-.5-1.3-.3-.4-.5-.8-.5-1.2 0-.9.7-1.6 1.6-1.6H16a5 5 0 0 0 5-5c0-4.1-4-7-9-7zM8 10a1.4 1.4 0 1 1 0 2.8A1.4 1.4 0 0 1 8 10zm3-3.5a1.4 1.4 0 1 1 0 2.8 1.4 1.4 0 0 1 0-2.8zm5 1a1.4 1.4 0 1 1 0 2.8 1.4 1.4 0 0 1 0-2.8z",
                ["game"] = "M8 9h2.5v2.5H13V9h2.5V6.5H13V4h-2.5v2.5H8V9zM6.8 12H5.2a4.8 4.8 0 0 0-4.7 4.1c-.2 1.3-.3 3.6-.3 4.6 0 1.5 1 2.3 2.4 2.3 1 0 1.8-.5 2.4-1.2l1.3-1.4c.4-.4.9-.6 1.5-.6h5.4c.6 0 1.1.2 1.5.6l1.3 1.4c.6.7 1.4 1.2 2.4 1.2 1.4 0 2.4-.8 2.4-2.3 0-1-.1-3.3-.3-4.6A4.8 4.8 0 0 0 18.8 12h-1.6a1.2 1.2 0 0 1 0-2.4h1.6a2.4 2.4 0 0 1 .2 0h.1a4.4 4.4 0 0 0-2.9-4A12 12 0 0 0 14 5.2V5a2 2 0 0 0-2-2h-1a1 1 0 0 0-1 1v1.2a12 12 0 0 0-2.2.4 4.4 4.4 0 0 0-2.9 4 4.4 4.4 0 0 0 .1 1 2.4 2.4 0 0 1 .2 0h1.6a1.2 1.2 0 0 1 0 2.4z",
                ["music"] = "M20 3.5v13a3.5 3.5 0 1 1-2-3.16V7.2l-7 1.4v8.3a3.5 3.5 0 1 1-2-3.16V6.7l11-2.2z",
                ["chat"] = "M12 3c5 0 9 3.4 9 7.6 0 4.2-4 7.6-9 7.6-.9 0-1.8-.1-2.6-.3l-4.2 2a.6.6 0 0 1-.9-.7l1-3.7A7.3 7.3 0 0 1 3 10.6C3 6.4 7 3 12 3z",
                ["terminal"] = "M3.7 4.3A2 2 0 0 0 2 6.2v11.6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2V6.2a2 2 0 0 0-2-2H3.7zM6.4 8.6l3.6 3.4-3.6 3.4-.9-1 2.6-2.4-2.6-2.4.9-1zM12 14.6h6v1.6h-6v-1.6z",
                ["settings"] = "M12 9a3 3 0 1 0 0 6 3 3 0 0 0 0-6zM10.7 2.5a1 1 0 0 1 .9.6l.6 1.5 1.7.5a1 1 0 0 1 .6 1.3l-.7 1.8 1 1.5a1 1 0 0 1-.2 1.3l-1.5 1.1-.1 1.8a1 1 0 0 1-1 1h-2.5a1 1 0 0 1-1-1l-.1-1.8-1.5-1.1a1 1 0 0 1-.2-1.3l1-1.5-.7-1.8a1 1 0 0 1 .6-1.3l1.7-.5.6-1.5a1 1 0 0 1 .9-.6h.9z",
                ["star"] = "M12 2.6l2.9 5.9 6.5.9-4.7 4.6 1.1 6.4-5.8-3-5.8 3 1.1-6.4L2.6 9.4l6.5-.9L12 2.6z",
                ["download"] = "M12 2a1.2 1.2 0 0 1 1.2 1.2v9.2l3-3 1.2 1.2-5.1 5.1a1.2 1.2 0 0 1-1.7 0L5.5 10.6l1.2-1.2 3 3V3.2A1.2 1.2 0 0 1 12 2zM4.2 17.4h15.6v3.2H4.2v-3.2z",
                ["layers"] = "M12 3 2.5 8l9.5 5 9.5-5L12 3ZM4.8 11.7 2.5 12.9 12 17.9l9.5-5-2.3-1.2L12 15.6l-7.2-3.9ZM4.8 16.2l-2.3 1.2L12 22.4l9.5-5-2.3-1.2L12 20.1l-7.2-3.9Z",
            }
        },

        new IconPackDef
        {
            Id = "material", DisplayName = "Material (实心)", Kind = "Path", Filled = true, StrokeThickness = 0,
            Glyphs = new Dictionary<string, string>
            {
                ["folder"] = "M10 4H4a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2h-8l-2-2z",
                ["code"] = "M9.4 16.6 4.8 12l4.6-4.6L8 6l-6 6 6 6 1.4-1.4zM14.6 16.6l4.6-4.6-4.6-4.6L16 6l6 6-6 6-1.4-1.4z",
                ["globe"] = "M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20zm6.9 6h-2.9a15.6 15.6 0 0 0-1.5-3.6A8 8 0 0 1 18.9 8zM12 4.1c.8 1 1.5 2.4 1.9 3.9h-3.8c.4-1.5 1.1-2.9 1.9-3.9zM4.3 14c-.1-.6-.2-1.3-.2-2s.1-1.4.2-2h3.9c.1 1.3.1 2.7 0 4H4.3zm0 2h3.9c.2 1.4.5 2.7.8 3.9-2-.8-3.6-2.2-4.7-3.9zm5.9 0h3.6c-.4 1.5-1.1 2.9-1.9 3.9-.8-1-1.5-2.4-1.9-3.9zm2-6H8.2c-.3-1.4-.5-2.7-.6-3.9 1.1 1 2 2 2.6 3.9zm4.2 6c-.4 1.5-.9 2.8-1.4 3.6a8 8 0 0 1-2.9 0c-.5-.8-1-2.1-1.4-3.6h5.7zm1.4-2h3.9c.1.6.2 1.3.2 2s-.1 1.4-.2 2h-3.9c.1-1.3.1-2.7 0-4zm0-2c-.6-1.9-1.5-2.9-2.6-3.9-.1 1.2-.3 2.5-.6 3.9h3.2z",
                ["document"] = "M6 2h8l6 6v12a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2zm7 1.5V9h5.5L13 3.5zM7 13h10v2H7v-2zm0 4h10v2H7v-2z",
                ["palette"] = "M12 3a9 9 0 0 0 0 18c1 0 2-.8 2-1.8 0-.5-.2-.9-.5-1.2-.3-.4-.5-.7-.5-1.1 0-.9.7-1.6 1.6-1.6h1.9A4.5 4.5 0 0 0 21 11.5C21 7.4 17 3 12 3zM7.5 11.5a1.5 1.5 0 1 1 0-3 1.5 1.5 0 0 1 0 3zm3-4a1.5 1.5 0 1 1 0-3 1.5 1.5 0 0 1 0 3zm5 1a1.5 1.5 0 1 1 0-3 1.5 1.5 0 0 1 0 3z",
                ["game"] = "M21 7H3a1 1 0 0 0-1 1v8a1 1 0 0 0 1 1h18a1 1 0 0 0 1-1V8a1 1 0 0 0-1-1zM11 8H8v3H5v2h3v3h2v-3h3v-2h-3V8zM6.5 12.5a1 1 0 1 1 0 2 1 1 0 0 1 0-2zm11 2a1 1 0 1 1 0 2 1 1 0 0 1 0-2z",
                ["music"] = "M12 3v10.6A4 4 0 1 0 14 17V7h4V3h-6z",
                ["chat"] = "M20 2H4a2 2 0 0 0-2 2v18l4-4h14a2 2 0 0 0 2-2V4a2 2 0 0 0-2-2z",
                ["terminal"] = "M20 4H4a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2V6a2 2 0 0 0-2-2zM7.4 15.4 4 12l3.4-3.4 1.2 1.2L6.4 12l2.4 2.4-1.4 1zM12 15h6v1.6h-6V15z",
                ["settings"] = "M19.4 13a7.7 7.7 0 0 0 0-2l2-1.6-2-3.4-2.4 1a7.6 7.6 0 0 0-1.7-1L15 3H9l-.3 2a7.6 7.6 0 0 0-1.7 1l-2.4-1-2 3.4L4.6 11a7.7 7.7 0 0 0 0 2l-2 1.6 2 3.4 2.4-1c.5.4 1.1.8 1.7 1l.3 2h6l.3-2c.6-.2 1.2-.6 1.7-1l2.4 1 2-3.4L19.4 13zM12 15.5a3.5 3.5 0 1 1 0-7 3.5 3.5 0 0 1 0 7z",
                ["star"] = "M12 17.3 6.2 20.5l1.2-6.6L2.6 9.4l6.6-.9L12 2.5l2.8 6 6.6.9-4.8 4.5 1.2 6.6L12 17.3z",
                ["download"] = "M5 20h14v-2H5v2zM12 2 6.5 7.5 8 9l3-3v10h2V6l3 3 1.5-1.5L12 2z",
            }
        },

        new IconPackDef
        {
            Id = "lucide", DisplayName = "Lucide (线性)", Kind = "Path", Filled = false, StrokeThickness = 2,
            Glyphs = new Dictionary<string, string>
            {
                ["code"] = "m16 18 6-6-6-6M8 6l-6 6 6 6",
                ["globe"] = "M12 21a9 9 0 1 0 0-18 9 9 0 0 0 0 18ZM3.6 9h16.8M3.6 15h16.8M12 3a15.3 15.3 0 0 1 4 9 15.3 15.3 0 0 1-4 9 15.3 15.3 0 0 1-4-9 15.3 15.3 0 0 1 4-9Z",
                ["document"] = "M15 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7ZM14 2v4a2 2 0 0 0 2 2h4M16 13H8M16 17H8M10 9H8",
                ["palette"] = "M12 22a7 7 0 0 0 7-7c0-2-1-3.9-3-5.5s-3.5-4-4-6.5c-.5 2.5-2 4.9-4 6.5C6 11.1 5 13 5 15a7 7 0 0 0 7 7ZM9 12h.01M15 10h.01",
                ["game"] = "M6 12h4m-2-2v4M15 13h.01M18 11h.01M17.32 5H6.68a4 4 0 0 0-3.98 3.59c-.006.052-.01.101-.017.152C2.604 9.416 2 14.456 2 16a3 3 0 0 0 3 3c1 0 1.5-.5 2-1l1.414-1.414A2 2 0 0 1 9.828 16h4.344a2 2 0 0 1 1.414.586L17 18c.5.5 1 1 2 1a3 3 0 0 0 3-3c0-1.545-.604-6.584-.685-7.258A4 4 0 0 0 17.32 5Z",
                ["music"] = "M9 18V5l12-2v13M6 18a3 3 0 1 0 0-6 3 3 0 0 0 0 6ZM18 16a3 3 0 1 0 0-6 3 3 0 0 0 0 6Z",
                ["chat"] = "M7.9 20A9 9 0 1 0 4 16.1L2 22Z",
                ["terminal"] = "m4 17 6-6-6-6M12 19h8",
                ["settings"] = "M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76Z",
                ["star"] = "M11.525 2.295a.53.53 0 0 1 .95 0l2.31 4.679a2.123 2.123 0 0 0 1.595 1.16l5.166.756a.53.53 0 0 1 .294.904l-3.736 3.638a2.123 2.123 0 0 0-.611 1.878l.882 5.14a.53.53 0 0 1-.771.56l-4.618-2.428a2.122 2.122 0 0 0-1.973 0L6.396 21.01a.53.53 0 0 1-.77-.56l.881-5.139a2.122 2.122 0 0 0-.611-1.879L2.16 9.795a.53.53 0 0 1 .294-.904l5.165-.755a2.122 2.122 0 0 0 1.597-1.16Z",
                ["download"] = "M12 15V3M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4M7 10l5 5 5-5",
                ["layers"] = "M12 3 2.5 8 12 13l9.5-5L12 3ZM2.5 12.5 12 17.5l9.5-5M2.5 17 12 22l9.5-5",
            }
        },

        new IconPackDef
        {
            Id = "phosphor", DisplayName = "Phosphor (细线)", Kind = "Path", Filled = false, StrokeThickness = 1.5,
            Glyphs = new Dictionary<string, string>
            {
                ["code"] = "m18 16 4-4-4-4M6 8l-4 4 4 4",
                ["globe"] = "M12 3a9 9 0 1 0 0 18 9 9 0 0 0 0-18ZM3.6 9h16.8M3.6 15h16.8",
                ["document"] = "M14 3H6a1 1 0 0 0-1 1v16a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V8ZM14 3v5h5M9 13h6M9 17h6",
                ["palette"] = "M12 21a6 6 0 1 1 6-6c0-1.7-.9-3.2-2.6-4.7-1.7-1.5-3-3.4-3.4-5.3-.4 2-1.8 3.9-3.5 5.3-1.7 1.4-2.5 3-2.5 4.7a6 6 0 0 1 6 6ZM9 13h.01M15 11h.01",
                ["game"] = "M7 12h4M9 10v4M16 13h.01M18.5 10.5h.01M17.5 6H6.5a3.5 3.5 0 0 0-3.47 3.06C3 9.7 2.5 14 2.5 15.5a2.5 2.5 0 0 0 2.5 2.5c.8 0 1.2-.4 1.7-.9l1.2-1.2a1.5 1.5 0 0 1 1.1-.4h3.9a1.5 1.5 0 0 1 1.1.4l1.2 1.2c.5.5.9.9 1.7.9a2.5 2.5 0 0 0 2.5-2.5c0-1.5-.5-5.8-.53-6.44A3.5 3.5 0 0 0 17.5 6Z",
                ["music"] = "M9 18V5l11-2v13M6 18a3 3 0 1 0 0-6 3 3 0 0 0 0 6ZM17 16a3 3 0 1 0 0-6 3 3 0 0 0 0 6Z",
                ["chat"] = "M20.5 12a8.5 8.5 0 0 1-12.4 7.5L4 21l1.5-3.9A8.5 8.5 0 1 1 20.5 12Z",
                ["terminal"] = "m5 16 5-5-5-5M12 18h7",
                ["settings"] = "M12 15.5a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7ZM12 2.5v3M12 18.5v3M21.5 12h-3M5.5 12h-3M18.7 5.3l-2.1 2.1M7.4 16.6l-2.1 2.1M18.7 18.7l-2.1-2.1M7.4 7.4 5.3 5.3",
                ["star"] = "M12 3.5l2.6 5.3 5.9.9-4.3 4.1 1 5.8-5.2-2.8-5.2 2.8 1-5.8-4.3-4.1 5.9-.9Z",
                ["download"] = "M12 16V4M7 11l5 5 5-5M20 16v3a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1v-3",
                ["layers"] = "M12 3.5 3.5 8 12 12.5 20.5 8 12 3.5ZM3.5 12.5 12 17l8.5-4.5M3.5 17 12 21.5l8.5-4.5",
            }
        },

        // 全新风格：纯几何图形，与前面几套“具象图标”完全不同
        new IconPackDef
        {
            Id = "blocks", DisplayName = "Blocks (几何图形)", Kind = "Path", Filled = true, StrokeThickness = 0,
            Glyphs = new Dictionary<string, string>
            {
                ["folder"] = "M5 3h14a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2Z",
                ["box"] = "M12 2 3 7v10l9 5 9-5V7l-9-5Z",
                ["layers"] = "M3 5h18v4H3zM3 10h18v4H3zM3 15h18v4H3z",
                ["grid"] = "M7.5 3.5a4 4 0 1 0 0 8 4 4 0 0 0 0-8Zm9 0a4 4 0 1 0 0 8 4 4 0 0 0 0-8Zm-9 9a4 4 0 1 0 0 8 4 4 0 0 0 0-8Zm9 0a4 4 0 1 0 0 8 4 4 0 0 0 0-8Z",
                ["globe"] = "M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20Zm0 3.5a6.5 6.5 0 1 1 0 13 6.5 6.5 0 0 1 0-13Z",
                ["home"] = "M12 2 22 9.5 18.2 21H5.8L2 9.5 12 2Z",
                ["code"] = "M12 3 22 21H2L12 3Z",
                ["terminal"] = "M12 2l8.66 5v10L12 22l-8.66-5V7L12 2Z",
                ["document"] = "M9 3h6v18H9z",
                ["palette"] = "M12 2 22 12 12 22 2 12Z",
                ["camera"] = "M12 6.5a5.5 5.5 0 1 0 0 11 5.5 5.5 0 0 0 0-11Z",
                ["music"] = "M2 18a10 10 0 0 1 20 0H2Z",
                ["game"] = "M9 3h6v6h6v6h-6v6H9v-6H3V9h6V3Z",
                ["chat"] = "M3 3h18L3 21V3Z",
                ["download"] = "M12 21 3 12l2.2-2.2L12 16.6l6.8-6.8L21 12 12 21Z",
                ["clock"] = "M12 2a10 10 0 1 0 10 10H12V2Z",
                ["star"] = "M12 2l2.9 6.3 6.9.8-5.1 4.7 1.4 6.8-6.1-3.4-6.1 3.4 1.4-6.8L2.2 9.1l6.9-.8L12 2Z",
                ["shield"] = "M12 2l8 3v8c0 5-4 8-8 9-4-1-8-4-8-9V5l8-3Z",
                ["bulb"] = "M12 2a8 8 0 0 1 8 8c0 3.1-2.1 5-4 7.2H8C6.1 15 4 13.1 4 10a8 8 0 0 1 8-8Z",
                ["tag"] = "M8.5 4H21l-5.5 16H3L8.5 4Z",
                ["settings"] = "M12 4.5a7.5 7.5 0 1 0 0 15 7.5 7.5 0 0 0 0-15Zm0 4a3.5 3.5 0 1 1 0 7 3.5 3.5 0 0 1 0-7Z",
            }
        },

        // 全新风格：系统强调色圆角徽章 + 对比色图形
        new IconPackDef
        {
            Id = "badge", DisplayName = "Badge (彩色徽章)", Kind = "Path", Filled = true,
            StrokeThickness = 0, Badge = true, Base = "fluent",
            Glyphs = new Dictionary<string, string>()
        },

        // 全新风格：手机 UI 风——圆角渐变彩色方块 + 白色实心图形（彩色、实心、非线稿）
        new IconPackDef
        {
            Id = "phone", DisplayName = "Phone (彩色应用图标)", Kind = "Phone", Filled = true,
            StrokeThickness = 0, Glyphs = new Dictionary<string, string>()
        },

        new IconPackDef
        {
            Id = "emoji", DisplayName = "Emoji (彩色)", Kind = "Emoji",
            Glyphs = new Dictionary<string, string>
            {
                ["folder"] = "\U0001F4C1", ["code"] = "\U0001F4BB", ["globe"] = "\U0001F310",
                ["document"] = "\U0001F4DD", ["palette"] = "\U0001F3A8", ["game"] = "\U0001F3AE",
                ["music"] = "\U0001F3B5", ["chat"] = "\U0001F4AC", ["terminal"] = "\U0001F5A5",
                ["settings"] = "⚙️", ["star"] = "⭐", ["download"] = "⬇️",
                ["box"] = "\U0001F4E6", ["layers"] = "\U0001F5C2\uFE0F", ["grid"] = "\U0001F532",
                ["lightning"] = "\u26A1", ["shield"] = "\U0001F6E1\uFE0F", ["clock"] = "\U0001F550",
                ["camera"] = "\U0001F4F7", ["home"] = "\U0001F3E0", ["bulb"] = "\U0001F4A1",
                ["cloud"] = "\u2601\uFE0F", ["heart"] = "\u2764\uFE0F", ["tag"] = "\U0001F3F7\uFE0F",
            }
        },
    };

    public static IconPackDef Get(string id) =>
        All.Find(p => p.Id == id) ?? All[0];

    public static string Glyph(string packId, string key)
    {
        var pack = Get(packId);
        if (pack.Glyphs.TryGetValue(key, out var g)) return g;

        // 回退链：Base 包 → Shared 通用图形 → folder
        if (!string.IsNullOrEmpty(pack.Base))
        {
            var b = Get(pack.Base);
            if (b.Glyphs.TryGetValue(key, out g)) return g;
        }
        if (Shared.TryGetValue(key, out g)) return g;
        if (pack.Glyphs.TryGetValue("folder", out g)) return g;
        if (Shared.TryGetValue("folder", out g)) return g;
        return "";
    }
}
