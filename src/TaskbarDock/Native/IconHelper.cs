using System;
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
            var info = new ICONINFO
            {
                fIcon = 1,
                xHotspot = 0,
                yHotspot = 0,
                hbmMask = IntPtr.Zero,
                hbmColor = hbm
            };
            var hicon = CreateIconIndirect(ref info);
            DeleteObject(hbm);
            return hicon;
        }
        finally { Marshal.FreeHGlobal(ptr); }
    }
}
