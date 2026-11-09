namespace ImageConverter.Services;

/// <param name="Target">The output format.</param>
/// <param name="Quality">1 to 100, used by formats that have a quality setting.</param>
/// <param name="BackgroundHex">Colour that transparent areas are flattened onto when the target has no transparency.</param>
/// <param name="RemovePrivateData">Remove camera, location and other embedded EXIF/XMP/IPTC data (colour profile is kept).</param>
public sealed record ConvertOptions(
    TargetFormat Target,
    int Quality,
    string BackgroundHex,
    bool RemovePrivateData);
