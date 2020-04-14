// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.VisualStudio;
using Stride.Core.Presentation.Interop;
using Stride.Core.Presentation.Services;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.Services
{
    public static class VisualStudioService
    {
        public static async Task<bool> StartOrToggleVisualStudio(SessionViewModel session, IDEInfo ideInfo)
        {
            if (!await CheckCanOpenSolution(session))
                return false;

            var process = await GetVisualStudio(session, true) ?? await StartVisualStudio(session, ideInfo);
            return process != null;
        }

        public static async Task<Process> GetVisualStudio(SessionViewModel session, bool makeActive)
        {
            if (!await CheckCanOpenSolution(session))
                return null;

            try
            {
                // Try to find an existing instance of Visual Studio with this solution open.
                var process = FindVisualStudioInstance(session.SolutionPath);

                if (process != null && makeActive)
                {
                    int style = NativeHelper.GetWindowLong(process.MainWindowHandle, NativeHelper.GWL_STYLE);
                    // Restore the window if it is minimized
                    if ((style & NativeHelper.WS_MINIMIZE) == NativeHelper.WS_MINIMIZE)
                        NativeHelper.ShowWindow(process.MainWindowHandle, NativeHelper.SW_RESTORE);
                    NativeHelper.SetForegroundWindow(process.MainWindowHandle);
                }

                return process;
            }
            catch (Exception e)
            {
                // This operation can fail silently
                e.Ignore();
                return null;
            }
        }

        public static async Task<Process> StartVisualStudio(SessionViewModel session, IDEInfo ideInfo)
        {
            if (!await CheckCanOpenSolution(session))
                return null;

            var startInfo = new ProcessStartInfo();
            if (ideInfo == null)
            {
                var defaultIDEName = EditorSettings.DefaultIDE.GetValue();

                if (!EditorSettings.DefaultIDE.GetAcceptableValues().Contains(defaultIDEName))
                    defaultIDEName = EditorSettings.DefaultIDE.DefaultValue;

                ideInfo = VisualStudioVersions.AvailableVisualStudioInstances.FirstOrDefault(x => x.DisplayName == defaultIDEName) ?? VisualStudioVersions.DefaultIDE;
            }

            // It will be null if either "Default", or if not available anymore (uninstalled?)
            if (ideInfo.DevenvPath != null && File.Exists(ideInfo.DevenvPath))
            {
                startInfo.FileName = ideInfo.DevenvPath;
                startInfo.Arguments = $"\"{session.SolutionPath}\"";
            }
            else
            {
                startInfo.FileName = session.SolutionPath;
            }
            try
            {
                return Process.Start(startInfo);
            }
            catch
            {
                await session.Dialogs.MessageBox(Tr._p("Message", "An error occurred while starting Visual Studio."), MessageBoxButton.OK, MessageBoxImage.Information);
                return null;
            }
        }

        private static async Task<bool> CheckCanOpenSolution(SessionViewModel session)
        {
            if (string.IsNullOrEmpty(session.SolutionPath))
            {
                await session.Dialogs.MessageBox(Tr._p("Message", "The session currently open is not a Visual Studio session."), MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            return true;
        }

        private static Process FindVisualStudioInstance(UFile solutionPath)
        {
            // NOTE: this code is very hackish and does not 100% ensure that the correct instance of VS will be activated.
            var processes = Process.GetProcessesByName("devenv");
            foreach (var process in processes)
            {
                // Get instances that have a solution with the same name currently open (The solution name is displayed in the title bar).
                if (process.MainWindowTitle.ToLowerInvariant().StartsWith(solutionPath.GetFileNameWithoutExtension().ToLowerInvariant()))
                {
                    // If there is a matching instance, get its command line.
                    var query = $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}";
                    using (var managementObjectSearcher = new ManagementObjectSearcher(query))
                    {
                        var managementObject = managementObjectSearcher.Get().Cast<ManagementObject>().First();
                        var commandLine = managementObject["CommandLine"].ToString();
                        if (commandLine.ToLowerInvariant().Replace("/", "\\").Contains(solutionPath.ToString().ToLowerInvariant().Replace("/", "\\")))
                        {
                            return process;
                        }
                    }
                }
            }

            return null;
        }
    }
}
