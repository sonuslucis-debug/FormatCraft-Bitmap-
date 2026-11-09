# Sonus Lucis Image Converter

A fast, standalone Windows program for converting bitmap images from one format to another. Drop in files or whole folders, pick a target format, and convert them all at once.

## Features

- **Drag and drop.** Drop files or folders onto the window, or use **Add files** and **Add folder**. Folders can include their subfolders.
- **Reads almost any bitmap format.** WEBP, HEIC/HEIF (reading only), JPG, PNG, GIF, BMP, TIFF, ICO, TGA, PSD (flattened), AVIF, DDS, JPEG 2000 and more. Files are recognised by their contents, not just their extension.
- **Converts to** JPG, PNG, WebP, TIFF, BMP, GIF and AVIF.
- **Quality control** for JPG, WebP and AVIF.
- **Transparency handled properly.** Converting a transparent image to JPG or BMP flattens it onto white, black or grey instead of leaving black patches.
- **Photos stay the right way up.** The EXIF rotation is applied to the pixels, so phone photos aren't turned sideways.
- **Colour-safe.** CMYK images are converted to sRGB, and embedded colour profiles are kept.
- **Privacy option.** Remove camera, location and other embedded data (EXIF) while keeping the colour profile.
- **Fast.** Batches are converted on several CPU cores at once, with live progress and speed.
- **Safe.** Originals are never touched or overwritten. Name clashes get a number: `photo.jpg`, `photo (2).jpg`.
- **Your choice of output.** Save next to the originals, or into a folder you choose (sub-folder structure is kept).

## Notes

- This tool is for bitmap images only. Vector and document files (SVG, PDF, EPS, AI) are skipped.
- Animated files (GIF, animated WebP) convert their first frame only.
- HEIC files can be read but not written.
- Everything happens on your own computer. No image is uploaded anywhere.

## Requirements

Windows 10 or 11, 64-bit.

## Building from source

The project builds automatically on GitHub: upload it to a repository and the workflow in `.github/workflows/build.yml` produces `ImageConverter.exe` under **Actions**. To build it yourself, install the .NET 8 SDK and run:

```
dotnet publish ImageConverter.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Credits

- **Created by Sonus Lucis**: concept, design, direction, logos and artwork, and testing.
- **Programmed by Claude**, made by Anthropic: all of the code.
- **Image processing:** [ImageMagick](https://imagemagick.org), through the [Magick.NET](https://github.com/dlemstra/Magick.NET) library (Apache License 2.0).
- **Typeface:** Montserrat, by The Montserrat Project Authors, under the SIL Open Font License 1.1 (see `Fonts/OFL.txt`).

[sonuslucis.com](https://sonuslucis.com)
