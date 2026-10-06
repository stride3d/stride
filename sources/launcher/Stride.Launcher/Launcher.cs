// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Stride.Core;
using Stride.Core.Assets.Editor;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Packages;
using Stride.Core.Presentation.Avalonia.Windows;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Windows;
using Stride.Core.Windows;
using Stride.Crash;
using Stride.Crash.ViewModels;
using Stride.Launcher.Services;

namespace Stride.Launcher;

internal static class Launcher
{
    private static int terminating;
    internal static FileLock? Mutex;

    public const string ApplicationName = "Stride Launcher";

    [STAThread]
    public static LauncherErrorCode Main(string[] args)
    {
        // Managed crashes only, reported in process. No native crash capture (NativeCrashReporting.Install): the
        // launcher ships neither libstridecrash nor the reporter.
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        try
        {
            var arguments = ProcessArguments(args);
            return ProcessAction(arguments);
        }
        catch (Exception ex)
        {
            HandleException(ex, CrashLocation.Main);
            return LauncherErrorCode.ErrorWhileRunningServer;
        }
    }

    internal static NugetStore InitializeNugetStore()
    {
        var store = new NugetStore(Program.GetExecutableDirectory());
        return store;
    }

    private static LauncherErrorCode ProcessAction(LauncherArguments args)
    {
        // Uninstalling (run by the setup) doesn't take the single-instance lock, so that it can offer to close a running
        // launcher, and doesn't create the main window, whose view model would look for launcher updates
        if (args.Actions.Contains(LauncherArguments.ActionType.Uninstall))
            return IsQuietUninstall(args.Quiet, Environment.UserInteractive) ? UninstallQuiet() : Uninstall();

        var result = LauncherErrorCode.UnknownError;

        try
        {
            // Ensure to create parent of lock directory.
            Directory.CreateDirectory(EditorPath.DefaultTempPath);
            using (Mutex = FileLock.TryLock(Path.Combine(EditorPath.DefaultTempPath, "launcher.lock")))
            {
                if (Mutex is not null)
                {
                    Program.RunNewApp<App>(AppMain);
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
            result = TryRun(app.cts);
            return app.cts.Token;
        }

        static void DisplayError(string message, MessageBoxImage image)
        {
            // Note: because we are not running from the main loop, we have to start a new app
            Program.RunNewApp<MinimalApp>(AppMain);

            CancellationToken AppMain(Application app)
            {
                var cts = new CancellationTokenSource();
                _ = MessageBox.ShowAsync(ApplicationName, message, IDialogService.GetButtons(MessageBoxButton.OK), image).ContinueWith(_ => cts.Cancel());
                return cts.Token;
            }
        }
    }

    internal static LauncherArguments ProcessArguments(string[] args)
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
            else if (string.Equals(arg, "/Quiet", StringComparison.InvariantCultureIgnoreCase))
            {
                result.Quiet = true;
            }
        }

        return result;
    }

    private static LauncherErrorCode TryRun(CancellationTokenSource cts)
    {
        var mainWindow = ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow!;
        mainWindow.Closed += (_, _) => cts.Cancel();
        mainWindow.Show();
        return LauncherErrorCode.Success;
    }

    /// <summary>
    /// Whether the uninstall shows no UI. Also without <c>/quiet</c> when the process has no desktop (SYSTEM or
    /// session 0, e.g. Intune): a dialog would be invisible there and block the setup.
    /// </summary>
    internal static bool IsQuietUninstall(bool quiet, bool userInteractive) => quiet || !userInteractive;

    // Keeps the installed Stride versions: the processes running from them don't matter, and nothing is shown, not
    // even a window. Only the launcher's own leftovers are deleted.
    private static LauncherErrorCode UninstallQuiet()
    {
        DeleteLeftoverFiles(Program.GetExecutableDirectory());
        return LauncherErrorCode.Success;
    }

    private static LauncherErrorCode Uninstall()
    {
        var result = LauncherErrorCode.UnknownError;
        Program.RunNewApp<MinimalApp>(AppMain);
        return result;

        CancellationToken AppMain(Application app)
        {
            var cts = new CancellationTokenSource();
            _ = RunAsync(cts);
            return cts.Token;
        }

        // The result is set before the app stops: its loop wouldn't run what comes after
        async Task RunAsync(CancellationTokenSource cts)
        {
            try
            {
                result = await UninstallAsync();
            }
            finally
            {
                await cts.CancelAsync();
            }
        }
    }

    private static async Task<LauncherErrorCode> UninstallAsync()
    {
        var path = Program.GetExecutableDirectory();
        NugetStore store;
        (List<PackageVersion> Versions, List<NugetLocalPackage> Packages, long Size)? removable;
        try
        {
            // Kill all running processes
            if (!await UninstallHelper.CloseProcessesInPathsAsync(DisplayMessageAsync, "Stride", [path]))
                return LauncherErrorCode.UninstallCancelled; // User cancelled

            DeleteLeftoverFiles(path);

            store = new NugetStore(path);
            removable = await Task.Run(() => FindRemovableVersions(store));
        }
        catch (Exception)
        {
            return LauncherErrorCode.ErrorWhileUninstalling;
        }

        // The installed versions are kept unless the user asks: someone reinstalling the launcher wants them back
        if (removable is not { } removal || !await AskRemoveVersionsAsync(removal.Versions, removal.Size))
            return LauncherErrorCode.Success;

        try
        {
            // Game Studio runs from the packages, not from the launcher folder. Packages run their uninstall actions.
            store.UninstallGuard = packages => UninstallHelper.CloseProcessesInPathsAsync(DisplayMessageAsync, "the Stride versions", [.. packages.Select(x => x.InstallPath)]);
            await store.UninstallPackages(removal.Packages, null);
            return LauncherErrorCode.Success;
        }
        catch (OperationCanceledException)
        {
            return LauncherErrorCode.UninstallCancelled;
        }
        catch (Exception e)
        {
            await MessageBox.ShowAsync(ApplicationName, $"Some Stride versions could not be removed:{Environment.NewLine}{e.Message}", IDialogService.GetButtons(MessageBoxButton.OK), MessageBoxImage.Warning);
            return LauncherErrorCode.ErrorWhileUninstalling;
        }

        static async Task<bool> DisplayMessageAsync(string message)
        {
            var result = await MessageBox.ShowAsync(ApplicationName, message, IDialogService.GetButtons(MessageBoxButton.OKCancel), MessageBoxImage.Information);
            return result == (int)MessageBoxResult.OK;
        }
    }

    // The installed Stride versions that the uninstall offers to remove (local builds are never removed), the packages
    // to remove with them, and their size on disk. Null when there are none.
    private static (List<PackageVersion> Versions, List<NugetLocalPackage> Packages, long Size)? FindRemovableVersions(NugetStore store)
    {
        var mainPackages = store.GetPackagesInstalled(store.MainPackageIds).FilterStrideMainPackages().ToList();
        var releases = mainPackages.Where(x => !store.IsDevRedirectPackage(x)).ToList();
        if (releases.Count == 0)
            return null;

        var packages = StridePackageReferences.FindRemovable(store, releases, [.. mainPackages.Where(store.IsDevRedirectPackage)]);
        return ([.. releases.Select(x => x.Version).Distinct().OrderDescending()], packages, packages.Sum(x => GetDirectorySize(x.Path)));

        static long GetDirectorySize(string path)
        {
            try
            {
                return new DirectoryInfo(path).EnumerateFiles("*", SearchOption.AllDirectories).Sum(x => x.Length);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                return 0;
            }
        }
    }

    private static async Task<bool> AskRemoveVersionsAsync(IReadOnlyList<PackageVersion> versions, long size)
    {
        const int Remove = 1;
        const int Keep = 2;
        var nl = Environment.NewLine;
        var message = $"Also remove the installed Stride versions?{nl}{nl}{string.Join(", ", versions)} ({FormatSize(size)}){nl}{nl}Keep them if you plan to install Stride again.";
        var buttons = new[]
        {
            new DialogButtonInfo
            {
                Content = "Remove",
                Result = Remove,
            },
            new DialogButtonInfo
            {
                Content = "Keep",
                IsDefault = true,
                IsCancel = true,
                Key = "Escape",
                Result = Keep,
            },
        };
        return await MessageBox.ShowAsync(ApplicationName, message, buttons, MessageBoxImage.Question) == Remove;
    }

    internal static string FormatSize(long bytes)
        => bytes >= 1L << 30 ? $"{bytes / (double)(1L << 30):0.#} GB" : $"{Math.Max(1, bytes >> 20)} MB";

    // The lock files and the files a self-update renamed to .old. Locked ones are skipped.
    private static void DeleteLeftoverFiles(string path)
    {
        try
        {
            foreach (var file in Directory.GetFiles(path, "*.lock").Concat(Directory.GetFiles(path, "*.old")))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception e)
                {
                    e.Ignore();
                }
            }
        }
        catch (Exception e)
        {
            e.Ignore();
        }
    }

    #region Crash

    private static void CrashReport(CrashReportArgs args)
    {
        Program.RunNewApp<MinimalApp>(AppMain);

        CancellationToken AppMain(Application app)
        {
            var cts = new CancellationTokenSource();
            var window = new CrashReportWindow { Topmost = true };
            window.DataContext = new CrashReportViewModel(ApplicationName, args, window.Clipboard!.SetTextAsync, cts);
            window.Closed += (_, _) => cts.Cancel();
            if (!window.IsVisible)
            {
                window.Show();
            }
            ((IClassicDesktopStyleApplicationLifetime?)app?.ApplicationLifetime)?.MainWindow = window;
            return cts.Token;
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
        if (exception is null) return;

        // prevent multiple crash reports
        if (Interlocked.CompareExchange(ref terminating, 1, 0) == 1) return;

        var englishCulture = new CultureInfo("en-US");
        Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = englishCulture;
        // On the faulting thread: snapshot the other threads (the crashing thread's stack comes from the exception).
        var threads = Stride.CrashReport.ThreadSnapshot.CaptureAtCurrentThread(out var crashedThreadId, out var crashedThreadName);
        var reportArgs = new CrashReportArgs
        {
            Exception = exception,
            Location = location,
            ThreadName = crashedThreadName,
            ThreadId = crashedThreadId,
            Threads = threads
        };
        CrashReport(reportArgs);
    }

    #endregion // Crash
}
