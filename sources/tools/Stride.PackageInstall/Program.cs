// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using Stride.Core.VisualStudio;

namespace Stride.PackageInstall
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    throw new Exception("Expecting a parameter such as /install, /repair or /uninstall");
                }

                switch (args[0])
                {
                    case "/install":
                    case "/repair":
                    {
                        // Run prerequisites installer (if it exists)
                        var prerequisitesInstallerPath = @"install-prerequisites.exe";
                        if (File.Exists(prerequisitesInstallerPath))
                        {
                            RunProgramAndAskUntilSuccess("prerequisites", prerequisitesInstallerPath, string.Empty, DialogBoxTryAgain);
                        }

                        break;
                    }
                }

                return 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error: {e}");
                return 1;
            }
        }

        private static int RunProgramAndAskUntilSuccess(string programName, string fileName, string arguments, Func<string, Process, bool> processError)
        {
        TryAgain:
            try
            {
                var prerequisitesInstallerProcess = Process.Start(fileName, arguments);
                if (prerequisitesInstallerProcess == null)
                {
                    MessageBox.Show($"The installation of {programName} failed (file not found).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return -1;
                }
                prerequisitesInstallerProcess.WaitForExit();
                if (prerequisitesInstallerProcess.ExitCode != 0)
                {
                    if (!processError(programName, prerequisitesInstallerProcess))
                        return prerequisitesInstallerProcess.ExitCode;
                    goto TryAgain;
                }
                return 0;
            }
            catch (Win32Exception e) when (e.NativeErrorCode == 1223)
            {
                // We'll enter this if UAC has been declined, but also if it timed out (which is a frequent case)
                // if you don't stay in front of your computer during the installation.
                var result = MessageBox.Show($"The installation of {programName} failed to run (UAC denied).\r\nDo you want to try it again?", "Error", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                    return -1;
                goto TryAgain;
            }
            catch (Exception e)
            {
                MessageBox.Show($"The installation of {programName} failed unexpectedly:\r\n\r\n{e}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return -1;
            }
        }

        private static bool DialogBoxTryAgain(string programName, Process process)
        {
            var result = MessageBox.Show($"The installation of {programName} returned with code {process.ExitCode}.\r\nDo you want to try it again?", "Error", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            return result == DialogResult.Yes;
        }
    }
}
