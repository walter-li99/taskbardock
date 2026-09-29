using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TaskbarDock.Views;

public static class PromptWindow
{
    public static string Show(string title, string defaultValue, Window owner)
    {
        var box = new TextBox
        {
            Text = defaultValue ?? "",
            Margin = new Thickness(0, 10, 0, 0),
            FontSize = 13
        };

        var ok = new Button { Content = "确定", Style = (Style)Application.Current.TryFindResource("FlatButton"), Width = 90, Margin = new Thickness(0, 0, 10, 0) };
        var cancel = new Button { Content = "取消", Style = (Style)Application.Current.TryFindResource("FlatButton"), Width = 90 };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 14, 0, 0)
        };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);

        var root = new StackPanel { Margin = new Thickness(20, 16, 20, 16) };
        root.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = (Brush)Application.Current.TryFindResource("Br.Fg"),
            FontSize = 13
        });
        root.Children.Add(box);
        root.Children.Add(buttons);

        var border = new Border
        {
            Child = root,
            CornerRadius = new CornerRadius(10),
            Background = (Brush)Application.Current.TryFindResource("Br.Menu"),
            BorderBrush = (Brush)Application.Current.TryFindResource("Br.Border"),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(12)
        };
        border.Effect = new System.Windows.Media.Effects.DropShadowEffect
        {
            BlurRadius = 18, ShadowDepth = 0, Opacity = 0.35
        };

        var win = new Window
        {
            Title = title,
            Width = 420,
            SizeToContent = SizeToContent.Height,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = Brushes.Transparent,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ShowInTaskbar = false,
            Content = border,
            Owner = owner
        };

        string result = null;
        ok.Click += (s, e) => { result = box.Text; win.Close(); };
        cancel.Click += (s, e) => win.Close();
        box.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter) { result = box.Text; win.Close(); }
            else if (e.Key == Key.Escape) win.Close();
        };

        win.Loaded += (s, e) => { box.SelectAll(); box.Focus(); };
        win.ShowDialog();
        return result;
    }
}
