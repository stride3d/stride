// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Xunit;
using Xunit.Abstractions;

namespace xunit.runner.stride;

public static class StrideXunitRunner
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    public static void Main(string[] _, Action<bool>? setInteractiveMode = null, Action<bool>? setForceSaveImage = null)
    {
        if (IsHeadless())
        {
            setInteractiveMode?.Invoke(false);
            Environment.ExitCode = RunHeadless();
            return;
        }

        var builder = BuildAvaloniaApp(setInteractiveMode, setForceSaveImage)
            .SetupWithLifetime(new ClassicDesktopStyleApplicationLifetime());
        if (builder.Instance is App app)
        {
            app.Run(app.cts.Token);
        }
    }

    // Discover and run all tests in the entry assembly via XunitFrontController, printing a
    // compact summary so direct exe invocation isn't a no-op. Test Explorer / dotnet test
    // route through the xunit adapter and bypass this path entirely.
    private static int RunHeadless()
    {
        var assemblyFileName = Assembly.GetEntryAssembly()!.Location;
        using var controller = new XunitFrontController(AppDomainSupport.Denied, assemblyFileName);

        using var discoverySink = new TestDiscoverySink();
        controller.Find(includeSourceInformation: false, discoverySink, TestFrameworkOptions.ForDiscovery());
        discoverySink.Finished.WaitOne();

        Console.WriteLine($"Discovered {discoverySink.TestCases.Count} tests in {Path.GetFileName(assemblyFileName)}");

        using var executionSink = new ConsoleExecutionSink();
        controller.RunTests(discoverySink.TestCases, executionSink, TestFrameworkOptions.ForExecution());
        executionSink.Finished.WaitOne();

        Console.WriteLine($"Total: {executionSink.Total}, Passed: {executionSink.Passed}, Failed: {executionSink.Failed}, Skipped: {executionSink.Skipped}, Time: {executionSink.ExecutionTime:F2}s");
        return executionSink.Failed > 0 ? 1 : 0;
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

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp(Action<bool>? setInteractiveMode = null, Action<bool>? setForceSaveImage = null)
        => AppBuilder.Configure(() => new App { setInteractiveMode = setInteractiveMode, setForceSaveImage = setForceSaveImage })
            .UsePlatformDetect()
            .With(new Win32PlatformOptions
            {
                // Use Software rendering, otherwise default renderer (OpenGL) interfere with GPU capture tools such as RenderDoc
                RenderingMode = new[] { Win32RenderingMode.Software }
            })
            .WithInterFont()
            .LogToTrace();
}
