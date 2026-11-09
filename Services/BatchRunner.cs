using System.Diagnostics;

namespace ImageConverter.Services;

public sealed record BatchSummary(
    int Converted, int Failed, int Skipped,
    long BytesIn, long BytesOut,
    TimeSpan Elapsed, bool Cancelled,
    string? FirstOutputFolder);

/// <summary>Converts a list of files in parallel across the CPU cores.</summary>
public sealed class BatchRunner
{
    readonly int _maxParallel;

    public BatchRunner(int? maxParallel = null) =>
        _maxParallel = maxParallel ?? Math.Clamp(Environment.ProcessorCount - 1, 1, 8);

    /// <param name="outputFolder">Null means "next to each original".</param>
    /// <param name="progress">Reports how many files have finished (converted, failed or skipped).</param>
    public async Task<BatchSummary> RunAsync(
        IReadOnlyList<QueueItem> items, ConvertOptions options, string? outputFolder,
        IImageEngine engine, IProgress<int>? progress, CancellationToken ct)
    {
        var clock = Stopwatch.StartNew();
        int converted = 0, failed = 0, skipped = 0, finished = 0;
        long bytesIn = 0, bytesOut = 0;
        string? firstFolder = null;

        // Decide every output name up front, in order, so names are predictable and never clash.
        var names = new NameAllocator();
        foreach (var item in items)
        {
            item.OutputPath = null;
            if (ImageKinds.IsExcluded(item.Path))
            {
                item.State = ItemState.Skipped;
                item.Status = "Skipped: vector or document file";
                skipped++;
                finished++;
                continue;
            }
            item.State = ItemState.Waiting;
            item.Status = "Waiting";
            item.OutputPath = names.Allocate(OutputDirectory(item, outputFolder),
                Path.GetFileNameWithoutExtension(item.Path), options.Target.Extension);
        }
        progress?.Report(finished);

        var todo = items.Where(i => i.OutputPath != null).ToList();
        bool cancelled = false;
        try
        {
            await Parallel.ForEachAsync(todo,
                new ParallelOptions { MaxDegreeOfParallelism = _maxParallel, CancellationToken = ct },
                async (item, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    item.State = ItemState.Working;
                    item.Status = "Converting...";

                    // The conversion itself runs on a pool thread. (A body that finishes synchronously would
                    // keep everything on one thread, so the work has to be genuinely handed off.)
                    await Task.Run(() =>
                    {
                        try
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(item.OutputPath!)!);
                            engine.Convert(item.Path, item.OutputPath!, options);
                            long size = new FileInfo(item.OutputPath!).Length;
                            item.State = ItemState.Done;
                            item.Status = $"Done: {Path.GetFileName(item.OutputPath)} ({QueueItem.FormatSize(size)})";
                            Interlocked.Increment(ref converted);
                            Interlocked.Add(ref bytesIn, item.SizeBytes);
                            Interlocked.Add(ref bytesOut, size);
                            Interlocked.CompareExchange(ref firstFolder, Path.GetDirectoryName(item.OutputPath), null);
                        }
                        catch (Exception ex)
                        {
                            item.State = ItemState.Failed;
                            item.Status = "Failed: " + FirstLine(ex.Message);
                            Interlocked.Increment(ref failed);
                        }
                    }, CancellationToken.None);

                    progress?.Report(Interlocked.Increment(ref finished));
                });
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
            foreach (var item in todo.Where(i => i.State is ItemState.Waiting or ItemState.Working))
            {
                item.State = ItemState.Waiting;
                item.Status = "Cancelled";
            }
        }

        clock.Stop();
        return new BatchSummary(converted, failed, skipped, bytesIn, bytesOut, clock.Elapsed, cancelled, firstFolder);
    }

    /// <summary>Where this file's output goes: next to the original, or in the chosen folder (mirroring sub-folders).</summary>
    public static string OutputDirectory(QueueItem item, string? outputFolder)
    {
        string sourceDir = Path.GetDirectoryName(item.Path) ?? "";
        if (outputFolder == null) return sourceDir;
        if (item.Root == null) return outputFolder;

        string relative = Path.GetRelativePath(item.Root, sourceDir);
        return relative == "." ? outputFolder : Path.Combine(outputFolder, relative);
    }

    static string FirstLine(string message)
    {
        int i = message.IndexOfAny(new[] { '\r', '\n' });
        string line = i >= 0 ? message.Substring(0, i) : message;
        return line.Length > 160 ? line.Substring(0, 160) + "..." : line;
    }
}
