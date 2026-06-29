// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;
using System.Reflection;
using Avalonia.Threading;
using Xunit;
using Xunit.Abstractions;

namespace xunit.runner.stride.ViewModels;

public enum StatusFilter
{
    All,
    Passed,
    Failed,
    Skipped,
    NotRun,
}

public enum RenderDocCaptureMode
{
    /// <summary>No RenderDoc capture.</summary>
    Never,
    /// <summary>Capture every frame; keep only the captures for failing tests.</summary>
    OnError,
    /// <summary>Capture every frame; keep captures for every test, pass or fail.</summary>
    Always,
}

/// <summary>
///   Payload pushed by the launcher (which references Stride.Graphics.Regression) into the
///   runner whenever a screenshot/gold comparison completes. Lets the inspect panel show
///   current/reference/diff without the runner needing graphics-side type references.
/// </summary>
public sealed record ImageCompareResult(string CurrentPath, string ReferencePath, bool Passed, string? StatsSummary);

public class TestsViewModel : ViewModelBase
{
    private StrideTestController? Controller { get; }
    private readonly Assembly? testAssembly;

    // Discovery runs in the background — it triggers Mono assembly load + reflection for the
    // whole test assembly graph, which on Android is heavy enough (30-60s) to ANR the main
    // thread if done synchronously in the ctor. Headless mode awaits DiscoveryTask before
    // RunTestsAsync; UI mode shows whatever is in TestCases at bind time and refreshes later.
    public Task DiscoveryTask { get; }

    public TestsViewModel()
    {
        // Desktop launches via Main (entry assembly == test assembly); Android launches via the
        // per-project MainActivity, which sets App.TestAssembly.
        testAssembly = App.TestAssembly ?? Assembly.GetEntryAssembly();
        if (testAssembly is null)
        {
            TestCases.Add(new TestGroupViewModel(this, "No tests were found"));
            DiscoveryTask = Task.CompletedTask;
            return;
        }

        Controller = new StrideTestController(testAssembly);
        DiscoveryTask = Task.Run(async () =>
        {
            var sink = new TestDiscoverySink();
            Controller.Find(true, sink, TestFrameworkOptions.ForDiscovery());
            sink.Finished.WaitOne();

            var testAssemblyViewModel = new TestGroupViewModel(this, sink.TestCases.FirstOrDefault()?.TestMethod.TestClass.TestCollection.TestAssembly.Assembly.Name ?? "No tests were found");
            foreach (var testClass in sink.TestCases.GroupBy(x => x.TestMethod.TestClass))
            {
                var classFullName = testClass.Key.Class.Name;
                var classShortName = classFullName[(classFullName.LastIndexOf('.') + 1)..];
                var testClassViewModel = new TestGroupViewModel(this, classShortName);
                testAssemblyViewModel.Children.Add(testClassViewModel);
                foreach (var testCase in testClass)
                {
                    testClassViewModel.Children.Add(new TestCaseViewModel(this, testCase));
                }
            }
            // InvokeAsync (not Post): the headless `await DiscoveryTask` would otherwise resume
            // before the UI-thread Add ran, see Count==0, and skip RunTestsAsync.
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                TestCases.Add(testAssemblyViewModel);
                RecomputeCounts();
            });
        });

        // Mirror the env-var default so the dropdown opens on the same value GameTestBase
        // would have used at startup.
        renderDocMode = Environment.GetEnvironmentVariable("STRIDE_TESTS_RENDERDOC")?.ToLowerInvariant() switch
        {
            "error" => RenderDocCaptureMode.OnError,
            "always" => RenderDocCaptureMode.Always,
            _ => RenderDocCaptureMode.Never,
        };
    }

    /// <summary>Called by the launcher whenever an image comparison completes; appends a new
    /// entry to the currently-running test's list so multi-frame tests (e.g. UI tests) keep
    /// every comparison instead of overwriting the previous one.</summary>
    public void OnImageComparison(ImageCompareResult result)
    {
        var vm = currentlyRunningCase;
        if (vm is null) return;
        var entry = new ImageComparisonViewModel(result);
        Dispatcher.UIThread.Post(() => vm.ImageComparisons.Add(entry));
    }

    public void RunTests(TestNodeViewModel testNodeViewModel) => RunTests(testNodeViewModel, interactive: false);

    // Task-returning variant for headless dispatch (Android MainActivity awaits this and reads
    // FailedCount for the process exit code). The async void RunTests overloads are for UI
    // bindings that can't await.
    public Task RunTestsAsync(TestNodeViewModel testNodeViewModel)
        => RunTestCases(testNodeViewModel.EnumerateTestCases().ToList(), interactive: false);

    // Headless run narrowed by a vstest --filter predicate (Android/iOS launcher passes the
    // expression as a launch parameter). Mirrors the desktop StrideXunitRunner.RunHeadless filter.
    public Task RunFilteredAsync(Func<ITestCase, bool> predicate)
        => RunTestCases(EnumerateAllTestCases().Where(c => predicate(c.TestCase)).ToList(), interactive: false);

    // async void to satisfy Avalonia's method-as-Command binding (per-row buttons), but the
    // body is wrapped so a test-run exception can't take down the process.
    public async void RunTests(TestNodeViewModel testNodeViewModel, bool interactive)
    {
        try
        {
            var testCases = testNodeViewModel.EnumerateTestCases().ToList();
            if (testCases.Count == 0)
                return;
            await RunTestCases(testCases, interactive);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Test run failed: {ex}");
        }
    }

    public async Task RunSelectedCases(bool interactive)
    {
        var cases = SelectedCases.Count > 0 ? SelectedCases.ToList() : EnumerateVisibleTestCases().ToList();
        if (cases.Count == 0)
            return;
        await RunTestCases(cases, interactive);
    }

    public async Task RunFailed(bool interactive)
    {
        var cases = EnumerateAllTestCases().Where(c => c.Failed).ToList();
        if (cases.Count == 0)
            return;
        await RunTestCases(cases, interactive);
    }

    async Task RunTestCases(IReadOnlyList<TestCaseViewModel> testCases, bool interactive)
    {
        var testCaseViewModels = new Dictionary<string, TestCaseViewModel>();
        foreach (var testCase in testCases)
            testCaseViewModels[testCase.TestCase.UniqueID] = testCase;

        int testCasesFinished = 0;
        // The per-run interactive flag flows to GameTestBase via SetInteractiveMode.
        // ForceSaveImage rides on the persistent IsForceSaveImage checkbox setter.
        SetInteractiveMode?.Invoke(interactive);
        // Per-test stdout/stderr capture for the inspect panel.
        var originalOut = Console.Out;
        var originalErr = Console.Error;
        var outRedirector = new TestOutputRedirector(originalOut);
        var errRedirector = new TestOutputRedirector(originalErr);
        Console.SetOut(outRedirector);
        Console.SetError(errRedirector);

        // Live-stream the captured buffer into vm.Output every 200 ms so the inspect panel
        // updates while the test is still running. currentlyRunningCase is also read by
        // OnImageComparison so an out-of-band ImageTester event can find the right vm.
        var liveTimer = new System.Threading.Timer(_ =>
        {
            var vm = currentlyRunningCase;
            if (vm is null) return;
            var id = vm.TestCase.UniqueID;
            var outBuf = outRedirector.PeekOutput(id);
            var errBuf = errRedirector.PeekOutput(id);
            var output = (outBuf, errBuf) switch
            {
                (null, null) => null,
                (_, null) => outBuf,
                (null, _) => errBuf,
                _ => outBuf + Environment.NewLine + errBuf,
            };
            Dispatcher.UIThread.Post(() =>
            {
                if (vm == currentlyRunningCase) vm.Output = output;
            });
        }, null, dueTime: System.Threading.Timeout.Infinite, period: System.Threading.Timeout.Infinite);

        try
        {
            await Task.Run(() =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    TestCompletion = 0.0;
                    RunningTests = true;
                    foreach (var vm in testCaseViewModels.Values)
                    {
                        vm.Failed = false;
                        vm.Succeeded = false;
                        vm.Skipped = false;
                        vm.Pending = true;
                        vm.FailureMessage = null;
                        vm.FailureStackTrace = null;
                        vm.Output = null;
                        vm.ImageComparisons.Clear();
                    }
                    PropagateGroupState();
                });

                var sink = new XSink
                {
                    HandleTestCaseStarting = args =>
                    {
                        var id = args.Message.TestCase.UniqueID;
                        outRedirector.CurrentTestId = id;
                        errRedirector.CurrentTestId = id;
                        if (testCaseViewModels.TryGetValue(id, out var vm))
                        {
                            currentlyRunningCase = vm;
                            liveTimer.Change(200, 200);
                            Dispatcher.UIThread.Post(() =>
                            {
                                vm.Pending = false;
                                vm.Running = true;
                                PropagateGroupState();
                                // Only auto-select on a single-test run; a multi-test run
                                // must preserve the user's tree selection. The per-row ⭮
                                // icon still tracks which test is currently executing.
                                if (testCaseViewModels.Count == 1)
                                    FocusTest?.Invoke(vm);
                            });
                        }
                    },
                    HandleTestFailed = args =>
                    {
                        if (testCaseViewModels.TryGetValue(args.Message.TestCase.UniqueID, out var vm))
                        {
                            var msg = args.Message.Messages is { Length: > 0 } msgs ? string.Join("\n", msgs) : null;
                            var st = args.Message.StackTraces is { Length: > 0 } sts ? string.Join("\n---\n", sts.Where(s => !string.IsNullOrEmpty(s))!) : null;
                            Dispatcher.UIThread.Post(() =>
                            {
                                vm.FailureMessage = msg;
                                vm.FailureStackTrace = st;
                            });
                        }
                    },
                    HandleTestSkipped = args =>
                    {
                        if (testCaseViewModels.TryGetValue(args.Message.TestCase.UniqueID, out var vm))
                        {
                            Dispatcher.UIThread.Post(() => vm.Skipped = true);
                        }
                    },
                    HandleTestCaseFinished = args =>
                    {
                        var id = args.Message.TestCase.UniqueID;
                        // Drain output buffers, stop the streaming ticker, then push the
                        // final state in one UI update.
                        var outBuf = outRedirector.TakeOutput(id);
                        var errBuf = errRedirector.TakeOutput(id);
                        outRedirector.CurrentTestId = null;
                        errRedirector.CurrentTestId = null;
                        currentlyRunningCase = null;
                        liveTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                        if (testCaseViewModels.TryGetValue(id, out var vm))
                        {
                            var failed = args.Message.TestsFailed > 0;
                            var skipped = args.Message.TestsSkipped > 0 && args.Message.TestsFailed == 0 && args.Message.TestsRun == 0;
                            var elapsed = args.Message.ExecutionTime;
                            var output = (outBuf, errBuf) switch
                            {
                                (null, null) => null,
                                (_, null) => outBuf,
                                (null, _) => errBuf,
                                _ => outBuf + Environment.NewLine + errBuf,
                            };
                            Dispatcher.UIThread.Post(() =>
                            {
                                vm.Failed = failed;
                                vm.Succeeded = !failed && !skipped;
                                vm.Skipped = skipped;
                                vm.Running = false;
                                vm.ElapsedSeconds = elapsed;
                                vm.Output = output;
                                testCasesFinished++;
                                TestCompletion = (double)testCasesFinished / testCaseViewModels.Count * 100.0;
                                PropagateGroupState();
                                RecomputeCounts();
                            });
                        }
                    },
                };
                // Headless mode (Android/iOS launcher + desktop STRIDE_TESTS_INTERACTIVE!=1)
                // needs a TRX for the host driver to parse. Compose alongside the UI sink so
                // both observe the same test stream.
                using var trxSink = App.HeadlessMode ? new TrxWriter() : null;
                IMessageSinkWithTypes runSink = trxSink is null
                    ? sink
                    : new StrideXunitRunner.CompositeSink(sink, trxSink);
                Controller.RunTests(testCaseViewModels.Select(x => x.Value.TestCase).ToArray(), runSink, TestFrameworkOptions.ForExecution());
                sink.Finished.WaitOne();
                if (trxSink is not null && testAssembly is not null)
                {
                    trxSink.Finished.WaitOne();
                    trxSink.WriteTo(StrideXunitRunner.GetTrxPath(testAssembly));
                }

                Dispatcher.UIThread.Post(() =>
                {
                    RunningTests = false;
                    RecomputeCounts();
                    ApplyFilter();
                });
            });
        }
        finally
        {
            // Block until any in-flight timer callback finishes, then restore globals.
            // Without WaitOne the callback could still be running when we reassign Console
            // streams on the next run, racing against a fresh redirector.
            using var disposed = new System.Threading.ManualResetEvent(false);
            liveTimer.Dispose(disposed);
            disposed.WaitOne();
            Console.SetOut(originalOut);
            Console.SetError(originalErr);
            // Interactive is one-shot per click; reset so the next click defaults headless.
            SetInteractiveMode?.Invoke(false);
            // Defensive: clear running state and any stuck Pending flags if the run was
            // abandoned mid-flight.
            Dispatcher.UIThread.Post(() =>
            {
                if (RunningTests) RunningTests = false;
                foreach (var vm in testCaseViewModels.Values)
                    if (vm.Pending) vm.Pending = false;
                PropagateGroupState();
            });
        }
    }

    void PropagateGroupState()
    {
        foreach (var root in TestCases)
            PropagateGroupState(root);
    }

    static void PropagateGroupState(TestNodeViewModel node)
    {
        if (node is TestGroupViewModel group)
        {
            foreach (var child in group.Children)
                PropagateGroupState(child);

            group.Failed = group.Children.Any(c => c.Failed);
            group.Running = group.Children.Any(c => c.Running);
            group.Pending = group.Children.Any(c => c.Pending);
            group.Succeeded = !group.Failed && !group.Running && !group.Pending && group.Children.Count > 0 && group.Children.All(c => c.Succeeded || c.Skipped);
            group.Skipped = !group.Failed && !group.Running && !group.Pending && group.Children.Count > 0 && group.Children.All(c => c.Skipped);
            var sum = group.Children.Where(c => c.HasElapsed).Sum(c => c.ElapsedSeconds ?? 0m);
            group.ElapsedSeconds = sum > 0 ? sum : null;
        }
    }

    public IEnumerable<TestCaseViewModel> EnumerateAllTestCases() => TestCases.SelectMany(c => c.EnumerateTestCases());

    public IEnumerable<TestCaseViewModel> EnumerateVisibleTestCases() => EnumerateAllTestCases().Where(c => c.IsVisible);

    // === Filter ===

    public string FilterText
    {
        get;
        set
        {
            if (SetValue(ref field, value ?? string.Empty))
                ApplyFilter();
        }
    } = string.Empty;

    public StatusFilter StatusFilter
    {
        get;
        set
        {
            if (SetValue(ref field, value))
            {
                OnPropertyChanged(nameof(StatusFilterIsAll));
                OnPropertyChanged(nameof(StatusFilterIsPassed));
                OnPropertyChanged(nameof(StatusFilterIsFailed));
                OnPropertyChanged(nameof(StatusFilterIsSkipped));
                OnPropertyChanged(nameof(StatusFilterIsNotRun));
                ApplyFilter();
            }
        }
    } = StatusFilter.All;

    // ToggleButton bindings (one-way set true; user can't un-toggle the active one)
    public bool StatusFilterIsAll
    {
        get => StatusFilter == StatusFilter.All;
        set { if (value) StatusFilter = StatusFilter.All; else OnPropertyChanged(nameof(StatusFilterIsAll)); }
    }
    public bool StatusFilterIsPassed
    {
        get => StatusFilter == StatusFilter.Passed;
        set { if (value) StatusFilter = StatusFilter.Passed; else OnPropertyChanged(nameof(StatusFilterIsPassed)); }
    }
    public bool StatusFilterIsFailed
    {
        get => StatusFilter == StatusFilter.Failed;
        set { if (value) StatusFilter = StatusFilter.Failed; else OnPropertyChanged(nameof(StatusFilterIsFailed)); }
    }
    public bool StatusFilterIsSkipped
    {
        get => StatusFilter == StatusFilter.Skipped;
        set { if (value) StatusFilter = StatusFilter.Skipped; else OnPropertyChanged(nameof(StatusFilterIsSkipped)); }
    }
    public bool StatusFilterIsNotRun
    {
        get => StatusFilter == StatusFilter.NotRun;
        set { if (value) StatusFilter = StatusFilter.NotRun; else OnPropertyChanged(nameof(StatusFilterIsNotRun)); }
    }

    public void ApplyFilter()
    {
        var needle = FilterText.Trim();
        foreach (var root in TestCases)
            ApplyFilter(root, needle);
    }

    bool ApplyFilter(TestNodeViewModel node, string needle)
    {
        if (node is TestGroupViewModel group)
        {
            bool anyChildVisible = false;
            foreach (var child in group.Children)
                anyChildVisible |= ApplyFilter(child, needle);
            group.IsVisible = anyChildVisible;
            return anyChildVisible;
        }
        else if (node is TestCaseViewModel testCase)
        {
            bool matchesText = needle.Length == 0 || testCase.DisplayName.Contains(needle, StringComparison.OrdinalIgnoreCase);
            bool matchesStatus = StatusFilter switch
            {
                StatusFilter.All => true,
                StatusFilter.Passed => testCase.Succeeded,
                StatusFilter.Failed => testCase.Failed,
                StatusFilter.Skipped => testCase.Skipped,
                StatusFilter.NotRun => !testCase.Succeeded && !testCase.Failed && !testCase.Skipped && !testCase.Running,
                _ => true,
            };
            testCase.IsVisible = matchesText && matchesStatus;
            return testCase.IsVisible;
        }
        return false;
    }

    // === Counts / summary ===


    public int PassedCount { get; private set => SetValue(ref field, value); }
    public int FailedCount { get; private set => SetValue(ref field, value); }
    public int SkippedCount { get; private set => SetValue(ref field, value); }
    public int NotRunCount { get; private set => SetValue(ref field, value); }
    public int TotalCount { get; private set => SetValue(ref field, value); }
    public decimal TotalElapsedSeconds
    {
        get;
        private set
        {
            if (SetValue(ref field, value))
                OnPropertyChanged(nameof(TotalElapsedDisplay));
        }
    }

    public string TotalElapsedDisplay
    {
        get
        {
            var s = (double)TotalElapsedSeconds;
            return s < 60 ? $"{s:F2}s" : $"{(int)(s / 60)}m {s % 60:F1}s";
        }
    }

    public bool HasFailures => FailedCount > 0;

    /// <summary>True when there's at least one failed test AND no run in progress; drives the "Re-run failed" button.</summary>
    public bool CanRunFailed => FailedCount > 0 && !RunningTests;

    void RecomputeCounts()
    {
        int passed = 0, failed = 0, skipped = 0, notRun = 0, total = 0;
        decimal elapsed = 0m;
        foreach (var c in EnumerateAllTestCases())
        {
            total++;
            if (c.Failed) failed++;
            else if (c.Skipped) skipped++;
            else if (c.Succeeded) passed++;
            else notRun++;
            if (c.HasElapsed) elapsed += c.ElapsedSeconds!.Value;
        }
        PassedCount = passed;
        FailedCount = failed;
        SkippedCount = skipped;
        NotRunCount = notRun;
        TotalCount = total;
        TotalElapsedSeconds = elapsed;
        OnPropertyChanged(nameof(HasFailures));
        OnPropertyChanged(nameof(CanRunFailed));
    }

    // === Selection ===

    /// <summary>Test cases currently selected in the tree (multi-select).</summary>
    public ObservableCollection<TestCaseViewModel> SelectedCases { get; } = [];

    public bool HasSelection => SelectedCases.Count > 0;

    public void OnSelectionChanged()
    {
        OnPropertyChanged(nameof(HasSelection));
    }

    // === Commands invoked from view ===

    public void RunSelected() => _ = RunSelectedCases(interactive: false);
    public void RunSelectedInteractive() => _ = RunSelectedCases(interactive: true);

    public void RunFailedCmd() => _ = RunFailed(interactive: false);

    public void ClearFilter() => FilterText = string.Empty;

    // === State ===

    public double TestCompletion
    {
        get;
        set => SetValue(ref field, value);
    }

    public bool RunningTests
    {
        get;
        set
        {
            if (SetValue(ref field, value))
                OnPropertyChanged(nameof(CanRunFailed));
        }
    }

    public ObservableCollection<TestNodeViewModel> TestCases { get; } = [];

    public Action<bool>? SetInteractiveMode { get; set; }
    public Action<bool>? SetForceSaveImage { get; set; }
    /// <summary>Pushes the selected RenderDoc capture mode to the underlying static
    /// (typically <c>GameTestBase.RenderDocMode</c>). Accepts <c>null</c> ("never"), "error", "always".</summary>
    public Action<string?>? SetRenderDocMode { get; set; }

    /// <summary>Persistent toggle: when on, every run sets <c>ForceSaveImageOnSuccess</c> so
    /// the rendered output is written to disk even when the gold comparison passes.</summary>
    public bool IsForceSaveImage
    {
        get;
        set
        {
            if (SetValue(ref field, value))
                SetForceSaveImage?.Invoke(value);
        }
    }

    RenderDocCaptureMode renderDocMode = RenderDocCaptureMode.Never;
    /// <summary>Selected RenderDoc capture mode. Default: <see cref="RenderDocCaptureMode.Never"/>.
    /// Setting this pushes the matching string ("error"/"always"/null) to <see cref="SetRenderDocMode"/>.</summary>
    public RenderDocCaptureMode RenderDocMode
    {
        get => renderDocMode;
        set
        {
            if (SetValue(ref renderDocMode, value))
            {
                var s = value switch
                {
                    RenderDocCaptureMode.OnError => "error",
                    RenderDocCaptureMode.Always => "always",
                    _ => (string?)null,
                };
                SetRenderDocMode?.Invoke(s);
            }
        }
    }

    public bool HasRenderDocMode => SetRenderDocMode != null;

    /// <summary>Invoked on the UI thread when a test case starts; the view selects it in the
    /// tree so its output streams into the inspect panel.</summary>
    public Action<TestCaseViewModel>? FocusTest { get; set; }

    // The test case currently being executed by RunTestCases. Also drives the live-output
    // ticker and the OnImageComparison callback's vm-attach logic.
    TestCaseViewModel? currentlyRunningCase;

    // === Mobile / narrow-viewport state ===

    /// <summary>True when the main split is in stacked (phone/portrait) mode. Drives the
    /// drill-down nav (tap-to-detail / back) and hides per-row run buttons.</summary>
    public bool IsNarrowMode
    {
        get;
        set
        {
            if (SetValue(ref field, value))
            {
                OnPropertyChanged(nameof(IsWideMode));
                OnPropertyChanged(nameof(RowMinHeight));
                // Leaving narrow mode also exits the detail page so wide layout shows both panes.
                if (!value) IsDetailPageActive = false;
            }
        }
    }

    /// <summary>Inverse of <see cref="IsNarrowMode"/>; convenience for per-row "show on wide
    /// only" visibility bindings.</summary>
    public bool IsWideMode => !IsNarrowMode;

    /// <summary>Minimum tree-row height; bumped in narrow mode to give touch targets enough
    /// surface to be reliably tappable.</summary>
    public double RowMinHeight => IsNarrowMode ? 44 : 0;

    /// <summary>In narrow mode, true means the detail pane is showing instead of the list.
    /// Ignored in wide mode.</summary>
    public bool IsDetailPageActive
    {
        get;
        set => SetValue(ref field, value);
    }
}
