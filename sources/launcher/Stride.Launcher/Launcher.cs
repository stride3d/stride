// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Stride.Core.Assets.Editor;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Packages;
using Stride.Core.Presentation.Avalonia.Windows;
using Stride.Core.Presentation.Services;
using Stride.Core.Windows;
using Stride.Launcher.Crash;
using Stride.Launcher.Services;
using Stride.Metrics;

namespace Stride.Launcher;

internal static partial class Launcher
{
    private static int terminating;
    internal static MetricsClient? Metrics;
    internal static FileLock? Mutex;

    public const string ApplicationName = "Stride Launcher";

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

    internal static NugetStore InitializeNugetStore()
    {
        var thisExeDirectory = new UFile(Assembly.GetEntryAssembly()!.Location).GetFullDirectory().ToWindowsPath();
        var store = new NugetStore(thisExeDirectory);
        return store;
    }

    private static LauncherErrorCode ProcessAction(LauncherArguments args)
    {
        var result = LauncherErrorCode.UnknownError;

        try
        {
            // Ensure to create parent of lock directory.
            Directory.CreateDirectory(EditorPath.DefaultTempPath);
            using (Mutex = FileLock.TryLock(Path.Combine(EditorPath.DefaultTempPath, "launcher.lock")))
            {
                if (Mutex is not null)
                {
                    // Only needed for Stride up to 2.x (and possibly 3.0): setup the StrideDir to make sure that it is passed to the underlying process (msbuild...etc.)
                    Environment.SetEnvironmentVariable("SiliconStudioStrideDir", AppDomain.CurrentDomain.BaseDirectory);
                    Environment.SetEnvironmentVariable("StrideDir", AppDomain.CurrentDomain.BaseDirectory);

                    // We need to do that before starting recording metrics
                    // TODO: we do not display Privacy Policy anymore from launcher, because it's either accepted from installer or shown again when a new version of GS with new Privacy Policy starts. Might want to reconsider that after the 2.0 free period
                    PrivacyPolicyHelper.RestartApplication = SelfUpdater.RestartApplication;
                    PrivacyPolicyHelper.EnsurePrivacyPolicyStride40();

                    // Install Metrics for the launcher
                    using (Metrics = new MetricsClient(CommonApps.StrideLauncherAppId))
                    {
                        Program.RunNewApp<App>(AppMain);
                    }
                }
                else
                {
                    DisplayError("An instance of Stride Launcher is already running.", MessageBoxImage.Warning);
                    result = LauncherErrorCode.ServerAlreadyRunning;
                }
            }
        }
        catch (Exception e)
        {
            DisplayError($"Cannot start the instance of the Stride Launcher due to the following exception:\n{e.Message}", MessageBoxImage.Error);
            result = LauncherErrorCode.UnknownError;
        }

        return result;

        CancellationToken AppMain(App app)
        {
            _ = AppMainAsync(app.cts);
            return app.cts.Token;
        }

        async Task AppMainAsync(CancellationTokenSource cts)
        {
            foreach (var action in args.Actions)
            {
                result = action switch
                {
                    LauncherArguments.ActionType.Run => TryRun(cts),
                    LauncherArguments.ActionType.Uninstall => await UninstallAsync(cts),
                    _ => LauncherErrorCode.UnknownError,// Unknown action
                };
                if (result < LauncherErrorCode.Success)
                    break;
            }
        }

        static void DisplayError(string message, MessageBoxImage image)
        {
            // Note: because we are not running from the main loop, we have to start a new app
            Program.RunNewApp<Application>(AppMain);

            CancellationToken AppMain(Application app)
            {
                var cts = new CancellationTokenSource();
                _ = MessageBox.ShowAsync(ApplicationName, message, MessageBoxButton.OK, image).ContinueWith(_ => cts.Cancel());
                return cts.Token;
            }
        }
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

    private static LauncherErrorCode TryRun(CancellationTokenSource cts)
    {
        var mainWindow = ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow!;
        mainWindow.Closed += (_, __) => cts.Cancel();
        mainWindow.Show();
        return LauncherErrorCode.Success;
    }

    private static async Task<LauncherErrorCode> UninstallAsync(CancellationTokenSource cts)
    {
        try
        {
            // Kill all running processes
            var path = new UFile(Assembly.GetEntryAssembly()!.Location).GetFullDirectory().ToWindowsPath();
            if (!await UninstallHelper.CloseProcessesInPathAsync(DisplayMessageAsync, "Stride", path))
                return LauncherErrorCode.UninstallCancelled; // User cancelled

            // Uninstall packages (they might have uninstall actions)
            var store = new NugetStore(path);
            foreach (var package in store.MainPackageIds.SelectMany(store.GetLocalPackages).FilterStrideMainPackages().ToList())
            {
                await store.UninstallPackage(package, null);
            }

            foreach (var remainingFiles in Directory.GetFiles(path, "*.lock").Concat(Directory.GetFiles(path, "*.old")))
            {
                try
                {
                    File.Delete(remainingFiles);
                }
                catch (Exception e)
                {
                    e.Ignore();
                }
            }

            //PrivacyPolicyHelper.RevokeAllPrivacyPolicy(); // FIXME: xplat-launcher

            return LauncherErrorCode.Success;
        }
        catch (Exception)
        {
            return LauncherErrorCode.ErrorWhileUninstalling;
        }
        finally
        {
            cts.Cancel();
        }

        static async Task<bool> DisplayMessageAsync(string message)
        {
            var result = await MessageBox.ShowAsync(ApplicationName, message, MessageBoxButton.YesNo, MessageBoxImage.Information);
            return result == MessageBoxResult.Yes;
        }
    }

    #region Crash

    private static void CrashReport(CrashReportArgs args)
    {
        Program.RunNewApp<Application>(AppMain);

        CancellationToken AppMain(Application app)
        {
            var cts = new CancellationTokenSource();
            var window = new CrashReportWindow { Topmost = true };
            window.DataContext = new CrashReportViewModel(args, window.Clipboard!.SetTextAsync, cts);
            window.Closed += (_, __) => cts.Cancel();
            if (!window.IsVisible)
            {
                window.Show();
            }
            return cts.Token;
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
        if (exception is null) return;

        // prevent multiple crash reports
        if (Interlocked.CompareExchange(ref terminating, 1, 0) == 1) return;

        var englishCulture = new CultureInfo("en-US");
        Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = englishCulture;
        var reportArgs = new CrashReportArgs(exception, Thread.CurrentThread.Name);
        CrashReport(reportArgs);
    }

    #endregion // Crash

}
