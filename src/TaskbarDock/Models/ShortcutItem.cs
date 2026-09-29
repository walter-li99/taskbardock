using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace TaskbarDock.Models;

public class ShortcutItem : INotifyPropertyChanged
{
    public string Id { get; set; }
    public string FilePath { get; set; }
    public string TargetPath { get; set; }
    public string Arguments { get; set; }
    public string WorkingDirectory { get; set; }
    public string Description { get; set; }
    public string DisplayName { get; set; }
    public ImageSource Icon { get; set; }
    public bool IsFolder { get; set; }

    public System.Windows.Visibility IconsVisibility { get; set; } = System.Windows.Visibility.Visible;

    private bool _isDragging;
    public bool IsDragging
    {
        get => _isDragging;
        set { _isDragging = value; OnPropertyChanged(); }
    }

    private bool _isDropTarget;
    public bool IsDropTarget
    {
        get => _isDropTarget;
        set { _isDropTarget = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
