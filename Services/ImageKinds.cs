namespace ImageConverter.Services;

/// <summary>Which files count as bitmap images when a folder is added or files are dropped.</summary>
public static class ImageKinds
{
    /// <summary>Bitmap formats picked up when scanning folders.</summary>
    public static readonly HashSet<string> Readable = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".jpe", ".jfif", ".png", ".webp", ".heic", ".heif", ".avif", ".gif", ".bmp",
        ".tif", ".tiff", ".ico", ".tga", ".psd", ".dds", ".jp2", ".j2k", ".jxl", ".pcx",
        ".ppm", ".pgm", ".pbm", ".pnm", ".hdr"
    };

    /// <summary>Vector and document formats: this tool is for bitmaps only, so these are skipped.</summary>
    public static readonly HashSet<string> Excluded = new(StringComparer.OrdinalIgnoreCase)
    {
        ".svg", ".svgz", ".pdf", ".eps", ".ai", ".ps", ".emf", ".wmf"
    };

    public static bool IsImage(string path) => Readable.Contains(Path.GetExtension(path));

    public static bool IsExcluded(string path) => Excluded.Contains(Path.GetExtension(path));

    /// <summary>A short label for the list, e.g. "WEBP".</summary>
    public static string Label(string path)
    {
        string ext = Path.GetExtension(path).TrimStart('.').ToUpperInvariant();
        return ext.Length == 0 ? "?" : ext;
    }
}
