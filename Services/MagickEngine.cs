using ImageMagick;

namespace ImageConverter.Services;

public sealed class MagickEngine : IImageEngine
{
    public void Convert(string inputPath, string outputPath, ConvertOptions options)
    {
        // The file's contents decide how it is read, not its extension. Animated files give their first frame.
        using var image = new MagickImage(inputPath);
        MagickProcessing.Prepare(image, options, new MagickColor(options.BackgroundHex));

        // Write to a temporary name first so a failure never leaves a half-written file behind.
        string temp = outputPath + ".part";
        try
        {
            image.Write(temp, options.Target.Format);
            File.Move(temp, outputPath, overwrite: false);
        }
        finally
        {
            if (File.Exists(temp))
            {
                try { File.Delete(temp); } catch { }
            }
        }
    }

    /// <summary>The formats this build of the library can actually read.</summary>
    public static HashSet<MagickFormat> ReadableFormats()
    {
        var readable = new HashSet<MagickFormat>();
        foreach (var info in MagickNET.SupportedFormats)
            if (info.SupportsReading) readable.Add(info.Format);
        return readable;
    }

    /// <summary>The target formats this build of the library can actually write.</summary>
    public static IReadOnlyList<TargetFormat> AvailableTargets()
    {
        var writable = new HashSet<MagickFormat>();
        foreach (var info in MagickNET.SupportedFormats)
            if (info.SupportsWriting) writable.Add(info.Format);

        var list = TargetFormats.All.Where(t => writable.Contains(t.Format)).ToList();
        return list.Count > 0 ? list : TargetFormats.All;
    }
}
