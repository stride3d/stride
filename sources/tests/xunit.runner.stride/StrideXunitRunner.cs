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
#if !ANDROID
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

    // Discover and run all tests in the entry assembly via XunitFrontController, printing a
    // compact summary so direct exe invocation isn't a no-op. Test Explorer / dotnet test
    // route through the xunit adapter and bypass this path entirely.
    private static int RunHeadless(string[] args)
    {
        var testAssembly = App.TestAssembly ?? Assembly.GetEntryAssembly()!;
        using var controller = new StrideTestController(testAssembly);

        using var discoverySink = new TestDiscoverySink();
        controller.Find(includeSourceInformation: false, discoverySink, TestFrameworkOptions.ForDiscovery());
        discoverySink.Finished.WaitOne();

        var filter = ParseVstestFilter(args);
        IList<ITestCase> testCases = discoverySink.TestCases;
        if (filter is not null)
            testCases = testCases.Where(filter).ToList();

        Console.WriteLine(filter is null
            ? $"Discovered {discoverySink.TestCases.Count} tests in {testAssembly.GetName().Name}"
            : $"Discovered {discoverySink.TestCases.Count} tests in {testAssembly.GetName().Name}, running {testCases.Count} after --filter");

        using var executionSink = new ConsoleExecutionSink();
        using var trxSink = new TrxWriter();
        var composite = new CompositeSink(executionSink, trxSink);
        controller.RunTests(testCases, composite, TestFrameworkOptions.ForExecution());
        executionSink.Finished.WaitOne();
        trxSink.Finished.WaitOne();

        trxSink.WriteTo(GetTrxPath(testAssembly));

        Console.WriteLine($"Total: {executionSink.Total}, Passed: {executionSink.Passed}, Failed: {executionSink.Failed}, Skipped: {executionSink.Skipped}, Time: {executionSink.ExecutionTime:F2}s");
        return executionSink.Failed > 0 ? 1 : 0;
    }

    // Minimal `dotnet test --filter` / VSTest filter parser: single binary op `<Property><op><Value>`
    // where op ∈ { =, !=, ~, !~ } and Property ∈ { FullyQualifiedName, DisplayName, Name }.
    // Compound (& |) and parens aren't supported — the "run one test" path doesn't need them.
    private static Func<ITestCase, bool>? ParseVstestFilter(string[] args)
    {
        for (int i = 0; i + 1 < args.Length; i++)
        {
            if (args[i] != "--filter") continue;
            var expr = args[i + 1];
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
        }
        return null;
    }

    // Headless trx: Android writes to internal FilesDir (targetSdk 30+ scoped storage
    // blocks app writes through the FUSE-bound external-files path); host script pulls
    // via `adb shell run-as <pkg>`. Desktop drops it beside the test binary so
    // `dotnet test --logger trx` parity tools find it without extra config.
    internal static string GetTrxPath(Assembly testAssembly)
    {
        var name = testAssembly.GetName().Name ?? "tests";
#if ANDROID
        var root = Android.App.Application.Context.FilesDir!.AbsolutePath;
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

#if !ANDROID
    // Avalonia configuration, don't remove; also used by visual designer.
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
