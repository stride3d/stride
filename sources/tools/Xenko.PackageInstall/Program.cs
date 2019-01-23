// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
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
        private static readonly string[] NecessaryVS2017Workloads = new[] { "Microsoft.VisualStudio.Workload.ManagedDesktop" };
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
                            var prerequisitesInstalled = false;
                            while (!prerequisitesInstalled)
                            {
                                try
                                {
                                    var prerequisitesInstallerProcess = Process.Start(prerequisitesInstallerPath);
                                    if (prerequisitesInstallerProcess == null)
                                        throw new InvalidOperationException();
                                    prerequisitesInstallerProcess.WaitForExit();
                                    if (prerequisitesInstallerProcess.ExitCode != 0)
                                        throw new InvalidOperationException();
                                    prerequisitesInstalled = true;
                                }
                                catch
                                {
                                    // We'll enter this if UAC has been declined, but also if it timed out (which is a frequent case
                                    // if you don't stay in front of your computer during the installation.
                                    var result = MessageBox.Show("The installation of prerequisites has been canceled by user or failed to run. Do you want to run it again?", "Error",
                                        MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                                    if (result != DialogResult.Yes)
                                        break;
                                }
                            }
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
                    var vsInstaller = Process.Start(vsInstallerPath, $"modify --passive --norestart --installPath \"{existingVisualStudio2017Install.InstallationPath}\" {string.Join(" ", NecessaryVS2017Workloads.Select(x => $"--add {x}"))}");
                    if (vsInstaller == null)
                        throw new InvalidOperationException("Could not run vs_installer.exe");
                    vsInstaller.WaitForExit();

                    MessageBox.Show("Visual Studio 2017 was missing the .NET desktop develpment workload.\r\nWe highly recommend a reboot after the installation is finished, otherwise Xenko projects won't compile.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                        var vsBuildToolsInstaller = Process.Start("vs_buildtools.exe", buildToolsCommandLine);
                        if (vsBuildToolsInstaller == null)
                            throw new InvalidOperationException("Could not run vs_buildtools installer");
                        vsBuildToolsInstaller.WaitForExit();
                    }
                }
            }
        }
    }
}
