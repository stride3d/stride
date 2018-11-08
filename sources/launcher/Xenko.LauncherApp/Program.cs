// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Xenko.LauncherApp
{
    static class Program
    {
        private const string LauncherPrerequisites = @"Prerequisites\launcher-prerequisites.exe";

        [STAThread]
        [LoaderOptimization(LoaderOptimization.MultiDomainHost)] // Optimize loading of AppDomain assemblies
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
                var exeLocation = Assembly.GetEntryAssembly().Location;
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

            // Loading assemblies as embedded resources
            // see http://www.digitallycreated.net/Blog/61/combining-multiple-assemblies-into-a-single-exe-for-a-wpf-application
            // NOTE: this class should not reference any of the embedded type to ensure the handler is registered before
            // these types are loaded
            // TODO: can we register this handler in the Module initializer?
            AppDomain.CurrentDomain.AssemblyResolve += OnResolveAssembly;
            AppDomain.CurrentDomain.ExecuteAssemblyByName("Xenko.Launcher", null, args);
        }

        private static bool CheckPrerequisites(StringBuilder prerequisiteLog)
        {
            var result = true;

            // Check for .NET 4.7.2+
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

        private static Assembly OnResolveAssembly(object sender, ResolveEventArgs args)
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            var assemblyName = new AssemblyName(args.Name);

            // PCL System assemblies are using version 2.0.5.0 while we have a 4.0
            // Redirect the PCL to use the 4.0 from the current app domain.
            if (assemblyName.Name.StartsWith("System") && (assemblyName.Flags & AssemblyNameFlags.Retargetable) != 0)
            {
                Assembly systemCoreAssembly = null;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.GetName().Name == assemblyName.Name)
                    {
                        systemCoreAssembly = assembly;
                        break;
                    }
                }
                return systemCoreAssembly;
            }

            foreach (var extension in new string[] { ".dll", ".exe" })
            {
                var path = assemblyName.Name + extension;
                if (assemblyName.CultureInfo != null && assemblyName.CultureInfo.Equals(CultureInfo.InvariantCulture) == false)
                {
                    path = $@"{assemblyName.CultureInfo}\{path}";
                }

                using (Stream stream = executingAssembly.GetManifestResourceStream(path))
                {
                    if (stream != null)
                    {
                        var assemblyRawBytes = new byte[stream.Length];
                        stream.Read(assemblyRawBytes, 0, assemblyRawBytes.Length);
#if DEBUG
                        byte[] symbolsRawBytes = null;
                        // Let's load the PDB if it exists
                        using (Stream symbolsStream = executingAssembly.GetManifestResourceStream(assemblyName.Name + ".pdb"))
                        {
                            if (symbolsStream != null)
                            {
                                symbolsRawBytes = new byte[symbolsStream.Length];
                                symbolsStream.Read(symbolsRawBytes, 0, symbolsRawBytes.Length);
                            }
                        }
                        return Assembly.Load(assemblyRawBytes, symbolsRawBytes);
#else
                        return Assembly.Load(assemblyRawBytes);
#endif
                    }
                }
            }

            return null;
        }
    }
}
