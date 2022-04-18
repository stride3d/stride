// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Stride.Core.VisualStudio;

namespace Stride.VisualStudio.PackageInstall
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

                const string vsixFile = "Stride.vsix";

                // Locate a VS installation with VSIXInstaller.exe.
                // Select the latest version of VS possible, in case there is some bugfixes or incompatible changes.
                var visualStudioVersionByVsixVersion = VisualStudioVersions.AvailableVisualStudioInstances.Where(x => x.HasVsixInstaller);
                var ideInfo = visualStudioVersionByVsixVersion.OrderByDescending(x => x.InstallationVersion).FirstOrDefault(x => File.Exists(x.VsixInstallerPath));
                if (ideInfo == null)
                {
                    throw new InvalidOperationException($"Could not find a proper installation of Visual Studio 2019 or later");
                }

                switch (args[0])
                {
                    case "/install":
                    case "/repair":
                    {
                        // Install VSIX
                        var exitCode = RunVsixInstaller(ideInfo.VsixInstallerPath, "\"" + vsixFile + "\"");
                        if (exitCode != 0)
                            throw new InvalidOperationException($"VSIX Installer didn't run properly: exit code {exitCode}");
                        break;
                    }

                    case "/uninstall":
                    {
                        // Check that the VSIX is intalled
                        bool vsixFound = false;
                        if(Directory.Exists(ideInfo.VsixInstallationPath))
                        {
                            // Since a VSIX installation is put in a random folder, iterate through all of them.
                            foreach(var directory in Directory.GetDirectories(ideInfo.VsixInstallationPath))
                            {
                                if (File.Exists(directory + "\\Stride.VisualStudio.Package.dll"))
                                {
                                    vsixFound = true;
                                    break;
                                }
                            }
                        }

                        // Note: we allow uninstall to fail (i.e. VSIX install was not complete)
                        if (vsixFound) 
                            RunVsixInstaller(ideInfo.VsixInstallerPath, "/uninstall:Stride.VisualStudio.Package.2022");
                        break;
                    }
                }

                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e}");
                return 1;
            }
        }

        /// <summary>
        /// Starts the VSIX installer at the given path with the given argument, and waits for the process to exit before returning.
        /// </summary>
        /// <param name="pathToVsixInstaller">The path to a VSIX installer provided by a version of Visual Studio.</param>
        /// <param name="arguments">The arguments to pass to the VSIX installer.</param>
        /// <returns><c>True</c> if the VSIX installer exited with code 0, <c>False</c> otherwise.</returns>
        private static int RunVsixInstaller(string pathToVsixInstaller, string arguments)
        {
            var process = Process.Start(pathToVsixInstaller, arguments);
            if (process == null)
            {
                return -1;
            }
            process.WaitForExit();
            return process.ExitCode;
        }
    }
}
