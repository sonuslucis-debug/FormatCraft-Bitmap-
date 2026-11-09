using ImageMagick;

namespace ImageConverter.Services;

/// <summary>An output format the user can pick, with the options that make sense for it.</summary>
public sealed record TargetFormat(
    string Name,
    string Extension,
    MagickFormat Format,
    bool HasQuality,
    bool SupportsAlpha,
    int DefaultQuality)
{
    public override string ToString() => Name;
}

public static class TargetFormats
{
    /// <summary>Every format the app offers (the list shown is filtered to what the library can write).</summary>
    public static readonly IReadOnlyList<TargetFormat> All = new[]
    {
        new TargetFormat("JPG", ".jpg", MagickFormat.Jpeg, HasQuality: true,  SupportsAlpha: false, DefaultQuality: 100),
        new TargetFormat("PNG", ".png", MagickFormat.Png,  HasQuality: false, SupportsAlpha: true,  DefaultQuality: 100),
        new TargetFormat("WebP", ".webp", MagickFormat.WebP, HasQuality: true, SupportsAlpha: true, DefaultQuality: 85),
        new TargetFormat("TIFF", ".tif", MagickFormat.Tiff, HasQuality: false, SupportsAlpha: true, DefaultQuality: 100),
        new TargetFormat("BMP", ".bmp", MagickFormat.Bmp,  HasQuality: false, SupportsAlpha: false, DefaultQuality: 100),
        new TargetFormat("GIF", ".gif", MagickFormat.Gif,  HasQuality: false, SupportsAlpha: true,  DefaultQuality: 100),
        new TargetFormat("AVIF", ".avif", MagickFormat.Avif, HasQuality: true, SupportsAlpha: true, DefaultQuality: 60),
    };
}
