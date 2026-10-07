// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Stride.Core.Assets;
using Stride.Core.Extensions;
using Stride.Core.Packages;
using Stride.Core.Presentation.Avalonia.Windows;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Launcher.Services;

internal partial class UninstallHelper : IDisposable
{
    private readonly NugetStore store;

    internal UninstallHelper(IViewModelServiceProvider serviceProvider, NugetStore store)
    {
        this.store = store;
        store.UninstallGuard = CanUninstallAsync;
    }

    public void Dispose()
    {
        store.UninstallGuard = null;
    }

    /// <summary>
    /// Closes all processes that were started from the given directories or one of their subdirectories. If the process has a window,
    /// this method will spawn a dialog box to ask the user to terminate the process himself.
    /// </summary>
    /// <param name="showMessageAsync">An function that will display a message box with the given text and OK/Cancel buttons, and returns <c>True</c> if the user pressed OK or <c>False</c> if he pressed Cancel.</param>
    /// <param name="uninstallingProgramName">The name of the program being uninstalled, used for displaying a dialog message.</param>
    /// <param name="paths">The paths in which processes to terminate are located: one scan of the processes covers them all.</param>
    /// <returns><c>True</c> if all the processes were terminated, <c>False</c> if the user cancelled the operation.</returns>
    /// <remarks>There is no guarantee that all processes will be killed at the end. An error might occurs when trying to close a process.</remarks>
    public static async Task<bool> CloseProcessesInPathsAsync(Func<string, Task<bool>> showMessageAsync, string uninstallingProgramName, IReadOnlyCollection<string> paths)
    {
        // Check processes
        var processesWithWindow = new List<Tuple<string, Process>>();
        List<Process> processes;
        var editorRunning = false;
        do
        {
            processes = CollectPackageProcesses(paths);

            // Make sure all process with main window are closed
            processesWithWindow.Clear();
            foreach (var process in processes)
            {
                try
                {
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        processesWithWindow.Add(Tuple.Create(process.MainModule!.ModuleName, process));
                    }
                }
                catch (Exception exception)
                {
                    exception.Ignore();
                }
            }

            // There is still process with main window, inform user so that he can properly close them
            if (processesWithWindow.Count > 0)
            {
                var nl = Environment.NewLine;
                // Display error to user and block until he presses try again
                var runningProcesses = string.Join(nl, processesWithWindow.GroupBy(x => x.Item1).Select(x => $" - {x.Key} ({x.Count()} instance(s))"));
                var message = $"Can't uninstall {uninstallingProgramName} because processes are still running:{nl}{runningProcesses}{nl}{nl}Please close them and press OK to try again, or Cancel to stop.";
                var confirmResult = await showMessageAsync(message);

                if (!confirmResult)
                {
                    // User pressed Cancel, no need to uninstall
                    return false;
                }
            }
            else
            {
                // A Game Studio re-executed on another .NET major is a dotnet process the scan misses; it holds a marker instead.
                editorRunning = paths.SelectMany(PackageLayout.FrameworkDirectories).Any(HostInstanceMutex.IsHeld);
                if (editorRunning && !await showMessageAsync($"Can't uninstall {uninstallingProgramName} because Game Studio is still running from it.{Environment.NewLine}{Environment.NewLine}Please close it and press OK to try again, or Cancel to stop."))
                    return false;
            }
        } while (processesWithWindow.Count > 0 || editorRunning);

        // Kill all other processes (there should be no processes with main window left, so probably services/console apps)
        foreach (var process in processes)
        {
            try
            {
                try
                {
                    process.StandardInput.Close();
                }
                catch
                {
                    process.Kill();
                }
            }
            catch (Exception exception)
            {
                // Ignore weird errors (process gone, etc...)
                exception.Ignore();
            }
        }

        return true;
    }

    private static bool IsPathInside(string folder, string path)
    {
        // Can probably be improved (not sure how stable and unique path could be?)
        return (path.IndexOf(folder, StringComparison.OrdinalIgnoreCase) != -1);
    }

    private static List<Process> CollectPackageProcesses(IReadOnlyCollection<string> installPaths)
    {
        var result = new List<Process>();
        var buffer = new char[32767];
        foreach (var process in Process.GetProcesses())
        {
            // Discard ourselves, and processes whose file can't be read (protected, exited...)
            if (process.Id != Environment.ProcessId && GetProcessPath(process, buffer) is { } filename && installPaths.Any(path => IsPathInside(path, filename)))
                result.Add(process);
            else
                process.Dispose();
        }

        return result;
    }

    // Runs for every process: on Windows, a query that doesn't throw, rather than Process.MainModule, which lists the
    // process modules and throws for many processes. The buffer is as long as a Windows path can be (long paths enabled).
    private static string? GetProcessPath(Process process, char[] buffer)
    {
        if (OperatingSystem.IsWindows())
        {
            var handle = OpenProcess(ProcessQueryLimitedInformation, false, process.Id);
            if (handle == IntPtr.Zero)
                return null;
            try
            {
                var size = buffer.Length;
                return QueryFullProcessImageName(handle, 0, buffer, ref size) ? new string(buffer, 0, size) : null;
            }
            finally
            {
                CloseHandle(handle);
            }
        }

        try
        {
            return process.MainModule?.FileName;
        }
        catch (Exception exception)
        {
            // Many errors can happen when accessing process main module (permission, process killed, etc...)
            exception.Ignore();
            return null;
        }
    }

    private const uint ProcessQueryLimitedInformation = 0x1000;

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr OpenProcess(uint desiredAccess, [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, int processId);

    [LibraryImport("kernel32.dll", SetLastError = true, EntryPoint = "QueryFullProcessImageNameW", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool QueryFullProcessImageName(IntPtr process, uint flags, [Out] char[] exeName, ref int size);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseHandle(IntPtr handle);

    // On the UI thread: the launcher uninstalls from worker threads.
    private static Task<bool> DisplayMessageAsync(string message)
        => Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var result = await MessageBox.ShowAsync(Launcher.ApplicationName, message, IDialogService.GetButtons(MessageBoxButton.OKCancel));
            return result != (int)MessageBoxResult.Cancel;
        });

    // Awaited by the store before it deletes anything, so Cancel keeps the packages.
    private static Task<bool> CanUninstallAsync(IReadOnlyList<PackageOperationEventArgs> packages)
        => CloseProcessesInPathsAsync(DisplayMessageAsync, packages.Count == 1 ? packages[0].Id : $"{packages.Count} packages", packages.Select(x => x.InstallPath).ToList());
}
