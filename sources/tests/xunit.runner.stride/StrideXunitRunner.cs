// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Xunit;
using Xunit.Abstractions;
using xunit.runner.stride.ViewModels;

namespace xunit.runner.stride;

public static class StrideXunitRunner
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
#if !ANDROID && !IOS
    public static void Main(string[] args, Action<bool>? setInteractiveMode = null, Action<bool>? setForceSaveImage = null, Action<string?>? setRenderDocMode = null, Action<Action<ImageCompareResult>>? subscribeImageComparison = null)
    {
        // Stash on App's static slots — Android's MainActivity assigns the same way, so the App
        // initialization code reads from a single place regardless of entry point.
        App.SetInteractiveMode = setInteractiveMode;
        App.SetForceSaveImage = setForceSaveImage;
        App.SetRenderDocMode = setRenderDocMode;
        App.SubscribeImageComparison = subscribeImageComparison;

        if (IsHeadless())
        {
            setInteractiveMode?.Invoke(false);
            Environment.ExitCode = RunHeadless(args);
            return;
        }

        var builder = BuildAvaloniaApp()
            .SetupWithLifetime(new ClassicDesktopStyleApplicationLifetime());
        if (builder.Instance is App app)
        {
            app.Run(app.cts.Token);
        }
    }
#endif

    // Discover and run all tests via XunitFrontController, printing a compact summary so direct
    // exe invocation isn't a no-op. Test Explorer / dotnet test route through the xunit adapter
    // and bypass this path entirely.
    //
    // Iterates over every loaded assembly whose name contains ".Tests" — supports combined-test
    // hosts (Stride.Tests.Combined) that aggregate multiple suites via ProjectReference. Each
    // assembly runs as an independent xunit pass with its own .trx, matching the per-suite output
    // CI scripts already consume; total/failed counts are summed for the process exit code.
    internal static int RunHeadless(string[] args)
    {
        var filter = ParseVstestFilter(args);
        var repeat = ParseRepeat(args);
        // Always discover: a single-suite host has just the entry assembly; the Combined host
        // (Stride.Tests.Combined) has 0 [Fact]s of its own and must load every inner suite.
        IReadOnlyList<Assembly> assemblies = DiscoverTestAssemblies();

        int total = 0, passed = 0, failed = 0, skipped = 0;
        decimal totalTime = 0m;
        foreach (var testAssembly in assemblies)
        {
            // No bundle setup here: GameTestBase resolves AssetBundleName from its concrete
            // subclass's assembly, which lives in the per-suite .Tests.dll. The Combined host
            // packages each suite's bundle as <SuiteName>.bundle alongside the meta's default
            // bundle, so each test naturally loads its own suite's bundle by name.
            using var controller = new StrideTestController(testAssembly);

            using var discoverySink = new TestDiscoverySink();
            controller.Find(includeSourceInformation: false, discoverySink, TestFrameworkOptions.ForDiscovery());
            discoverySink.Finished.WaitOne();

            IList<ITestCase> testCases = discoverySink.TestCases;
            if (filter is not null)
                testCases = testCases.Where(filter).ToList();
            if (testCases.Count == 0)
                continue;

            Console.WriteLine(filter is null
                ? $"Discovered {discoverySink.TestCases.Count} tests in {testAssembly.GetName().Name}"
                : $"Discovered {discoverySink.TestCases.Count} tests in {testAssembly.GetName().Name}, running {testCases.Count} after --filter");

            // --repeat=N: run the filtered set up to N times, stop on first failed iteration.
            // Pass = N consecutive greens, fail = trx captures the failing iteration (no entry-
            // count inflation for downstream test reporters). Fresh TrxWriter per iteration so
            // a clean pass only persists the last run's results.
            int iterTotal = 0, iterPassed = 0, iterFailed = 0, iterSkipped = 0;
            decimal iterTime = 0m;
            TrxWriter lastTrxSink = null!;
            for (int iter = 0; iter < repeat; iter++)
            {
                if (repeat > 1)
                    Console.WriteLine($"  iteration {iter + 1}/{repeat}");
                using var executionSink = new ConsoleExecutionSink();
                var trxSink = new TrxWriter();
                lastTrxSink = trxSink;
                var composite = new CompositeSink(executionSink, trxSink);
                controller.RunTests(testCases, composite, TestFrameworkOptions.ForExecution());
                executionSink.Finished.WaitOne();
                trxSink.Finished.WaitOne();
                iterTotal   = executionSink.Total;
                iterPassed  = executionSink.Passed;
                iterFailed  = executionSink.Failed;
                iterSkipped = executionSink.Skipped;
                iterTime    = executionSink.ExecutionTime;
                if (iterFailed > 0) break;
            }
            lastTrxSink.WriteTo(GetTrxPath(testAssembly));

            total   += iterTotal;
            passed  += iterPassed;
            failed  += iterFailed;
            skipped += iterSkipped;
            totalTime += iterTime;
            Console.WriteLine($"  {testAssembly.GetName().Name}: total={iterTotal} passed={iterPassed} failed={iterFailed} skipped={iterSkipped}");
        }

        Console.WriteLine($"Total: {total}, Passed: {passed}, Failed: {failed}, Skipped: {skipped}, Time: {totalTime:F2}s");
        return failed > 0 ? 1 : 0;
    }

    // Discover test assemblies whose name contains ".Tests". Two passes because the Combined host
    // (Stride.Tests.Combined) has no code using inner-suite types, so the C# compiler drops the
    // ProjectReference-derived assembly refs from its IL — Assembly.Load against GetReferencedAssemblies
    // would miss them. Second pass scans the .app/APK/bin directory for *.Tests.dll and LoadFrom each.
    private static List<Assembly> DiscoverTestAssemblies()
    {
        // Android has no managed entry point, so Assembly.GetEntryAssembly() returns null; the
        // launcher sets App.TestAssembly to the host assembly instead. Desktop/iOS keep a real
        // entry assembly. Without this fallback the null deref aborts the headless run silently.
        var entry = Assembly.GetEntryAssembly() ?? App.TestAssembly
            ?? throw new InvalidOperationException("No test host assembly: GetEntryAssembly() and App.TestAssembly are both null.");
        foreach (var refName in entry.GetReferencedAssemblies())
        {
            if (refName.Name?.Contains(".Tests", StringComparison.Ordinal) != true)
                continue;
            try { Assembly.Load(refName); } catch { /* missing or unavailable; let scan skip it */ }
        }
        try
        {
            foreach (var dll in Directory.EnumerateFiles(AppContext.BaseDirectory, "*.Tests.dll", SearchOption.TopDirectoryOnly))
            {
                try { Assembly.LoadFrom(dll); } catch { /* unloadable on this platform; let scan skip it */ }
            }
        }
        catch { /* AppContext.BaseDirectory not enumerable */ }
        var result = new List<Assembly> { entry };
        foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (a == entry || a.IsDynamic) continue;
            if (a.GetName().Name?.Contains(".Tests", StringComparison.Ordinal) == true)
                result.Add(a);
        }
        return result;
    }

    // --repeat=N or "--repeat N": run the filtered test set N times in the same process.
    // Default 1. Used to catch sporadic failures (decoder timing, GPU race) cheaply — one
    // assembly load + one runner setup + N executions of the same case list.
    private static int ParseRepeat(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            string? value = null;
            if (args[i] == "--repeat" && i + 1 < args.Length) value = args[i + 1];
            else if (args[i].StartsWith("--repeat=", StringComparison.Ordinal)) value = args[i]["--repeat=".Length..];
            if (value != null && int.TryParse(value, out var n) && n >= 1) return n;
        }
        return 1;
    }

    // Minimal `dotnet test --filter` / VSTest filter parser: binary ops `<Property><op><Value>`
    // where op ∈ { =, !=, ~, !~ } and Property ∈ { FullyQualifiedName, DisplayName, Name },
    // combinable with | and & (OR binds loosest, as in vstest). Parens/escapes unsupported.
    private static Func<ITestCase, bool>? ParseVstestFilter(string[] args)
    {
        for (int i = 0; i + 1 < args.Length; i++)
            if (args[i] == "--filter")
                return ParseVstestFilter(args[i + 1]);
        return null;
    }

    // Shared by the desktop --filter args path and the Android/iOS headless launcher, which
    // passes the raw expression as an intent/launch parameter (App.HeadlessFilter).
    internal static Func<ITestCase, bool>? ParseVstestFilter(string expr)
    {
        if (string.IsNullOrEmpty(expr)) return null;
        var orParts = expr.Split('|');
        if (orParts.Length > 1)
        {
            var parsed = Array.ConvertAll(orParts, ParseVstestFilter);
            return Array.IndexOf(parsed, null) >= 0 ? null : tc => Array.Exists(parsed, p => p!(tc));
        }
        var andParts = expr.Split('&');
        if (andParts.Length > 1)
        {
            var parsed = Array.ConvertAll(andParts, ParseVstestFilter);
            return Array.IndexOf(parsed, null) >= 0 ? null : tc => Array.TrueForAll(parsed, p => p!(tc));
        }
        // Order matters: check 2-char ops before 1-char ones to avoid splitting on the wrong byte.
        foreach (var op in new[] { "!~", "!=", "~", "=" })
        {
            int idx = expr.IndexOf(op, StringComparison.Ordinal);
            if (idx < 0) continue;
            var prop = expr[..idx].Trim();
            var val = expr[(idx + op.Length)..].Trim();
            Func<ITestCase, string> get = prop switch
            {
                "FullyQualifiedName" => tc => tc.TestMethod.TestClass.Class.Name + "." + tc.TestMethod.Method.Name,
                "DisplayName"        => tc => tc.DisplayName,
                "Name"               => tc => tc.TestMethod.Method.Name,
                _                    => tc => tc.DisplayName,
            };
            return op switch
            {
                "="  => tc => string.Equals(get(tc), val, StringComparison.OrdinalIgnoreCase),
                "!=" => tc => !string.Equals(get(tc), val, StringComparison.OrdinalIgnoreCase),
                "~"  => tc => get(tc).Contains(val, StringComparison.OrdinalIgnoreCase),
                "!~" => tc => !get(tc).Contains(val, StringComparison.OrdinalIgnoreCase),
                _    => null,
            };
        }
        return null;
    }

    // Headless trx: Android writes to internal FilesDir (targetSdk 30+ scoped storage
    // blocks app writes through the FUSE-bound external-files path); iOS writes to the
    // sandboxed Documents (bundle dir is read-only); host scripts pull via
    // `adb shell run-as <pkg>` / `xcrun simctl get_app_container ... data` respectively.
    // Desktop drops it beside the test binary so `dotnet test --logger trx` parity tools
    // find it without extra config.
    internal static string GetTrxPath(Assembly testAssembly)
    {
        var name = testAssembly.GetName().Name ?? "tests";
#if ANDROID
        var root = Android.App.Application.Context.FilesDir!.AbsolutePath;
        return Path.Combine(root, "tests", "local", name, $"{name}.trx");
#elif IOS
        var root = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Path.Combine(root, "tests", "local", name, $"{name}.trx");
#else
        var binDir = Path.GetDirectoryName(testAssembly.Location) ?? AppContext.BaseDirectory;
        return Path.Combine(binDir, "TestResults", $"{name}.trx");
#endif
    }

    internal sealed class CompositeSink : IMessageSinkWithTypes
    {
        private readonly IMessageSinkWithTypes[] inners;
        public CompositeSink(params IMessageSinkWithTypes[] inners) => this.inners = inners;
        public bool OnMessageWithTypes(IMessageSinkMessage message, HashSet<string> messageTypes)
        {
            var keepGoing = true;
            foreach (var inner in inners)
                keepGoing &= inner.OnMessageWithTypes(message, messageTypes);
            return keepGoing;
        }
        public void Dispose() { foreach (var inner in inners) (inner as IDisposable)?.Dispose(); }
    }

    private sealed class ConsoleExecutionSink : TestMessageSink
    {
        public int Total;
        public int Passed;
        public int Failed;
        public int Skipped;
        public decimal ExecutionTime;
        public ManualResetEvent Finished { get; } = new(initialState: false);

        public ConsoleExecutionSink()
        {
            Execution.TestPassedEvent       += a => { Interlocked.Increment(ref Passed); Interlocked.Increment(ref Total); };
            Execution.TestSkippedEvent      += a => { Interlocked.Increment(ref Skipped); Interlocked.Increment(ref Total);
                Console.WriteLine($"  SKIP {a.Message.Test.DisplayName}: {a.Message.Reason}"); };
            Execution.TestFailedEvent       += a => { Interlocked.Increment(ref Failed); Interlocked.Increment(ref Total);
                Console.WriteLine($"  FAIL {a.Message.Test.DisplayName}");
                if (a.Message.Messages is { Length: > 0 } msgs) Console.WriteLine($"    {msgs[0]}");
                if (a.Message.StackTraces is { Length: > 0 } st && st[0] is { Length: > 0 }) Console.WriteLine($"    {st[0].Replace("\n", "\n    ")}"); };
            Execution.TestAssemblyFinishedEvent += a => { ExecutionTime = a.Message.ExecutionTime; Finished.Set(); };
        }
    }

    private static bool IsHeadless()
    {
        // Avalonia UI shows only when STRIDE_TESTS_INTERACTIVE=1 (set by per-project
        // launchSettings.json profiles). Test Explorer / dotnet test / CI default to headless.
        return Environment.GetEnvironmentVariable("STRIDE_TESTS_INTERACTIVE") != "1";
    }

#if !ANDROID && !IOS
    // Avalonia configuration, don't remove; also used by visual designer.
    // Mobile platforms wire Avalonia through their own AppDelegate / Activity (CustomizeAppBuilder
    // overrides), not through this desktop BuildAvaloniaApp helper.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new Win32PlatformOptions
            {
                // Use Software rendering, otherwise default renderer (OpenGL) interfere with GPU capture tools such as RenderDoc
                RenderingMode = new[] { Win32RenderingMode.Software }
            })
            .WithInterFont()
            .LogToTrace();
#endif
}
