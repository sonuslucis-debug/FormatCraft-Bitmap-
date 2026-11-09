using ImageMagick;

namespace ImageConverter.Services;

/// <summary>The image adjustments applied before writing, written against Magick.NET's interfaces.</summary>
public static class MagickProcessing
{
    /// <param name="background">Used only to flatten transparency for formats that can't store it.</param>
    public static void Prepare(IMagickImage<byte> image, ConvertOptions options, IMagickColor<byte> background)
    {
        // Bake the EXIF rotation into the pixels, so photos stay the right way up even with the EXIF data removed.
        image.AutoOrient();

        // Print-style CMYK files become sRGB so they look right on screen and in other formats.
        if (image.ColorSpace == ColorSpace.CMYK)
            image.TransformColorSpace(ColorProfiles.USWebCoatedSWOP, ColorProfiles.SRGB);

        // JPG and BMP can't hold transparency: composite onto the chosen background instead of leaving black.
        if (image.HasAlpha && !options.Target.SupportsAlpha)
        {
            image.BackgroundColor = background;
            image.Alpha(AlphaOption.Remove);
        }

        // Privacy: drop camera/GPS/software data but keep the ICC colour profile so colours stay correct.
        if (options.RemovePrivateData)
        {
            image.RemoveProfile("exif");
            image.RemoveProfile("xmp");
            image.RemoveProfile("iptc");
        }

        if (options.Target.HasQuality)
            image.Quality = (uint)Math.Clamp(options.Quality, 1, 100);
    }
}
