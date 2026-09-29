using System.Collections.Generic;

namespace TaskbarDock.Models;

public enum IconPackKind { Font, Path, Emoji }

public class DockGroup
{
    public string Id { get; set; } = System.Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "分组";
    public string Folder { get; set; } = "";
    public string IconPack { get; set; } = "system";
    public string IconKey { get; set; } = "folder";
    public string CustomIconPath { get; set; } = "";
    public List<string> Order { get; set; } = new();
    public Dictionary<string, string> Aliases { get; set; } = new();
    public List<string> Hidden { get; set; } = new();
    public bool SortByCustom { get; set; } = true;
}

public class AppConfig
{
    public List<DockGroup> Groups { get; set; } = new();
    public string ThemeMode { get; set; } = "System";
    public bool UseAcrylic { get; set; } = true;
    public bool MatchTaskbarColor { get; set; } = true;
    public string Align { get; set; } = "Left";
    public int IconSize { get; set; } = 40;
    public double MenuWidth { get; set; } = 300;
    public double MenuMaxHeight { get; set; } = 520;
    public bool ShowItemIcons { get; set; } = true;
    public int ItemIconSize { get; set; } = 20;
    public int CornerRadius { get; set; } = 8;
    public bool StartWithWindows { get; set; } = true;
    public bool ShowTrayIcon { get; set; } = true;
    public string Language { get; set; } = "zh-CN";
}
