namespace ImageConverter.Services;

/// <summary>Reads one image file and writes it in the target format. Kept behind an interface so the batch logic can be tested.</summary>
public interface IImageEngine
{
    void Convert(string inputPath, string outputPath, ConvertOptions options);
}
