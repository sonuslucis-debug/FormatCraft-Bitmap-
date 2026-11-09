namespace ImageConverter.Services;

public static class FileScanner
{
    /// <summary>
    /// Turns dropped or chosen paths into queue items. Files are taken as given (even with an unusual extension);
    /// folders are scanned for known bitmap formats, optionally including sub-folders.
    /// </summary>
    public static List<QueueItem> Expand(IEnumerable<string> paths, bool includeSubfolders, ISet<string>? alreadyQueued = null)
    {
        var result = new List<QueueItem>();
        var seen = new HashSet<string>(alreadyQueued ?? new HashSet<string>(), StringComparer.OrdinalIgnoreCase);

        void Add(string file, string? root)
        {
            if (seen.Add(file)) result.Add(new QueueItem(file, root));
        }

        foreach (string path in paths)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    var options = new EnumerationOptions
                    {
                        RecurseSubdirectories = includeSubfolders,
                        IgnoreInaccessible = true   // skip folders we aren't allowed to read instead of giving up
                    };
                    var files = Directory.EnumerateFiles(path, "*", options)
                        .Where(ImageKinds.IsImage)
                        .OrderBy(f => f, StringComparer.OrdinalIgnoreCase);
                    foreach (string file in files) Add(file, path);
                }
                else if (File.Exists(path))
                {
                    Add(path, null);
                }
            }
            catch (UnauthorizedAccessException) { /* a folder we aren't allowed to read: skip it */ }
            catch (IOException) { }
        }
        return result;
    }
}
