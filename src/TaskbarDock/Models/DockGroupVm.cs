using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TaskbarDock.Models;

namespace TaskbarDock.Models;

public class DockGroupVm : INotifyPropertyChanged
{
    public DockGroup Group { get; set; }
    public string Id => Group.Id;
    public string Name => Group.Name;
    public string Folder => Group.Folder;
    public string IconPack => Group.IconPack;
    public string IconKey => Group.IconKey;
    public string CustomPath => Group.CustomIconPath;

    /// <summary>图标或目录改了之后刷新列表里的显示。</summary>
    public void Refresh()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Folder));
        OnPropertyChanged(nameof(IconPack));
        OnPropertyChanged(nameof(IconKey));
        OnPropertyChanged(nameof(CustomPath));
    }

    private double _buttonSize = 36;
    public double ButtonSize { get => _buttonSize; set { _buttonSize = value; OnPropertyChanged(); } }

    private double _iconSize = 24;
    public double IconSize { get => _iconSize; set { _iconSize = value; OnPropertyChanged(); } }

    private bool _isOpen;
    public bool IsOpen { get => _isOpen; set { _isOpen = value; OnPropertyChanged(); } }

    public DockGroupVm(DockGroup g) => Group = g;

    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
