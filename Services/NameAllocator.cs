namespace ImageConverter.Services;

/// <summary>Picks output file names that never overwrite anything: "photo.jpg", then "photo (2).jpg", and so on.</summary>
public sealed class NameAllocator
{
    readonly HashSet<string> _reserved = new(StringComparer.OrdinalIgnoreCase);

    public string Allocate(string directory, string baseName, string extension)
    {
        string candidate = Path.Combine(directory, baseName + extension);
        int n = 2;
        while (_reserved.Contains(candidate) || File.Exists(candidate))
            candidate = Path.Combine(directory, $"{baseName} ({n++}){extension}");
        _reserved.Add(candidate);
        return candidate;
    }
}
