// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Stride.LauncherApp
{
    static class Program
    {
        private const string LauncherPrerequisites = @"Prerequisites\launcher-prerequisites.exe";

        [STAThread]
        private static void Main(string[] args)
        {
            // Check prerequisites
            var prerequisiteLog = new StringBuilder();
            var prerequisitesFailedOnce = false;
            while (!CheckPrerequisites(prerequisiteLog))
            {
                prerequisitesFailedOnce = true;

                // Check if launcher prerequisite installer exists
                if (!File.Exists(LauncherPrerequisites))
                {
                    MessageBox.Show($"Some prerequisites are missing, but no prerequisite installer was found!\n\n{prerequisiteLog}\n\nPlease install them manually or report the problem.", "Prerequisite error", MessageBoxButtons.OK);
                    return;
                }

                // One of the prerequisite failed, launch the prerequisite installer
                var prerequisitesApproved = MessageBox.Show($"Some prerequisites are missing, do you want to install them?\n\n{prerequisiteLog}", "Install missing prerequisites?", MessageBoxButtons.OKCancel);
                if (prerequisitesApproved == DialogResult.Cancel)
                    return;

                try
                {
                    var prerequisitesInstallerProcess = Process.Start(LauncherPrerequisites);
                    if (prerequisitesInstallerProcess == null)
                    {
                        MessageBox.Show($"There was an error running the prerequisite installer {LauncherPrerequisites}.", "Prerequisite error", MessageBoxButtons.OK);
                        return;
                    }

                    prerequisitesInstallerProcess.WaitForExit();
                }
                catch
                {
                    MessageBox.Show($"There was an error running the prerequisite installer {LauncherPrerequisites}.", "Prerequisite error", MessageBoxButtons.OK);
                    return;
                }
                prerequisiteLog.Length = 0;
            }

            if (prerequisitesFailedOnce)
            {
                // If prerequisites failed at least once, we want to restart ourselves to run with proper .NET framework
                var exeLocation = Launcher.GetExecutablePath();
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
                return;
            }

            Launcher.Main(args);
        }

        private static bool CheckPrerequisites(StringBuilder prerequisiteLog)
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

        private static bool CheckDotNet4Version(int requiredVersion)
        {
            // Check for .NET v4 version
            using (var ndpKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"))
            {
                if (ndpKey == null)
                    return false;

                int releaseKey = Convert.ToInt32(ndpKey.GetValue("Release"));
                if (releaseKey < requiredVersion)
                    return false;
            }

            return true;
        }
    }
}
