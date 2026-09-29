using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TaskbarDock.Models;

namespace TaskbarDock.Services;

public static class ConfigService
{
    public static readonly string AppDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TaskbarDock");

    public static readonly string ConfigPath = Path.Combine(AppDir, "config.json");
    public static readonly string LogPath = Path.Combine(AppDir, "log.txt");

    private static readonly JsonSerializerOptions Opts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static AppConfig Config { get; private set; } = new();

    public static void Load()
    {
        try
        {
            Directory.CreateDirectory(AppDir);
            if (File.Exists(ConfigPath))
            {
                var text = File.ReadAllText(ConfigPath);
                Config = JsonSerializer.Deserialize<AppConfig>(text, Opts) ?? new AppConfig();
            }
        }
        catch (Exception ex)
        {
            Log("加载配置失败: " + ex.Message);
            Config = new AppConfig();
        }

        Config.Groups ??= new List<DockGroup>();
        if (Config.Groups.Count == 0)
        {
            Config.Groups.Add(new DockGroup
            {
                Name = "我的工具",
                Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop")
            });
        }
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(AppDir);
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(Config, Opts));
        }
        catch (Exception ex)
        {
            Log("保存配置失败: " + ex.Message);
        }
    }

    public static void Log(string msg)
    {
        try
        {
            Directory.CreateDirectory(AppDir);
            File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}\r\n");
        }
        catch { }
    }
}
