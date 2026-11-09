using System.ComponentModel;

namespace ImageConverter.Services;

public enum ItemState { Waiting, Working, Done, Failed, Skipped }

/// <summary>One file in the list. Shown in the window, so it reports changes.</summary>
public sealed class QueueItem : INotifyPropertyChanged
{
    string _status = "Waiting";
    ItemState _state = ItemState.Waiting;

    public QueueItem(string path, string? root)
    {
        Path = path;
        Root = root;
        Name = System.IO.Path.GetFileName(path);
        Kind = ImageKinds.Label(path);
        try { SizeBytes = new FileInfo(path).Length; } catch { SizeBytes = 0; }
        SizeText = FormatSize(SizeBytes);
    }

    public string Path { get; }
    /// <summary>The folder this file was added from (so sub-folders can be mirrored), or null if added as a single file.</summary>
    public string? Root { get; }
    public string Name { get; }
    public string Kind { get; }
    public long SizeBytes { get; }
    public string SizeText { get; }
    public string? OutputPath { get; set; }

    public string Status
    {
        get => _status;
        set { _status = value; Raise(nameof(Status)); }
    }

    public ItemState State
    {
        get => _state;
        set { _state = value; Raise(nameof(State)); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:0.#} KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / 1024.0 / 1024.0:0.#} MB";
        return $"{bytes / 1024.0 / 1024.0 / 1024.0:0.##} GB";
    }
}
