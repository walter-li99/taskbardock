using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using TaskbarDock.Icons;
using TaskbarDock.Services;

namespace TaskbarDock.Icons;

/// <summary>按图标包渲染：字体包 / 矢量包 / Emoji 包 / 自定义图片。</summary>
public class PackIcon : ContentControl
{
    public static readonly DependencyProperty PackProperty =
        DependencyProperty.Register(nameof(Pack), typeof(string), typeof(PackIcon),
            new PropertyMetadata("system", OnChanged));

    public static readonly DependencyProperty IconKeyProperty =
        DependencyProperty.Register(nameof(IconKey), typeof(string), typeof(PackIcon),
            new PropertyMetadata("folder", OnChanged));

    public static readonly DependencyProperty IconSizeProperty =
        DependencyProperty.Register(nameof(IconSize), typeof(double), typeof(PackIcon),
            new PropertyMetadata(24d, OnChanged));

    public static readonly DependencyProperty CustomPathProperty =
        DependencyProperty.Register(nameof(CustomPath), typeof(string), typeof(PackIcon),
            new PropertyMetadata("", OnChanged));

    public string Pack { get => (string)GetValue(PackProperty); set => SetValue(PackProperty, value); }
    public string IconKey { get => (string)GetValue(IconKeyProperty); set => SetValue(IconKeyProperty, value); }
    public double IconSize { get => (double)GetValue(IconSizeProperty); set => SetValue(IconSizeProperty, value); }
    public string CustomPath { get => (string)GetValue(CustomPathProperty); set => SetValue(CustomPathProperty, value); }

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((PackIcon)d).Build();

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        Build();
    }

    public void Build()
    {
        if (!IsInitialized) return;

        if (!string.IsNullOrWhiteSpace(CustomPath) && File.Exists(CustomPath))
        {
            var img = IconService.LoadImage(CustomPath);
            if (img != null)
            {
                Content = new Image { Source = img, Width = IconSize, Height = IconSize, Stretch = Stretch.Uniform };
                return;
            }
        }

        var pack = IconPacks.Get(Pack);
        var glyph = IconPacks.Glyph(Pack, IconKey);

        if (pack.Kind == "Font")
        {
            Content = new TextBlock
            {
                Text = glyph,
                FontFamily = new FontFamily(pack.FontFamily),
                FontSize = IconSize * 0.66,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };
            BindFg(TextBlock.ForegroundProperty, (TextBlock)Content);
            return;
        }

        if (pack.Kind == "Emoji")
        {
            Content = new TextBlock
            {
                Text = glyph,
                FontFamily = new FontFamily("Segoe UI Emoji"),
                FontSize = IconSize * 0.78,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };
            return;
        }

        // 矢量包
        Geometry geo;
        try { geo = Geometry.Parse(glyph); }
        catch { geo = Geometry.Parse(IconPacks.Glyph(Pack, "folder")); }

        var path = new System.Windows.Shapes.Path
        {
            Data = geo,
            Width = IconSize * 0.78,
            Height = IconSize * 0.78,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        if (pack.Badge)
        {
            // 彩色徽章：系统强调色圆角底 + 按亮度自动选黑/白前景
            var bgBrush = TryFindResource("Br.Accent") as SolidColorBrush
                ?? new SolidColorBrush(Color.FromRgb(0, 0x78, 0xD4));
            var c = bgBrush.Color;
            double lum = (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0;
            path.Fill = lum > 0.62 ? Brushes.Black : Brushes.White;

            var grid = new Grid { Width = IconSize, Height = IconSize };
            grid.Children.Add(new Border
            {
                Background = bgBrush,
                CornerRadius = new CornerRadius(IconSize * 0.28)
            });
            path.Width = IconSize * 0.56;
            path.Height = IconSize * 0.56;
            grid.Children.Add(path);
            Content = grid;
            return;
        }

        if (pack.Filled) BindFg(Shape.FillProperty, path);
        else
        {
            BindFg(Shape.StrokeProperty, path);
            path.StrokeThickness = pack.StrokeThickness;
            path.StrokeLineJoin = PenLineJoin.Round;
            path.StrokeStartLineCap = PenLineCap.Round;
            path.StrokeEndLineCap = PenLineCap.Round;
        }
        Content = path;
    }

    private void BindFg(DependencyProperty dp, FrameworkElement target)
    {
        target.SetBinding(dp, new Binding(nameof(Foreground)) { Source = this });
    }
}
