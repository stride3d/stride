// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using Xenko.Core.VisualStudio;

namespace Xenko.PackageInstall
{
    class Program
    {
        private static readonly string[] NecessaryVS2017Workloads = new[] { "Microsoft.VisualStudio.Workload.ManagedDesktop", "Microsoft.NetCore.ComponentGroup.DevelopmentTools.2.1" };
        private static readonly string[] NecessaryBuildTools2017Workloads = new[] { "Microsoft.VisualStudio.Workload.MSBuildTools", "Microsoft.VisualStudio.Workload.NetCoreBuildTools", "Microsoft.Net.Component.4.6.1.TargetingPack" };
        private const bool AllowVisualStudioOnly = true; // Somehow this doesn't work well yet, so disabled for now

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
                            RunProgramAndAskUntilSuccess("prerequisites", prerequisitesInstallerPath, string.Empty);
                        }

                        // Make sure we have the proper VS2017/BuildTools prerequisites
                        CheckVisualStudioAndBuildTools();

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

        private static int RunProgramAndAskUntilSuccess(string programName, string fileName, string arguments)
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
                    // We'll enter this if UAC has been declined, but also if it timed out (which is a frequent case)
                    // if you don't stay in front of your computer during the installation.
                    var result = MessageBox.Show($"The installation of {programName} returned with code {prerequisitesInstallerProcess.ExitCode}.\r\nDo you want to try it again?", "Error", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result != DialogResult.Yes)
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

        private static void CheckVisualStudioAndBuildTools()
        {
            // Check if there is any VS2017 installed with necessary workloads
            var matchingVisualStudioInstallation = VisualStudioVersions.AvailableVisualStudioInstances.FirstOrDefault(x => NecessaryVS2017Workloads.All(workload => x.PackageVersions.ContainsKey(workload)));
            if (AllowVisualStudioOnly && matchingVisualStudioInstallation != null)
            {
                if (!matchingVisualStudioInstallation.Complete)
                    MessageBox.Show("We detected Visual Studio 2017 was already installed but is not in a complete state.\r\nYou probably have to reboot, otherwise Xenko projects won't properly compile.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                // Check if there is actually a VS2017+ installed
                var existingVisualStudio2017Install = VisualStudioVersions.AvailableVisualStudioInstances.FirstOrDefault(x => x.PackageVersions.ContainsKey("Microsoft.VisualStudio.Component.CoreEditor"));
                var vsInstallerPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Microsoft Visual Studio\Installer\vs_installer.exe");
                if (AllowVisualStudioOnly && existingVisualStudio2017Install != null && File.Exists(vsInstallerPath))
                {
                    // First, check if a Visual Studio update is needed
                    // VS2017: 15.8+ (for .NET Core 2.1)
                    if (existingVisualStudio2017Install.Version.Major == 15 && existingVisualStudio2017Install.Version.Minor < 8)
                    {
                        // Not sure why, but it seems VS Update is sometimes sending Ctrl+C to our process...
                        try
                        {
                            Console.CancelKeyPress += Console_IgnoreControlC;
                            var vsInstallerExitCode = RunProgramAndAskUntilSuccess("Visual Studio", vsInstallerPath, $"update --passive --norestart --installPath \"{existingVisualStudio2017Install.InstallationPath}\"");
                            if (vsInstallerExitCode != 0)
                            {
                                var errorMessage = $"Visual Studio 2017 update failed with error {vsInstallerExitCode}";
                                MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                throw new InvalidOperationException(errorMessage);
                            }
                        }
                        finally
                        {
                            Console.CancelKeyPress -= Console_IgnoreControlC;
                        }
                    }

                    // Check workloads
                    {
                        var vsInstallerExitCode = RunProgramAndAskUntilSuccess("Visual Studio", vsInstallerPath, $"modify --passive --norestart --installPath \"{existingVisualStudio2017Install.InstallationPath}\" {string.Join(" ", NecessaryVS2017Workloads.Select(x => $"--add {x}"))}");
                        if (vsInstallerExitCode != 0)
                        {
                            var errorMessage = $"Visual Studio 2017 install failed with error {vsInstallerExitCode}";
                            MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            throw new InvalidOperationException(errorMessage);
                        }
                    }

                    // Refresh existingVisualStudio2017Install.Complete and check if restart is needed
                    VisualStudioVersions.Refresh();
                    existingVisualStudio2017Install = VisualStudioVersions.AvailableVisualStudioInstances.FirstOrDefault(x => x.InstallationPath == existingVisualStudio2017Install.InstallationPath);
                    if (existingVisualStudio2017Install != null && !existingVisualStudio2017Install.Complete)
                        MessageBox.Show("Visual Studio 2017 install needs a computer restart.\r\nIf you don't restart, Xenko projects likely won't compile.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    // Otherwise, fallback to vs_buildtools standalone detection and install
                    var buildTools = VisualStudioVersions.AvailableBuildTools.Where(x => x.PackageVersions.ContainsKey("Microsoft.VisualStudio.Workload.MSBuildTools")).ToList();
                    var matchingBuildTool = buildTools.FirstOrDefault(x => NecessaryBuildTools2017Workloads.All(workload => x.PackageVersions.ContainsKey(workload)));
                    string buildToolsCommandLine = null;

                    if (matchingBuildTool == null)
                    {
                        if (buildTools.Count > 0)
                        {
                            // Incomplete installation
                            buildToolsCommandLine = $"modify --wait --passive --norestart --installPath \"{buildTools.First().InstallationPath}\" {string.Join(" ", NecessaryBuildTools2017Workloads.Select(x => $"--add {x}"))}";
                        }
                        else
                        {
                            // Not installed yet
                            buildToolsCommandLine = $"--wait --passive --norestart {string.Join(" ", NecessaryBuildTools2017Workloads.Select(x => $"--add {x}"))}";
                        }
                    }

                    if (buildToolsCommandLine != null)
                    {
                        // Run vs_buildtools again
                        RunProgramAndAskUntilSuccess("Build Tools", "vs_buildtools.exe", buildToolsCommandLine);
                    }
                }
            }
        }

        private static void Console_IgnoreControlC(object sender, ConsoleCancelEventArgs e)
        {
            if (e.SpecialKey == ConsoleSpecialKey.ControlC && !e.Cancel)
                e.Cancel = true;
        }
    }
}
