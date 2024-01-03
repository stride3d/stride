// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Stride.Launcher.Crash;

namespace Stride.Launcher;

internal static partial class Launcher
{
    private static int terminating;

    [STAThread]
    public static LauncherErrorCode Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        try
        {
            return (LauncherErrorCode)BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            HandleException(ex);
            return LauncherErrorCode.ErrorWhileRunningServer;
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
        if (Application.Current == null)
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

                // Then setup the new application
                // HACK: SetupUnsafe is internal and we can't call Setup mutiple times
                typeof(AppBuilder).GetMethod("SetupUnsafe", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(appBuilder, null);

                AppMain(appBuilder.Instance!, args);
            });
        }

        static void AppMain(Application app, CrashReportArgs args)
        {
            var cts = new CancellationTokenSource();
            var window = new CrashReportWindow { Topmost = true };
            window.DataContext = new CrashReportViewModel(args, window.Clipboard!.SetTextAsync, cts);
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
            HandleException(e.ExceptionObject as Exception);
        }
    }

    private static void HandleException(Exception? exception)
    {
        if (exception == null) return;

        // prevent multiple crash reports
        if (Interlocked.CompareExchange(ref terminating, 1, 0) == 1) return;

        var englishCulture = new CultureInfo("en-US");
        Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = englishCulture;
        var reportArgs = new CrashReportArgs(exception, Thread.CurrentThread.Name);
        CrashReport(reportArgs);
    }
}
