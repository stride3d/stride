// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using xunit.runner.stride.ViewModels;

namespace xunit.runner.stride.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        // Tunnel so Enter/Space reach us before TreeView consumes them. Tab navigation is
        // declarative (IsTabStop="False" on chrome).
        AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        Loaded += OnLoaded;
        DataContextChanged += (_, _) => WireFocusTest();
        SizeChanged += (_, _) => ApplyResponsiveLayout();
    }

    void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // Focus the filter textbox immediately so users can start typing without clicking.
        Dispatcher.UIThread.Post(() => FilterBox.Focus(), DispatcherPriority.Background);
        WireFocusTest();
        ApplyResponsiveLayout();
    }

    // Tree | output split flips to stacked when the viewport is narrow or portrait.
    // Landscape phones with ≥720 width keep side-by-side (their vertical room is the
    // scarce dimension). Toolbar wraps independently via WrapPanel.
    const double NarrowBreakpoint = 720;

    bool currentLayoutNarrow;
    bool layoutInitialized;

    void ApplyResponsiveLayout()
    {
        bool narrow = Bounds.Width < NarrowBreakpoint || Bounds.Width < Bounds.Height;
        if (DataContext is MainViewModel m) m.Tests.IsNarrowMode = narrow;
        ApplyNarrowVisibility();
        if (layoutInitialized && narrow == currentLayoutNarrow) return;
        currentLayoutNarrow = narrow;
        layoutInitialized = true;

        if (narrow)
        {
            // Single-cell layout: both tree and detail occupy the full content area; the
            // ApplyNarrowVisibility toggle picks which one is shown. The splitter is hidden.
            MainSplit.ColumnDefinitions.Clear();
            MainSplit.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            MainSplit.RowDefinitions.Clear();
            MainSplit.RowDefinitions.Add(new RowDefinition(GridLength.Star));

            Grid.SetColumn(TestsTree, 0); Grid.SetRow(TestsTree, 0);
            Grid.SetColumn(InspectBorder, 0); Grid.SetRow(InspectBorder, 0);
            Grid.SetColumn(MainSplitter, 0); Grid.SetRow(MainSplitter, 0);
        }
        else
        {
            // Side by side: tree | splitter | output.
            MainSplit.RowDefinitions.Clear();
            MainSplit.ColumnDefinitions.Clear();
            MainSplit.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            MainSplit.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(8, GridUnitType.Pixel)));
            MainSplit.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(2, GridUnitType.Star)));

            Grid.SetRow(TestsTree, 0); Grid.SetColumn(TestsTree, 0);
            Grid.SetRow(MainSplitter, 0); Grid.SetColumn(MainSplitter, 1);
            Grid.SetRow(InspectBorder, 0); Grid.SetColumn(InspectBorder, 2);

            MainSplitter.ResizeDirection = GridResizeDirection.Columns;
            MainSplitter.Width = 8; MainSplitter.Height = double.NaN;
            MainSplitter.HorizontalAlignment = HorizontalAlignment.Center;
            MainSplitter.VerticalAlignment = VerticalAlignment.Stretch;
        }
    }

    void WireFocusTest()
    {
        if (DataContext is not MainViewModel main) return;
        // Selecting the running test surfaces it in the inspect panel (bound to SelectedItem).
        main.Tests.FocusTest = vm => TestsTree.SelectedItem = vm;
        // React to DetailPageActive flips from the VM (e.g. when leaving narrow mode).
        main.Tests.PropertyChanged -= OnTestsViewModelPropertyChanged;
        main.Tests.PropertyChanged += OnTestsViewModelPropertyChanged;
    }

    void OnTestsViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TestsViewModel.IsDetailPageActive)
                           or nameof(TestsViewModel.IsNarrowMode))
            ApplyNarrowVisibility();
    }

    // Apply the drill-down rule: in narrow mode show either the tree OR the detail; in wide
    // mode show both. Also drives the back button and the splitter handle.
    void ApplyNarrowVisibility()
    {
        if (DataContext is not MainViewModel main) return;
        var narrow = main.Tests.IsNarrowMode;
        var detail = narrow && main.Tests.IsDetailPageActive;
        TestsTree.IsVisible = !narrow || !detail;
        InspectBorder.IsVisible = !narrow || detail;
        MainSplitter.IsVisible = !narrow;
        BackButton.IsVisible = narrow;
    }

    void OnBackToList(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel main) return;
        main.Tests.IsDetailPageActive = false;
    }

    // Detail-pane Run/Preview buttons use the same multi-aware path as Enter so a tree
    // multi-selection runs all selected tests, not just the focused one.
    void OnDetailRun(object? sender, RoutedEventArgs e) => RunFromDetail(interactive: false);
    void OnDetailPreview(object? sender, RoutedEventArgs e) => RunFromDetail(interactive: true);

    void RunFromDetail(bool interactive)
    {
        if (DataContext is not MainViewModel main) return;
        var vm = main.Tests;
        if (vm.RunningTests) return;
        if (vm.SelectedCases.Count > 0)
            _ = vm.RunSelectedCases(interactive);
        else if (TestsTree.SelectedItem is TestNodeViewModel node)
            vm.RunTests(node, interactive);
    }

    void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainViewModel main) return;
        var vm = main.Tests;

        bool focusOnFilter = FilterBox.IsFocused;
        bool ctrl = (e.KeyModifiers & KeyModifiers.Control) != 0;
        bool shift = (e.KeyModifiers & KeyModifiers.Shift) != 0;

        // `/` focuses the filter from anywhere — except when already typing in it.
        if (e.Key == Key.OemQuestion && !ctrl && !shift && !focusOnFilter)
        {
            FilterBox.Focus();
            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case Key.Escape:
                TestsTree.SelectedItems?.Clear();
                FilterBox.Focus();
                e.Handled = true;
                return;

            case Key.F when ctrl:
                FilterBox.Focus();
                e.Handled = true;
                return;

            case Key.F5:
                if (!vm.RunningTests) vm.RunSelected();
                e.Handled = true;
                return;

            case Key.R when ctrl:
                if (vm.HasFailures && !vm.RunningTests) vm.RunFailedCmd();
                e.Handled = true;
                return;

            case Key.Up:
            case Key.Down:
                // From the filter: move tree selection without leaving the textbox. In the
                // tree, native nav handles it.
                if (focusOnFilter)
                {
                    MoveTreeSelection(e.Key == Key.Down ? +1 : -1);
                    e.Handled = true;
                }
                return;

            case Key.PageUp:
            case Key.PageDown:
                if (focusOnFilter)
                {
                    MoveTreeSelection(e.Key == Key.PageDown ? +10 : -10);
                    e.Handled = true;
                }
                return;

            case Key.Enter:
                // Headless run, or interactive preview with Shift. Runs every selected
                // test (SelectedCases is the flat expansion of the tree selection, so
                // selecting a group runs its cases too).
                if (!vm.RunningTests && vm.SelectedCases.Count > 0)
                {
                    _ = vm.RunSelectedCases(interactive: shift);
                    e.Handled = true;
                }
                return;

            case Key.Space:
                // Tree only — Space in the filter must type a literal space.
                if (!focusOnFilter && !vm.RunningTests && vm.SelectedCases.Count > 0)
                {
                    _ = vm.RunSelectedCases(interactive: shift);
                    e.Handled = true;
                }
                return;
        }
    }

    // Set while MoveTreeSelection is rewriting the selection so the SelectionChanged event
    // doesn't fire twice (Clear + Set). We sync once at the end instead.
    bool suspendSelectionSync;

    void MoveTreeSelection(int delta)
    {
        if (DataContext is not MainViewModel main) return;
        var vm = main.Tests;

        // Arrow nav walks the flat list of visible test cases (skipping groups so Enter
        // always has something runnable selected).
        var cases = vm.EnumerateAllTestCases().Where(c => c.IsVisible).ToList();
        if (cases.Count == 0) return;

        int currentIndex = -1;
        if (TestsTree.SelectedItem is TestCaseViewModel current)
            currentIndex = cases.IndexOf(current);

        int next = currentIndex < 0
            ? (delta > 0 ? 0 : cases.Count - 1)
            : Math.Clamp(currentIndex + delta, 0, cases.Count - 1);

        var target = cases[next];
        suspendSelectionSync = true;
        try
        {
            TestsTree.SelectedItems?.Clear();
            TestsTree.SelectedItem = target;
        }
        finally
        {
            suspendSelectionSync = false;
        }
        SyncSelection();
    }

    void OnTreeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (suspendSelectionSync) return;
        SyncSelection();
    }

    TestNodeViewModel? previousPrimary;

    void SyncSelection()
    {
        if (DataContext is not MainViewModel main) return;
        var vm = main.Tests;

        vm.SelectedCases.Clear();
        foreach (var item in TestsTree.SelectedItems!)
        {
            if (item is TestCaseViewModel testCase)
                vm.SelectedCases.Add(testCase);
            else if (item is TestGroupViewModel group)
                foreach (var c in group.EnumerateTestCases())
                    vm.SelectedCases.Add(c);
        }
        vm.OnSelectionChanged();
        // Live output coloring follows the focused test as it streams in during a run.
        WatchOutput(TestsTree.SelectedItem as TestCaseViewModel);

        // Track the primary (focused) row in a multi-selection so its row can show an accent
        // stripe — visually disambiguating "the one in the inspect pane" from the rest.
        var newPrimary = TestsTree.SelectedItem as TestNodeViewModel;
        if (previousPrimary is not null && previousPrimary != newPrimary)
            previousPrimary.IsPrimarySelection = false;
        if (newPrimary is not null)
            newPrimary.IsPrimarySelection = true;
        previousPrimary = newPrimary;

        // In narrow mode tapping a row drills into the detail page.
        if (vm.IsNarrowMode && TestsTree.SelectedItem is not null)
            vm.IsDetailPageActive = true;
    }

    // Per-line tinting requires populating Inlines manually (a plain Text binding can't
    // carry per-run colors). Lines containing "Warning:"/"Error:" get a hue.

    TestCaseViewModel? watchedCase;
    static readonly IBrush WarningBrush = new SolidColorBrush(Color.Parse("#E5C07B"));
    static readonly IBrush ErrorBrush = new SolidColorBrush(Color.Parse("#FF6E6E"));

    void WatchOutput(TestCaseViewModel? testCase)
    {
        if (watchedCase is not null)
        {
            watchedCase.PropertyChanged -= OnWatchedCasePropertyChanged;
            watchedCase.ImageComparisons.CollectionChanged -= OnImageComparisonsChanged;
        }
        watchedCase = testCase;
        if (watchedCase is not null)
        {
            watchedCase.PropertyChanged += OnWatchedCasePropertyChanged;
            watchedCase.ImageComparisons.CollectionChanged += OnImageComparisonsChanged;
        }
        RebuildOutputInlines(testCase?.Output);
        if (testCase is not null)
            foreach (var entry in testCase.ImageComparisons)
                _ = LoadEntryAsync(entry);
    }

    void OnWatchedCasePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not TestCaseViewModel vm) return;
        if (e.PropertyName == nameof(TestCaseViewModel.Output))
            RebuildOutputInlines(vm.Output);
    }

    void OnImageComparisonsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is null) return;
        foreach (ImageComparisonViewModel entry in e.NewItems)
            _ = LoadEntryAsync(entry);
    }

    void RebuildOutputInlines(string? output)
    {
        OutputBlock.Inlines ??= new InlineCollection();
        OutputBlock.Inlines.Clear();
        if (string.IsNullOrEmpty(output))
            return;
        foreach (var rawLine in output.Split('\n'))
        {
            var line = rawLine.EndsWith('\r') ? rawLine[..^1] : rawLine;
            IBrush? color = null;
            if (line.Contains("Error:", System.StringComparison.Ordinal)) color = ErrorBrush;
            else if (line.Contains("Warning:", System.StringComparison.Ordinal)) color = WarningBrush;
            var run = new Run(line + "\n");
            if (color is not null) run.Foreground = color;
            OutputBlock.Inlines.Add(run);
        }
    }

    // === Image comparison ===
    // Diff threshold: per-channel absolute difference above this is highlighted in red.
    const int DiffThreshold = 2;

    // Load bitmaps + (on failure) compute the pixel diff for one comparison entry, then
    // publish them on the VM so the bound Image controls update. Off-thread so a stack of
    // 20+ frames doesn't freeze the UI.
    static async Task LoadEntryAsync(ImageComparisonViewModel entry)
    {
        if (entry.CurrentBitmap is not null || entry.ReferenceBitmap is not null)
            return; // already loaded — no need to redo on re-watch
        var (curBmp, refBmp, diffBmp) = await Task.Run(() =>
        {
            Bitmap? cur = LoadIfExists(entry.CurrentPath);
            Bitmap? @ref = LoadIfExists(entry.ReferencePath);
            WriteableBitmap? diff = (!entry.Passed && cur is not null && @ref is not null)
                ? ComputeDiff(cur, @ref)
                : null;
            return (cur, @ref, diff);
        });
        entry.CurrentBitmap = curBmp;
        entry.ReferenceBitmap = refBmp;
        entry.DiffBitmap = diffBmp;
    }

    static Bitmap? LoadIfExists(string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
        try { return new Bitmap(path); } catch { return null; }
    }

    // Output is the current image with pixels that differ from reference (in any channel
    // by more than DiffThreshold) overpainted red. Dimensions clamp to the intersection of
    // both images so size mismatches don't throw — extra rows/cols on the larger one are
    // simply ignored.
    static unsafe WriteableBitmap? ComputeDiff(Bitmap current, Bitmap reference)
    {
        int w = (int)System.Math.Min(current.PixelSize.Width, reference.PixelSize.Width);
        int h = (int)System.Math.Min(current.PixelSize.Height, reference.PixelSize.Height);
        if (w <= 0 || h <= 0) return null;

        var size = new PixelSize(w, h);
        var stride = w * 4;
        var bytes = w * h * 4;
        var curBuf = new byte[bytes];
        var refBuf = new byte[bytes];
        try
        {
            current.CopyPixels(new PixelRect(0, 0, w, h), System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(curBuf, 0), bytes, stride);
            reference.CopyPixels(new PixelRect(0, 0, w, h), System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(refBuf, 0), bytes, stride);
        }
        catch { return null; }

        var diff = new WriteableBitmap(size, current.Dpi, PixelFormat.Bgra8888, AlphaFormat.Premul);
        using (var fb = diff.Lock())
        {
            var dst = (byte*)fb.Address;
            for (int i = 0; i < bytes; i += 4)
            {
                int db = System.Math.Abs(curBuf[i + 0] - refBuf[i + 0]);
                int dg = System.Math.Abs(curBuf[i + 1] - refBuf[i + 1]);
                int dr = System.Math.Abs(curBuf[i + 2] - refBuf[i + 2]);
                int maxD = System.Math.Max(db, System.Math.Max(dg, dr));
                if (maxD > DiffThreshold)
                {
                    // Highlight in red; intensity scales with the diff magnitude.
                    byte intensity = (byte)System.Math.Min(255, 96 + maxD * 4);
                    dst[i + 0] = 0;            // B
                    dst[i + 1] = 0;            // G
                    dst[i + 2] = intensity;    // R
                    dst[i + 3] = 255;          // A
                }
                else
                {
                    // Dim grayscale of current so the highlighted pixels pop visually.
                    int gray = (curBuf[i + 0] + curBuf[i + 1] + curBuf[i + 2]) / 3 / 3;
                    dst[i + 0] = (byte)gray;
                    dst[i + 1] = (byte)gray;
                    dst[i + 2] = (byte)gray;
                    dst[i + 3] = 255;
                }
            }
        }
        return diff;
    }

    // Reveal/Open/Copy/Promote sources: the MenuItem / Button is bound to one row's
    // ImageComparisonViewModel via {Binding} or {Binding <Path>} Tag — pull the target from
    // sender so each entry's actions hit its own paths, not the test's first/last comparison.
    void OnRevealCurrent(object? sender, RoutedEventArgs e) => RevealInExplorer((sender as Control)?.Tag as string);
    void OnRevealReference(object? sender, RoutedEventArgs e) => RevealInExplorer((sender as Control)?.Tag as string);

    void OnOpenFile(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.Tag is not string path || string.IsNullOrEmpty(path) || !File.Exists(path)) return;
        try
        {
            // UseShellExecute lets the OS pick the default app for the extension (image viewer
            // for .png on Windows, Preview on macOS, xdg-open on Linux).
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Open failed for {path}: {ex.Message}");
        }
    }

    async void OnCopyPath(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.Tag is not string path || string.IsNullOrEmpty(path)) return;
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null) return;
        try { await clipboard.SetTextAsync(path); }
        catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine($"Copy path failed: {ex.Message}"); }
    }

    // Opens the OS file manager scrolled to the target path. If the file itself doesn't
    // exist (e.g. promote-pending reference) we fall back to opening the parent folder.
    static void RevealInExplorer(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            if (OperatingSystem.IsWindows())
            {
                if (File.Exists(path))
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{path}\"");
                else if (Directory.Exists(Path.GetDirectoryName(path)))
                    System.Diagnostics.Process.Start("explorer.exe", $"\"{Path.GetDirectoryName(path)}\"");
            }
            else if (OperatingSystem.IsMacOS())
            {
                System.Diagnostics.Process.Start("open", File.Exists(path) ? $"-R \"{path}\"" : $"\"{Path.GetDirectoryName(path)}\"");
            }
            else
            {
                var dir = File.Exists(path) ? Path.GetDirectoryName(path) : path;
                if (!string.IsNullOrEmpty(dir))
                    System.Diagnostics.Process.Start("xdg-open", $"\"{dir}\"");
            }
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Reveal failed for {path}: {ex.Message}");
        }
    }

    void OnPromoteCurrent(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.Tag is not ImageComparisonViewModel entry) return;
        if (string.IsNullOrEmpty(entry.CurrentPath) || string.IsNullOrEmpty(entry.ReferencePath)) return;
        if (!File.Exists(entry.CurrentPath)) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(entry.ReferencePath)!);
            File.Copy(entry.CurrentPath, entry.ReferencePath, overwrite: true);
            // Force the entry's reference image to reload so the new gold appears immediately.
            entry.ReferenceBitmap = null;
            entry.CurrentBitmap = null;
            entry.DiffBitmap = null;
            _ = LoadEntryAsync(entry);
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Promote failed: {ex}");
        }
    }
}
