using ImageMagick;

namespace ImageConverter.Services;

/// <summary>Friendly names for the input formats shown on the drop box.</summary>
public static class SupportedFormats
{
    static readonly (string Label, MagickFormat[] Formats)[] Known =
    {
        ("WebP", new[] { MagickFormat.WebP }),
        ("HEIC/HEIF", new[] { MagickFormat.Heic, MagickFormat.Heif }),
        ("JPG", new[] { MagickFormat.Jpeg, MagickFormat.Jpg }),
        ("PNG", new[] { MagickFormat.Png }),
        ("GIF", new[] { MagickFormat.Gif }),
        ("BMP", new[] { MagickFormat.Bmp }),
        ("TIFF", new[] { MagickFormat.Tiff }),
        ("ICO", new[] { MagickFormat.Ico }),
        ("TGA", new[] { MagickFormat.Tga }),
        ("PSD", new[] { MagickFormat.Psd }),
        ("AVIF", new[] { MagickFormat.Avif }),
        ("DDS", new[] { MagickFormat.Dds }),
        ("JPEG 2000", new[] { MagickFormat.Jp2 }),
        ("JPEG XL", new[] { MagickFormat.Jxl }),
        ("PCX", new[] { MagickFormat.Pcx }),
        ("PNM", new[] { MagickFormat.Pnm }),
        ("HDR", new[] { MagickFormat.Hdr }),
    };

    /// <summary>Shown if the image engine can't be asked what it supports.</summary>
    public const string Fallback = "WebP \u00B7 HEIC/HEIF \u00B7 JPG \u00B7 PNG \u00B7 GIF \u00B7 BMP \u00B7 TIFF \u00B7 ICO \u00B7 TGA \u00B7 PSD \u00B7 AVIF \u00B7 and more";

    /// <summary>Only the formats the library really reports it can read, so the list is never over-promising.</summary>
    public static string Describe(ISet<MagickFormat> readable) =>
        string.Join(" \u00B7 ", Known.Where(k => k.Formats.Any(readable.Contains)).Select(k => k.Label));
}
