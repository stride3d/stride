// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xenko.Core.Extensions;
using Xenko.Core.VisualStudio;
using Xenko.Core.Packages;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Presentation.ViewModel;

namespace Xenko.LauncherApp.Services
{
    internal class UninstallHelper : IDisposable
    {
        private readonly IViewModelServiceProvider serviceProvider;
        private readonly NugetStore store;

        internal UninstallHelper(IViewModelServiceProvider serviceProvider,  NugetStore store)
        {
            this.serviceProvider = serviceProvider;
            this.store = store;
            store.NugetPackageUninstalling += PackageUninstalling;
        }

        public void Dispose()
        {
            store.NugetPackageUninstalling -= PackageUninstalling;
        }

        /// <summary>
        /// Closes all processes that were started from the given directory or one of its subdirectory. If the process has a window,
        /// this method will spawn a dialog box to ask the user to terminate the process himself.
        /// </summary>
        /// <param name="showMessage">An function that will display a message box with the given text and OK/Cancel buttons, and returns <c>True</c> if the user pressed OK or <c>False</c> if he pressed Cancel.</param>
        /// <param name="uninstallingProgramName">The name of the program being uninstalled, used for displaying a dialog message.</param>
        /// <param name="path">The path in which processes to terminate are located.</param>
        /// <returns><c>True</c> if all the processes were terminated, <c>False</c> if the user cancelled the operation.</returns>
        /// <remarks>There is no guarantee that all processes will be killed at the end. An error might occurs when trying to close a process.</remarks>
        public static bool CloseProcessesInPath(Func<string, bool> showMessage, string uninstallingProgramName, string path)
        {
            // Check processes
            var processesWithWindow = new List<Tuple<string, Process>>();
            List<Process> processes;
            do
            {
                processes = CollectPackageProcesses(path);

                // Make sure all process with main window are closed
                processesWithWindow.Clear();
                foreach (var process in processes)
                {
                    try
                    {
                        if (process.MainWindowHandle != IntPtr.Zero)
                        {
                            processesWithWindow.Add(Tuple.Create(process.MainModule.ModuleName, process));
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
                    var confirmResult = showMessage(message);

                    if (!confirmResult)
                    {
                        // User pressed Cancel, no need to uninstall
                        return false;
                    }
                }
            } while (processesWithWindow.Count > 0);

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
        
        private static List<Process> CollectPackageProcesses(string installPath)
        {
            var result = new List<Process>();
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    var filename = process.MainModule.FileName;

                    // Check if filename is inside install path
                    if (!IsPathInside(installPath, filename))
                        continue;

                    // Discard ourselves
                    if (process.Id == Process.GetCurrentProcess().Id)
                        continue;

                    result.Add(process);
                }
                catch (Exception exception)
                {
                    // Many errors can happen when accessing process main module (permission, process killed, etc...)
                    exception.Ignore();
                }
            }

            return result;
        }

        private bool DisplayMessage(string message)
        {
            var result = serviceProvider.Get<IDialogService>().BlockingMessageBox(message, MessageBoxButton.OKCancel);
            return result != MessageBoxResult.Cancel;
        }

        private void PackageUninstalling(object sender, PackageOperationEventArgs e)
        {
            CloseProcessesInPath(DisplayMessage, e.Id, e.InstallPath);
        }
    }
}
