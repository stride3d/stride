// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Text;
using Microsoft.Win32;
using Stride.Core.Presentation.Avalonia.Windows;
using Stride.Core.Presentation.Services;

namespace Stride.Launcher;

internal static class PrerequisitesValidator
{
    private const string LauncherPrerequisites = @"Prerequisites\launcher-prerequisites.exe";

    private static bool CheckDotNet4Version(int requiredVersion)
    {
        // Check for .NET v4 version
        using var ndpKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full");
        if (ndpKey is null)
            return false;

        int releaseKey = Convert.ToInt32(ndpKey.GetValue("Release"));
        if (releaseKey < requiredVersion)
            return false;

        return true;
    }

    private static bool ValidateDotNet4Version(StringBuilder prerequisiteLog)
    {
        var result = true;

        // Check for .NET 4.7.2+
        // Note: it should now always be the case since renaming: Stride launcher is a separate forced setup to run, and it checks for 4.7.2.
        // Still keeping code for future framework updates
        if (!CheckDotNet4Version(461808))
        {
            prerequisiteLog.AppendLine("- .NET framework 4.7.2");
            result = false;
        }

        // Everything passed
        return result;
    }

    internal static async Task<bool> Validate(string[] args)
    {
        // Check prerequisites
        var prerequisiteLog = new StringBuilder();
        var prerequisitesFailedOnce = false;
        while (!ValidateDotNet4Version(prerequisiteLog))
        {
            prerequisitesFailedOnce = true;

            // Check if launcher prerequisite installer exists
            if (!File.Exists(LauncherPrerequisites))
            {
                await MessageBox.ShowAsync("Prerequisite error", $"Some prerequisites are missing, but no prerequisite installer was found!\n\n{prerequisiteLog}\n\nPlease install them manually or report the problem.", MessageBoxButton.OK);
                return false;
            }

            // One of the prerequisite failed, launch the prerequisite installer
            var prerequisitesApproved = await MessageBox.ShowAsync("Install missing prerequisites?", $"Some prerequisites are missing, do you want to install them?\n\n{prerequisiteLog}", MessageBoxButton.OKCancel);
            if (prerequisitesApproved == MessageBoxResult.Cancel)
                return false;

            try
            {
                var prerequisitesInstallerProcess = Process.Start(LauncherPrerequisites);
                if (prerequisitesInstallerProcess is null)
                {
                    await MessageBox.ShowAsync("Prerequisite error", $"There was an error running the prerequisite installer {LauncherPrerequisites}.", MessageBoxButton.OK);
                    return false;
                }

                prerequisitesInstallerProcess.WaitForExit();
            }
            catch
            {
                await MessageBox.ShowAsync("Prerequisite error", $"There was an error running the prerequisite installer {LauncherPrerequisites}.", MessageBoxButton.OK);
                return false;
            }
            prerequisiteLog.Length = 0;
        }

        if (prerequisitesFailedOnce)
        {
            // If prerequisites failed at least once, we want to restart ourselves to run with proper .NET framework
            var exeLocation = Program.GetExecutablePath();
            if (File.Exists(exeLocation))
            {
                // Forward arguments
                for (int i = 0; i < args.Length; ++i)
                {
                    // Quote arguments with spaces
                    if (args[i].IndexOf(' ') != -1)
                        args[i] = '\"' + args[i] + '\"';
                }
                var arguments = string.Join(" ", args);

                // Start process
                Process.Start(exeLocation, arguments);
            }
            return false;
        }

        return true;
    }
}
