// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Concurrent;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Microsoft.Build.Locator;
using Stride.Core.Diagnostics;
using Stride.Crash;
using Stride.Crash.ViewModels;

namespace Stride.GameStudio.Avalonia.Desktop;

internal sealed class Program
{
    private const int MaxLogMessageCount = 10;
    private static readonly ConcurrentQueue<string> logRingbuffer = new();
    private static int terminating;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        try
        {
            MSBuildLocator.RegisterDefaults();
            Thread.CurrentThread.Name = "Main thread";

            // Listen to logger for crash report
            GlobalLogger.GlobalMessageLogged += OnGlobalMessageLogged;

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args, ShutdownMode.OnMainWindowClose);
        }
        catch (Exception ex)
        {
            HandleException(ex, CrashLocation.Main);
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
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
        var reportArgs = new CrashReportArgs
        {
            Exception = exception,
            Location = location,
            Logs = [.. logRingbuffer],
            ThreadName = Thread.CurrentThread.Name
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
