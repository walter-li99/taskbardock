using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using TaskbarDock.Icons;
using static TaskbarDock.Native.Win32;

namespace TaskbarDock.Native;

/// <summary>把 WPF 图标渲染成真正的 HICON，用于设置任务栏按钮图标。</summary>
public static class IconHelper
{
    /// <param name="pack">图标包 id</param>
    /// <param name="key">图标 key</param>
    /// <param name="customPath">自定义图片/图标路径，可为空</param>
    /// <param name="fg">前景色</param>
    /// <param name="size">像素尺寸</param>
    public static IntPtr Build(string pack, string key, string customPath, Color fg, int size)
    {
        size = Math.Max(16, size);
        try
        {
            var grid = new System.Windows.Controls.Grid
            {
                Width = size,
                Height = size,
                Background = Brushes.Transparent,
                UseLayoutRounding = false,
                SnapsToDevicePixels = false
            };

            var icon = new PackIcon
            {
                Pack = pack,
                IconKey = key,
                CustomPath = customPath ?? "",
                IconSize = size,
                Foreground = new SolidColorBrush(fg),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            icon.BeginInit();
            icon.EndInit();

            grid.Children.Add(icon);
            grid.Measure(new Size(size, size));
            grid.Arrange(new Rect(0, 0, size, size));
            grid.UpdateLayout();

            return ToHIcon(grid, size);
        }
        catch (Exception ex)
        {
            Services.ConfigService.Log("生成图标失败: " + ex.Message);
            return IntPtr.Zero;
        }
    }

    /// <summary>把 HICON 保存为 Vista+ 支持的 PNG-in-ICO 文件，用于任务栏 RelaunchIconResource。</summary>
    public static string SaveToIco(IntPtr hIcon, string path)
    {
        if (hIcon == IntPtr.Zero) return null;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var icon = System.Drawing.Icon.FromHandle(hIcon);
            using var bmp = icon.ToBitmap();
            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            var png = ms.ToArray();

            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            // ICONDIR
            fs.WriteByte(0); fs.WriteByte(0);              // Reserved
            fs.WriteByte(1); fs.WriteByte(0);              // Type: icon
            fs.WriteByte(1); fs.WriteByte(0);              // Count
            // ICONDIRENTRY
            int w = bmp.Width, h = bmp.Height;
            fs.WriteByte((byte)(w > 255 ? 0 : w));
            fs.WriteByte((byte)(h > 255 ? 0 : h));
            fs.WriteByte(0);                               // Colors
            fs.WriteByte(0);                               // Reserved
            fs.WriteByte(1); fs.WriteByte(0);              // Planes
            fs.WriteByte(32); fs.WriteByte(0);             // Bit count
            fs.Write(BitConverter.GetBytes(png.Length), 0, 4);
            fs.Write(BitConverter.GetBytes(6 + 16), 0, 4); // Offset to data
            fs.Write(png, 0, png.Length);
            return path;
        }
        catch (Exception ex)
        {
            Services.ConfigService.Log("保存 ICO 失败: " + ex.Message);
            return null;
        }
    }

    private static IntPtr ToHIcon(Visual visual, int size)
    {
        var rtb = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(visual);

        var conv = new FormatConvertedBitmap(rtb, PixelFormats.Bgra32, null, 0);
        int w = conv.PixelWidth, h = conv.PixelHeight, stride = w * 4;
        if (w <= 0 || h <= 0) return IntPtr.Zero;

        var src = new byte[stride * h];
        conv.CopyPixels(src, stride, 0);

        // GDI 位图是自下而上的，翻转行序
        var dst = new byte[src.Length];
        for (int y = 0; y < h; y++)
            Buffer.BlockCopy(src, y * stride, dst, (h - 1 - y) * stride, stride);

        var ptr = Marshal.AllocHGlobal(dst.Length);
        try
        {
            Marshal.Copy(dst, 0, ptr, dst.Length);
            var hbm = CreateBitmap(w, h, 1, 32, ptr);
            if (hbm == IntPtr.Zero) return IntPtr.Zero;

            // CreateIconIndirect 需要一张单色 AND 掩码（全 0 表示不透明）。
            int maskStride = ((w + 31) / 32) * 4;
            var mask = new byte[maskStride * h];
            var maskPtr = Marshal.AllocHGlobal(mask.Length);
            IntPtr hbmMask;
            try
            {
                Marshal.Copy(mask, 0, maskPtr, mask.Length);
                hbmMask = CreateBitmap(w, h, 1, 1, maskPtr);
            }
            finally { Marshal.FreeHGlobal(maskPtr); }

            if (hbmMask == IntPtr.Zero) { DeleteObject(hbm); return IntPtr.Zero; }

            var info = new ICONINFO
            {
                fIcon = 1,
                xHotspot = 0,
                yHotspot = 0,
                hbmMask = hbmMask,
                hbmColor = hbm
            };
            var hicon = CreateIconIndirect(ref info);
            DeleteObject(hbm);
            DeleteObject(hbmMask);
            return hicon;
        }
        finally { Marshal.FreeHGlobal(ptr); }
    }
}
