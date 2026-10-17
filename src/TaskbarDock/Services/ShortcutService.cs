using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TaskbarDock.Models;
using TaskbarDock.Native;
using static TaskbarDock.Native.Win32;

namespace TaskbarDock.Services;

public static class ShortcutService
{
    public class LinkInfo
    {
        public string TargetPath = "";
        public string Arguments = "";
        public string WorkingDirectory = "";
        public string Description = "";
        public string IconLocation = "";
        public int IconIndex;
    }

    public static LinkInfo Resolve(string lnkPath)
    {
        var info = new LinkInfo();
        try
        {
            var link = (Win32.IShellLinkW)new Win32.ShellLink();
            var pf = (Win32.IPersistFile)link;
            pf.Load(lnkPath, 0);
            link.Resolve(IntPtr.Zero, 0x1);

            var sb = new StringBuilder(260);
            link.GetPath(sb, sb.Capacity, IntPtr.Zero, 0);
            info.TargetPath = sb.ToString();

            sb = new StringBuilder(260);
            link.GetDescription(sb, sb.Capacity);
            info.Description = sb.ToString();

            sb = new StringBuilder(260);
            link.GetArguments(sb, sb.Capacity);
            info.Arguments = sb.ToString();

            sb = new StringBuilder(260);
            link.GetWorkingDirectory(sb, sb.Capacity);
            info.WorkingDirectory = sb.ToString();

            sb = new StringBuilder(260);
            link.GetIconLocation(sb, sb.Capacity, out var idx);
            info.IconLocation = sb.ToString();
            info.IconIndex = idx;

            Marshal.FinalReleaseComObject(link);
        }
        catch (Exception ex)
        {
            ConfigService.Log("解析快捷方式失败 " + lnkPath + " : " + ex.Message);
        }
        return info;
    }

    public static List<ShortcutItem> Scan(string folder, DockGroup group)
    {
        var list = new List<ShortcutItem>();
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder)) return list;

        var cfg = ConfigService.Config;
        var files = new List<string>();
        var dirs = new List<string>();
        try
        {
            // 默认显示文件夹里的所有文件；关闭该选项时才只认快捷方式和程序
            if (cfg.ShowAllFiles)
                files.AddRange(Directory.GetFiles(folder, "*", SearchOption.TopDirectoryOnly));
            else
            {
                files.AddRange(Directory.GetFiles(folder, "*.lnk", SearchOption.TopDirectoryOnly));
                files.AddRange(Directory.GetFiles(folder, "*.url", SearchOption.TopDirectoryOnly));
                files.AddRange(Directory.GetFiles(folder, "*.exe", SearchOption.TopDirectoryOnly));
                files.AddRange(Directory.GetFiles(folder, "*.appref-ms", SearchOption.TopDirectoryOnly));
            }

            if (cfg.IncludeSubfolders)
                dirs.AddRange(Directory.GetDirectories(folder, "*", SearchOption.TopDirectoryOnly));
        }
        catch (Exception ex)
        {
            ConfigService.Log("扫描目录失败: " + ex.Message);
        }

        var hidden = new HashSet<string>(group.Hidden ?? new List<string>(), StringComparer.OrdinalIgnoreCase);

        foreach (var f in files.Where(IsVisible).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var id = Path.GetFileNameWithoutExtension(f);
            if (hidden.Contains(id)) continue;

            var item = new ShortcutItem
            {
                Id = id,
                FilePath = f,
                DisplayName = group.Aliases != null && group.Aliases.TryGetValue(id, out var alias) && !string.IsNullOrWhiteSpace(alias)
                    ? alias : Path.GetFileNameWithoutExtension(f)
            };

            if (Path.GetExtension(f).Equals(".lnk", StringComparison.OrdinalIgnoreCase))
            {
                var info = Resolve(f);
                item.TargetPath = info.TargetPath;
                item.Arguments = info.Arguments;
                item.WorkingDirectory = info.WorkingDirectory;
                item.Description = info.Description;
                item.Icon = IconService.GetIcon(
                    string.IsNullOrWhiteSpace(info.IconLocation) ? info.TargetPath : info.IconLocation,
                    info.IconIndex, string.IsNullOrWhiteSpace(info.TargetPath) ? f : info.TargetPath);
            }
            else
            {
                item.TargetPath = f;
                item.Icon = IconService.GetIcon(f, 0, f);
            }

            list.Add(item);
        }

        // 子文件夹也作为菜单项列出：点它就用资源管理器打开
        foreach (var d in dirs.Where(IsDirVisible).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var id = Path.GetFileName(d);
            if (string.IsNullOrEmpty(id) || hidden.Contains(id)) continue;
            list.Add(new ShortcutItem
            {
                Id = id,
                FilePath = d,
                TargetPath = d,
                IsFolder = true,
                DisplayName = group.Aliases != null && group.Aliases.TryGetValue(id, out var alias) && !string.IsNullOrWhiteSpace(alias)
                    ? alias : id,
                Icon = IconService.GetIcon(d, 0, d)
            });
        }

        // 排序：自定义顺序优先，其余按名称
        if (group.SortByCustom && group.Order != null && group.Order.Count > 0)
        {
            var idx = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < group.Order.Count; i++) idx[group.Order[i]] = i;
            list = list
                .OrderBy(x => idx.TryGetValue(x.Id, out var i) ? i : int.MaxValue)
                .ThenBy(x => x.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
        else
        {
            list = list.OrderBy(x => x.DisplayName, StringComparer.CurrentCultureIgnoreCase).ToList();
        }

        return list;
    }

    /// <summary>系统自带的杂项文件不进菜单。</summary>
    private static readonly HashSet<string> SkipNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "desktop.ini", "thumbs.db", ".ds_store", "ehthumbs.db", "iconcache.db"
    };

    private static bool IsVisible(string path)
    {
        try
        {
            var name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(name) || SkipNames.Contains(name)) return false;
            if (name.StartsWith("~$") || name.StartsWith(".")) return false;

            var attr = File.GetAttributes(path);
            if ((attr & FileAttributes.Hidden) != 0) return false;
            if ((attr & FileAttributes.System) != 0) return false;
            return true;
        }
        catch { return false; }
    }

    private static bool IsDirVisible(string path)
    {
        try
        {
            var name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(name) || name.StartsWith(".")) return false;
            var attr = File.GetAttributes(path);
            if ((attr & FileAttributes.Hidden) != 0) return false;
            if ((attr & FileAttributes.System) != 0) return false;
            return true;
        }
        catch { return false; }
    }

    public static void Launch(ShortcutItem item)
    {
        try
        {
            var wd = string.IsNullOrWhiteSpace(item.WorkingDirectory)
                ? Path.GetDirectoryName(item.FilePath)
                : item.WorkingDirectory;

            ConfigService.Log("正在启动: " + item.FilePath + " (工作目录: " + wd + ")");

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = item.FilePath,
                UseShellExecute = true,
                WorkingDirectory = wd
            };
            var proc = System.Diagnostics.Process.Start(psi);
            ConfigService.Log("已创建进程: " + item.FilePath + " PID=" + (proc?.Id.ToString() ?? "?"));

            // 异步观察：单实例程序（Electron 应用居多）的第二个实例会把窗口交给已有实例后秒退。
            // 3 秒后若进程已退出，记录退出码并尝试把已有实例的窗口带到前台，
            // 避免用户以为「点了没启动」。
            ObserveAfterStart(proc, item);
        }
        catch (Exception ex)
        {
            var code = ex is System.ComponentModel.Win32Exception we ? we.NativeErrorCode : 0;
            ConfigService.Log("启动失败 " + item.FilePath + " : " + ex.Message + " (错误码 " + code + ")");
            System.Windows.MessageBox.Show("无法启动：\n" + item.FilePath + "\n\n" + ex.Message,
                "任务栏整合", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
    }

    private static async void ObserveAfterStart(System.Diagnostics.Process proc, ShortcutItem item)
    {
        try
        {
            if (proc == null) return;
            await System.Threading.Tasks.Task.Delay(3000);
            proc.Refresh();
            if (!proc.HasExited)
            {
                ConfigService.Log("进程仍在运行: " + item.FilePath + " PID=" + proc.Id);
                return;
            }
            var exitCode = -1;
            try { exitCode = proc.ExitCode; } catch { }
            ConfigService.Log("进程 3 秒内退出: " + item.FilePath + " PID=" + proc.Id + " 退出码=" + exitCode
                              + "（单实例程序激活已有实例 / 被拦截 / 目标失效都有可能）");
            ActivateExistingInstance(proc.ProcessName, item.DisplayName);
        }
        catch (Exception ex)
        {
            ConfigService.Log("观察启动结果失败: " + ex.Message);
        }
    }

    /// <summary>按进程名找到已运行的实例，把它的主窗口恢复到前台。</summary>
    private static void ActivateExistingInstance(string processName, string displayName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(processName)) return;
            foreach (var p in System.Diagnostics.Process.GetProcessesByName(processName))
            {
                IntPtr h = p.MainWindowHandle;
                if (h == IntPtr.Zero) continue;
                ShowWindow(h, SW_RESTORE);
                if (!SetForegroundWindow(h))
                {
                    // 前台限制：借输入线程的输入队列把窗口带上来
                    uint targetThread = GetWindowThreadProcessId(h, out _);
                    uint selfThread = GetWindowThreadProcessId(Win32.GetForegroundWindow(), out _);
                    if (targetThread != 0 && selfThread != 0 && targetThread != selfThread)
                        AttachThreadInput(selfThread, targetThread, true);
                    SetForegroundWindow(h);
                    if (targetThread != 0 && selfThread != 0 && targetThread != selfThread)
                        AttachThreadInput(selfThread, targetThread, false);
                }
                ConfigService.Log("已激活已有实例窗口: " + displayName + " (PID " + p.Id + ")");
                return;
            }
        }
        catch (Exception ex)
        {
            ConfigService.Log("激活已有实例失败: " + ex.Message);
        }
    }
}

public static class IconService
{
    private static readonly Dictionary<string, ImageSource> Cache = new();

    public static ImageSource GetIcon(string path, int index, string fallback)
    {
        if (string.IsNullOrWhiteSpace(path)) path = fallback;
        if (string.IsNullOrWhiteSpace(path)) return null;

        var key = path + "|" + index;
        if (Cache.TryGetValue(key, out var cached)) return cached;

        ImageSource src = null;
        try
        {
            if (index > 0 || (!Path.GetExtension(path).Equals(".exe", StringComparison.OrdinalIgnoreCase)
                              && !Path.GetExtension(path).Equals(".dll", StringComparison.OrdinalIgnoreCase)
                              && !Path.GetExtension(path).Equals(".ico", StringComparison.OrdinalIgnoreCase)))
            {
                src = ExtractViaShell(path);
            }
            else
            {
                src = ExtractViaShell(path) ?? ExtractIconFile(path);
            }
        }
        catch { }

        src ??= ExtractViaShell(fallback);
        if (src != null && src.CanFreeze) src.Freeze();
        Cache[key] = src;
        return src;
    }

    private static ImageSource ExtractViaShell(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        var shfi = new Win32.SHFILEINFO();
        var flags = Win32.SHGFI_ICON | Win32.SHGFI_LARGEICON;
        if (!Path.Exists(path) && !path.StartsWith("shell:", StringComparison.OrdinalIgnoreCase))
            flags |= Win32.SHGFI_USEFILEATTRIBUTES;

        var res = Win32.SHGetFileInfo(path, Win32.FILE_ATTRIBUTE_NORMAL, ref shfi,
            (uint)Marshal.SizeOf<Win32.SHFILEINFO>(), flags);
        if (res == IntPtr.Zero || shfi.hIcon == IntPtr.Zero) return null;

        try
        {
            var bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                shfi.hIcon, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            if (bs.CanFreeze) bs.Freeze();
            return bs;
        }
        finally
        {
            Win32.DestroyIcon(shfi.hIcon);
        }
    }

    private static ImageSource ExtractIconFile(string path)
    {
        try
        {
            Win32.ExtractIconEx(path, 0, out var large, out var small, 1);
            var h = large != IntPtr.Zero ? large : small;
            if (h == IntPtr.Zero) return null;
            var bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                h, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            if (large != IntPtr.Zero) Win32.DestroyIcon(large);
            if (small != IntPtr.Zero) Win32.DestroyIcon(small);
            if (bs.CanFreeze) bs.Freeze();
            return bs;
        }
        catch { return null; }
    }

    public static ImageSource LoadImage(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(path, UriKind.Absolute);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.DecodePixelWidth = 64;
            bmp.EndInit();
            if (bmp.CanFreeze) bmp.Freeze();
            return bmp;
        }
        catch { return null; }
    }
}
