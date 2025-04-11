// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Diagnostics;
using System.Management;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.CodeEditorSupport;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.Interop;

namespace Stride.Core.Assets.Editor.Services;

public static class VisualStudioService
{
    public static Process FindVisualStudioInstance(UFile solutionPath)
    {
        // NOTE: this code is very hackish and does not 100% ensure that the correct instance of VS will be activated.
        var processes = Process.GetProcessesByName("devenv");
        foreach (var process in processes)
        {
            // Get instances that have a solution with the same name currently open (The solution name is displayed in the title bar).
            if (process.MainWindowTitle.StartsWith(solutionPath.GetFileNameWithoutExtension(), StringComparison.OrdinalIgnoreCase))
            {
                // If there is a matching instance, get its command line.
                var query = $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}";
                using var managementObjectSearcher = new ManagementObjectSearcher(query);
                var managementObject = managementObjectSearcher.Get().Cast<ManagementObject>().First();
                var commandLine = managementObject["CommandLine"].ToString();
                if (commandLine.Replace('/', '\\').Contains(solutionPath.ToString().Replace('/', '\\'), StringComparison.OrdinalIgnoreCase))
                {
                    return process;
                }
            }
        }

        return null;
    }

    public static async Task<Process> GetInstance(SessionViewModel session, bool makeActive)
    {
        if (!await CodeEditorOpenerService.CheckCanOpenSolution(session, IDEInfo.DefaultIDE))
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
}
