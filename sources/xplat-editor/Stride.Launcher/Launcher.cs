// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Stride.Core.Assets.Editor;
using Stride.Core.Presentation.Avalonia.Windows;
using Stride.Core.Presentation.Services;
using Stride.Core.Windows;
using Stride.Launcher.Crash;

namespace Stride.Launcher;

internal static partial class Launcher
{
    private static int terminating;
    internal static FileLock? Mutex;

    [STAThread]
    public static LauncherErrorCode Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        try
        {
            var arguments = ProcessArguments(args);
            return ProcessAction(arguments);
        }
        catch (Exception ex)
        {
            HandleException(ex);
            return LauncherErrorCode.ErrorWhileRunningServer;
        }
    }

    private static LauncherErrorCode ProcessAction(LauncherArguments args)
    {
        var result = LauncherErrorCode.UnknownError;
        foreach (var action in args.Actions)
        {
            switch (action)
            {
                case LauncherArguments.ActionType.Run:
                    result = TryRun(args.Args);
                    break;
                case LauncherArguments.ActionType.Uninstall:
                    result = Uninstall();
                    break;
                default:
                    // Unknown action
                    return LauncherErrorCode.UnknownError;
            }
            if (result < LauncherErrorCode.Success)
                return result;
        }
        return result;
    }

    private static LauncherArguments ProcessArguments(string[] args)
    {
        var result = new LauncherArguments
        {
            // Default action is to run the server
            Actions = [LauncherArguments.ActionType.Run],
            Args = args,
        };

        foreach (var arg in args)
        {
            if (string.Equals(arg, "/Uninstall", StringComparison.InvariantCultureIgnoreCase))
            {
                // No other action possible when uninstalling.
                result.Actions.Clear();
                result.Actions.Add(LauncherArguments.ActionType.Uninstall);
            }
        }

        return result;
    }

    private static LauncherErrorCode TryRun(string[] args)
    {
        try
        {
            // Ensure to create parent of lock directory.
            Directory.CreateDirectory(EditorPath.DefaultTempPath);
            using (Mutex = FileLock.TryLock(Path.Combine(EditorPath.DefaultTempPath, "launcher.lock")))
            {
                if (Mutex != null)
                {
                    return (LauncherErrorCode)Program.BuildAvaloniaApp()
                        .StartWithClassicDesktopLifetime(args);
                }

                DisplayError("An instance of Stride Launcher is already running.", MessageBoxImage.Warning);
                return LauncherErrorCode.ServerAlreadyRunning;
            }
        }
        catch (Exception e)
        {
            DisplayError($"Cannot start the instance of the Stride Launcher due to the following exception:\n{e.Message}", MessageBoxImage.Error);
            return LauncherErrorCode.UnknownError;
        }
    }

    private static LauncherErrorCode Uninstall()
    {
        return LauncherErrorCode.Success;
    }

    private static void DisplayError(string message, MessageBoxImage image)
    {
        // Note: we need a new app because the main one may be already shutting down
        var appBuilder = AppBuilder.Configure<Application>()
            .UsePlatformDetect();
        if (Application.Current == null)
        {
            appBuilder = appBuilder
                .SetupWithLifetime(new ClassicDesktopStyleApplicationLifetime());
            AppMain(appBuilder.Instance!, message, image);
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

                AppMain(appBuilder.Instance!, message, image);
            });
        }

        static void AppMain(Application app, string message, MessageBoxImage image)
        {
            var cts = new CancellationTokenSource();
            _ = MessageBox.ShowAsync(null, "Stride Launcher", message, MessageBoxButton.OK, image).ContinueWith(_ => cts.Cancel());
            app.Run(cts.Token);
        }
    }

    #region Crash

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

    #endregion // Crash
}
