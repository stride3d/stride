// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Concurrent;
using System.Globalization;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using Stride.Core.Assets;
using Stride.Core.Diagnostics;
using Stride.Crash;
using Stride.Crash.ViewModels;
using Stride.GameStudio.Avalonia.Services;

namespace Stride.GameStudio.Avalonia.Desktop;

internal sealed class Program
{
    private const int MaxLogMessageCount = 10;
    private static readonly ConcurrentQueue<string> logRingbuffer = new();
    private static int terminating;
    private static bool resetGraphicsApiPreference;

    /// <summary>
    /// Whether the graphics API preference of the settings was unavailable at startup and must be reset to the default.
    /// </summary>
    public static bool ResetGraphicsApiPreference => resetGraphicsApiPreference;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        // The persisted graphics API preference isn't staged in this build: fall back to the platform default and reset
        // the setting, so the user isn't stuck in a fail-at-launch loop (the WPF Game Studio asks first; there is no window yet here).
        if (GraphicsApiHostResolver.UnavailablePreference is { } unavailableApi)
        {
            var fallback = GraphicsApiHostResolver.UseFallback();
            Console.Error.WriteLine($"Graphics API '{unavailableApi}' (from Game Studio settings) is not available in this build; continuing with '{fallback}' and resetting the setting to Default.");
            resetGraphicsApiPreference = true;
        }

        try
        {
            // MSBuild is located when the first session opens (PackageSessionPublicHelper.FindAndSetMSBuildVersion loads it in its
            // own context: the SDK's NuGet assemblies must not collide with the editor's own)
            Thread.CurrentThread.Name = "Main thread";

            // The managed handlers can't see a native access violation (native interop, GPU drivers): it kills the process
            // first. The native handler captures such a crash for the reporter (Windows only; a no-op elsewhere).
            Stride.CrashReport.NativeCrashReporting.Install("GameStudio");

            // Listen to logger for crash report
            GlobalLogger.GlobalMessageLogged += OnGlobalMessageLogged;

            if (GraphicsApiPreferenceInitializer.OverriddenPreference is { } overriddenApi)
                GlobalLogger.GetLogger("GameStudio").Warning($"The graphics API '{overriddenApi}' of the Game Studio settings is not used: the Avalonia Game Studio renders with {GraphicsApiSelector.HostPreference} for now.");

            // Parse launcher arguments before starting the Avalonia app so that
            // App.LauncherNotifier is set before the first window opens.
            ParseLauncherArgs(args);
            // The session to open and the services to turn off (/DisableThumbnails, /DisablePreview, /DebugEditorGraphics)
            App.Options = StartupOptions.Parse(args);
            foreach (var unknown in App.Options.UnknownArguments)
                GlobalLogger.GetLogger("GameStudio").Warning($"Unknown command line option [{unknown}] ignored.");
            // Another session opens in a new process, started as this one was (from the working directory it has now)
            App.ProcessLauncher = new EditorProcessLauncher(typeof(Program).Assembly.Location);

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args, ShutdownMode.OnMainWindowClose);
        }
        catch (Exception ex)
        {
            HandleException(ex, CrashLocation.Main);
        }
    }

    /// <summary>
    /// Parses the <c>/LauncherWindowHandle</c> (Windows) and <c>/LauncherPipe</c> (cross-platform)
    /// arguments injected by the launcher when <c>AutoCloseLauncher</c> is on, and wires up
    /// <see cref="Stride.GameStudio.Avalonia.App.LauncherNotifier"/> to signal the launcher once
    /// the main window is loaded.
    /// </summary>
    private static void ParseLauncherArgs(string[] args)
    {
        IntPtr launcherWindowHandle = IntPtr.Zero;
        string? launcherPipeName = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "/LauncherWindowHandle" && i + 1 < args.Length)
            {
                if (long.TryParse(args[++i], out var hwnd))
                    launcherWindowHandle = new IntPtr(hwnd);
            }
            else if (args[i] == "/LauncherPipe" && i + 1 < args.Length)
            {
                launcherPipeName = args[++i];
            }
        }

        if (launcherWindowHandle != IntPtr.Zero || launcherPipeName is not null)
        {
            var handle = launcherWindowHandle;
            var pipe = launcherPipeName;
            Stride.GameStudio.Avalonia.App.LauncherNotifier = () => NotifyLauncher(handle, pipe);
        }
    }

    /// <summary>
    /// Signals the launcher that Game Studio has started.
    /// On Windows: sends a Win32 <c>WM_CLOSE</c> message to the launcher window handle.
    /// On other platforms (e.g. Linux): connects to the launcher's named pipe and writes a byte.
    /// </summary>
    private static void NotifyLauncher(IntPtr windowHandle, string? pipeName)
    {
        if (OperatingSystem.IsWindows() && windowHandle != IntPtr.Zero)
        {
            SendCloseMessage(windowHandle);
            return;
        }

        if (pipeName is not null)
        {
            _ = SendPipeSignalAsync(pipeName);
        }
    }

    [SupportedOSPlatform("windows")]
    private static void SendCloseMessage(IntPtr hWnd)
    {
        const uint WM_CLOSE = 0x0010;
        NativeSendMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
    }

    [DllImport("user32.dll", EntryPoint = "SendMessage")]
    [SupportedOSPlatform("windows")]
    private static extern IntPtr NativeSendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private static async Task SendPipeSignalAsync(string pipeName)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
            await client.ConnectAsync(TimeSpan.FromSeconds(10), default);
            client.WriteByte(1);
        }
        catch (Exception) { }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            // Linux: Vulkan first, so that the compositor can import the memory of the Vulkan textures the editor's games
            // render into (GpuFramePresenter); then Avalonia's usual GLX, then software rendering
            .With(new X11PlatformOptions { RenderingMode = [X11RenderingMode.Vulkan, X11RenderingMode.Glx, X11RenderingMode.Software] })
            .WithInterFont()
            .LogToTrace();

    private static void CrashReport(CrashReportArgs args)
    {
        // Note: we need a new app because the main one may be already shutting down
        var appBuilder = AppBuilder.Configure<Application>()
            .UsePlatformDetect();
        if (args.Location == CrashLocation.Main && Application.Current == null)
        {
            appBuilder = appBuilder
                .SetupWithLifetime(new ClassicDesktopStyleApplicationLifetime());
            AppMain(appBuilder.Instance!, args);
        }
        else
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                // First hide the main window
                ((IClassicDesktopStyleApplicationLifetime?)Application.Current?.ApplicationLifetime)?.MainWindow?.Hide();

                AppMain(appBuilder.Instance!, args);
            });
        }

        static void AppMain(Application app, CrashReportArgs args)
        {
            var cts = new CancellationTokenSource();
            var window = new CrashReportWindow { Topmost = true };
            window.DataContext = new CrashReportViewModel("Stride Game Studio", args, window.Clipboard!.SetTextAsync, cts);
            window.Closed += (_, __) => cts.Cancel();
            if (!window.IsVisible)
            {
                window.Show();
            }
            app.Run(cts.Token);
        }
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.IsTerminating)
        {
            HandleException(e.ExceptionObject as Exception, CrashLocation.UnhandledException);
        }
    }

    private static void HandleException(Exception? exception, CrashLocation location)
    {
        if (exception == null) return;

        // prevent multiple crash reports
        if (Interlocked.CompareExchange(ref terminating, 1, 0) == 1) return;

        var englishCulture = new CultureInfo("en-US");
        Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = englishCulture;
        // The exception goes to the standard error too: the crash report window may not open (no display, a broken rendering)
        Console.Error.WriteLine($"Game Studio crashed ({location}):");
        Console.Error.WriteLine(exception);
        // Snapshot the other threads on the faulting thread (the crashing thread's stack is in the exception)
        var threads = Stride.CrashReport.ThreadSnapshot.CaptureAtCurrentThread(out var crashedThreadId, out var crashedThreadName);
        var reportArgs = new CrashReportArgs
        {
            Exception = exception,
            Location = location,
            Logs = [.. logRingbuffer],
            ThreadName = crashedThreadName ?? Thread.CurrentThread.Name,
            ThreadId = crashedThreadId,
            Threads = threads,
        };
        CrashReport(reportArgs);
    }

    private static void OnGlobalMessageLogged(ILogMessage logMessage)
    {
        if (logMessage.Type <= LogMessageType.Warning) return;

        logRingbuffer.Enqueue(logMessage.ToString()!);
        while (logRingbuffer.Count > MaxLogMessageCount)
        {
            logRingbuffer.TryDequeue(out _);
        }
    }
}
