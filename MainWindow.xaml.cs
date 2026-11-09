using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ImageConverter.Services;
using Microsoft.Win32;

namespace ImageConverter;

public partial class MainWindow : Window
{
    readonly ObservableCollection<QueueItem> _items = new();
    readonly IImageEngine _engine = new MagickEngine();
    CancellationTokenSource? _cts;
    DispatcherTimer? _timer;
    readonly Stopwatch _clock = new();
    int _finished;
    int _total;
    bool _busy;
    string? _lastOutputFolder;

    public MainWindow()
    {
        InitializeComponent();
        QueueGrid.ItemsSource = _items;
        Loaded += (_, _) => LoadFormats();
    }

    void SetStatus(string text) => StatusText.Text = text;

    // ---------------------------------------------------------------- startup

    void LoadFormats()
    {
        IReadOnlyList<TargetFormat> formats;
        try
        {
            formats = MagickEngine.AvailableTargets();
        }
        catch (Exception ex)
        {
            formats = TargetFormats.All;
            SetStatus("The image engine didn't start properly: " + ex.Message);
        }
        try
        {
            SupportedText.Text = "Reads: " + SupportedFormats.Describe(MagickEngine.ReadableFormats());
        }
        catch
        {
            SupportedText.Text = "Reads: " + SupportedFormats.Fallback;
        }

        FormatBox.ItemsSource = formats;
        FormatBox.SelectedIndex = 0;   // JPG
        if (StatusText.Text.Length == 0)
            SetStatus("Drop images or folders onto the window, choose a format, then click Convert.");
    }

    // ---------------------------------------------------------------- adding files

    void Window_DragOver(object sender, DragEventArgs e)
    {
        bool ok = !_busy && e.Data.GetDataPresent(DataFormats.FileDrop);
        e.Effects = ok ? DragDropEffects.Copy : DragDropEffects.None;
        if (ok) DropRect.Fill = (Brush)FindResource("TintDarkBrush");
        e.Handled = true;
    }

    void Window_DragLeave(object sender, DragEventArgs e) => DropRect.Fill = (Brush)FindResource("TintBrush");

    async void Window_Drop(object sender, DragEventArgs e)
    {
        DropRect.Fill = (Brush)FindResource("TintBrush");
        if (_busy) return;
        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths)
            await AddPathsAsync(paths);
    }

    async Task AddPathsAsync(IEnumerable<string> paths)
    {
        SetStatus("Scanning...");
        bool subfolders = SubfoldersBox.IsChecked == true;
        var existing = new HashSet<string>(_items.Select(i => i.Path), StringComparer.OrdinalIgnoreCase);
        var list = paths.ToList();

        List<QueueItem> added;
        try
        {
            added = await Task.Run(() => FileScanner.Expand(list, subfolders, existing));
        }
        catch (Exception ex)
        {
            SetStatus("Couldn't read that: " + ex.Message);
            return;
        }

        foreach (var item in added) _items.Add(item);
        SetStatus(added.Count == 0
            ? "No new images found there."
            : $"Added {added.Count} file(s). {_items.Count} in the list.");
    }

    async void AddFiles_Click(object sender, RoutedEventArgs e)
    {
        string patterns = string.Join(";", ImageKinds.Readable.OrderBy(x => x).Select(x => "*" + x));
        var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Title = "Choose images to convert",
            Filter = $"Image files|{patterns}|All files|*.*"
        };
        if (dialog.ShowDialog() == true) await AddPathsAsync(dialog.FileNames);
    }

    async void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Choose a folder of images" };
        if (dialog.ShowDialog() == true) await AddPathsAsync(new[] { dialog.FolderName });
    }

    void Clear_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        _items.Clear();
        ResetStats();
        SetStatus("List cleared.");
    }

    void QueueGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Delete || _busy) return;
        foreach (var item in QueueGrid.SelectedItems.Cast<QueueItem>().ToList())
            _items.Remove(item);
        e.Handled = true;
    }

    // ---------------------------------------------------------------- options

    void Format_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (QualityPanel == null || BackgroundPanel == null) return;
        if (FormatBox.SelectedItem is not TargetFormat target) return;

        QualityPanel.Visibility = target.HasQuality ? Visibility.Visible : Visibility.Collapsed;
        BackgroundPanel.Visibility = target.SupportsAlpha ? Visibility.Collapsed : Visibility.Visible;
        QualitySlider.Value = target.DefaultQuality;
    }

    void Quality_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (QualityValue == null) return;
        QualityValue.Text = ((int)QualitySlider.Value).ToString();
    }

    void OutputBox_GotFocus(object sender, RoutedEventArgs e) => FolderRadio.IsChecked = true;

    void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Choose where to save the converted images" };
        if (dialog.ShowDialog() != true) return;
        OutputBox.Text = dialog.FolderName;
        FolderRadio.IsChecked = true;
    }

    string BackgroundHex() =>
        (BackgroundBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "#FFFFFF";

    // ---------------------------------------------------------------- converting

    void SetBusy(bool busy)
    {
        _busy = busy;
        ConvertButton.IsEnabled = !busy;
        CancelButton.IsEnabled = busy;
        AddFilesButton.IsEnabled = !busy;
        AddFolderButton.IsEnabled = !busy;
        ClearButton.IsEnabled = !busy;
        SubfoldersBox.IsEnabled = !busy;
        FormatBox.IsEnabled = !busy;
        QualitySlider.IsEnabled = !busy;
        BackgroundBox.IsEnabled = !busy;
        StripBox.IsEnabled = !busy;
        SameFolderRadio.IsEnabled = !busy;
        FolderRadio.IsEnabled = !busy;
        OutputBox.IsEnabled = !busy;
        BrowseButton.IsEnabled = !busy;
    }

    async void Convert_Click(object sender, RoutedEventArgs e)
    {
        if (_items.Count == 0) { SetStatus("Add some images first: drop them onto the window."); return; }
        if (FormatBox.SelectedItem is not TargetFormat target) { SetStatus("Choose a format to convert to."); return; }

        string? outputFolder = null;
        if (FolderRadio.IsChecked == true)
        {
            outputFolder = OutputBox.Text.Trim();
            if (outputFolder.Length == 0)
            {
                SetStatus("Choose an output folder, or pick \"Next to the originals\".");
                return;
            }
        }

        var options = new ConvertOptions(target, (int)QualitySlider.Value, BackgroundHex(), StripBox.IsChecked == true);
        var items = _items.ToList();

        _cts = new CancellationTokenSource();
        _total = items.Count;
        _finished = 0;
        SetBusy(true);
        OpenFolderButton.IsEnabled = false;
        RunProgress.Value = 0;
        StartStats();
        SetStatus($"Converting {_total} file(s) to {target.Name}...");

        string? dialogText = null;
        var icon = MessageBoxImage.Information;
        try
        {
            var progress = new Progress<int>(n =>
            {
                _finished = n;
                RunProgress.Value = _total == 0 ? 0 : n * 100.0 / _total;
            });
            var summary = await new BatchRunner().RunAsync(items, options, outputFolder, _engine, progress, _cts.Token);

            _lastOutputFolder = summary.FirstOutputFolder;
            if (summary.Cancelled)
            {
                SetStatus($"Cancelled. {summary.Converted} converted before stopping.");
            }
            else
            {
                string text = $"Converted {summary.Converted} image(s) to {target.Name}.";
                if (summary.Failed > 0) text += $"  {summary.Failed} failed.";
                if (summary.Skipped > 0) text += $"  {summary.Skipped} skipped.";
                SetStatus(text);

                dialogText = text;
                if (summary.Converted > 0)
                    dialogText += $"\n\nSize: {QueueItem.FormatSize(summary.BytesIn)} to {QueueItem.FormatSize(summary.BytesOut)}." +
                                  $"\nTime: {FormatTime(summary.Elapsed.TotalSeconds)}.";
                if (_lastOutputFolder != null) dialogText += $"\n\nSaved to:\n{_lastOutputFolder}";
                if (summary.Failed > 0)
                {
                    dialogText += "\n\nThe files that failed are marked in red in the list, with the reason.";
                    icon = MessageBoxImage.Warning;
                }
            }
        }
        catch (Exception ex)
        {
            SetStatus("Error: " + ex.Message);
        }
        finally
        {
            _cts = null;
            StopStats();
            SetBusy(false);
            OpenFolderButton.IsEnabled = _lastOutputFolder != null && Directory.Exists(_lastOutputFolder);
            RunProgress.Value = 0;
            if (dialogText != null)
                MessageBox.Show(this, dialogText, "Conversion complete", MessageBoxButton.OK, icon);
        }
    }

    void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        CancelButton.IsEnabled = false;
        SetStatus("Cancelling...");
    }

    void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (_lastOutputFolder != null && Directory.Exists(_lastOutputFolder))
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{_lastOutputFolder}\"") { UseShellExecute = true });
    }

    // ---------------------------------------------------------------- live readout

    void StartStats()
    {
        _clock.Restart();
        _timer?.Stop();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _timer.Tick += (_, _) => UpdateStats();
        _timer.Start();
        UpdateStats();
    }

    void StopStats()
    {
        _timer?.Stop();
        _clock.Stop();
        UpdateStats();   // leave the final figures on screen
    }

    void UpdateStats()
    {
        double seconds = _clock.Elapsed.TotalSeconds;
        double speed = seconds > 0.2 ? _finished / seconds : 0;
        int failed = _items.Count(i => i.State == ItemState.Failed);

        // each figure sits in its own fixed-width cell, so the line never shifts as the numbers change
        DoneText.Text = $"Done {_finished} of {_total}";
        ElapsedText.Text = $"Elapsed {FormatTime(seconds)}";
        SpeedText.Text = $"Speed {speed:0.0} images/second";
        FailedText.Text = $"Failed {failed}";
    }

    void ResetStats()
    {
        _finished = 0;
        _total = 0;
        DoneText.Text = ElapsedText.Text = SpeedText.Text = FailedText.Text = "";
        RunProgress.Value = 0;
    }

    static string FormatTime(double seconds) => $"{(int)seconds / 60}:{(int)seconds % 60:00}";

    // ---------------------------------------------------------------- about and links

    void About_Click(object sender, RoutedEventArgs e) => new AboutWindow { Owner = this }.ShowDialog();

    void Link_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        OpenUrl(e.Uri.AbsoluteUri);
        e.Handled = true;
    }

    /// <summary>Opens a web address in the user's default browser.</summary>
    public static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { /* no browser available: nothing useful to do */ }
    }
}
