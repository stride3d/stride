// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
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

                // Locate VSIXInstaller.exe
                // We now only deal with VS2019+ which has a unified installer. Still getting latest version of VS possible, in case there is some bugfixes or incompatible changes.
                var visualStudioVersionByVsixVersion = VisualStudioVersions.AvailableVisualStudioInstances.Where(x => x.HasVsixInstaller && x.VsixInstallerVersion == VSIXInstallerVersion.VS2019AndFutureVersions);
                var visualStudioVersion = visualStudioVersionByVsixVersion.OrderByDescending(x => x.Version).FirstOrDefault(x => File.Exists(x.VsixInstallerPath));
                if (visualStudioVersion == null)
                {
                    throw new InvalidOperationException($"Could not find a proper installation of Visual Studio 2019 or later");
                }

                switch (args[0])
                {
                    case "/install":
                    case "/repair":
                    {
                        // Install VSIX
                        var exitCode = RunVsixInstaller(visualStudioVersion.VsixInstallerPath, "\"" + vsixFile + "\"");
                        if (exitCode != 0)
                            throw new InvalidOperationException($"VSIX Installer didn't run properly: exit code {exitCode}");
                        break;
                    }
                    case "/uninstall":
                    {
                        // Note: we allow uninstall to fail (i.e. VSIX was not installed for that specific VIsual Studio version)
                        RunVsixInstaller(visualStudioVersion.VsixInstallerPath, "/uninstall:b0b8feb1-7b83-43fc-9fc0-70065ddb80a1");
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
