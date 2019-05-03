// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using Xenko.Core;
using Xenko.Core.Extensions;
using Xenko.LauncherApp.Views;
using Xenko.Core.Packages;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Presentation.ViewModel;
using MessageBoxButton = Xenko.Core.Presentation.Services.MessageBoxButton;
using MessageBoxImage = Xenko.Core.Presentation.Services.MessageBoxImage;

namespace Xenko.LauncherApp.Services
{
    public static class SelfUpdater
    {
        public static readonly string Version;

        private static SelfUpdateWindow selfUpdateWindow;

        static SelfUpdater()
        {
            var assembly = Assembly.GetEntryAssembly();
            var assemblyInformationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            Version = assemblyInformationalVersion.InformationalVersion;
        }

        internal static Task SelfUpdate(IViewModelServiceProvider services, NugetStore store)
        {
            return Task.Run(async () =>
            {
                var dispatcher = services.Get<IDispatcherService>();
                try
                {
                    await UpdateLauncherFiles(dispatcher, store, CancellationToken.None);
                }
                catch (Exception e)
                {
                    dispatcher.Invoke(() => selfUpdateWindow?.ForceClose());
                    throw;
                }
            });
        }

        private static async Task UpdateLauncherFiles(IDispatcherService dispatcher, NugetStore store, CancellationToken cancellationToken)
        {
            var version = new PackageVersion(Version);
            var productAttribute = (typeof(SelfUpdater).Assembly).GetCustomAttribute<AssemblyProductAttribute>();
            var packageId = productAttribute.Product;
            var packages = (await store.GetUpdates(new PackageName(packageId, version), true, true, cancellationToken)).OrderBy(x => x.Version);

            // If there is a mandatory intermediate upgrade, take it, otherwise update straight to latest version
            var package = (packages.FirstOrDefault(x => x.Version > version && x.Version.SpecialVersion == "req") ?? packages.LastOrDefault());

            // Check to see if an update is needed
            if (package != null && version < new PackageVersion(package.Version.Version, package.Version.SpecialVersion))
            {
                var windowCreated = new TaskCompletionSource<SelfUpdateWindow>();
                var mainWindow = dispatcher.Invoke(() => Application.Current.MainWindow as LauncherWindow);
                if (mainWindow == null)
                    throw new ApplicationException("Update requested without a Launcher Window. Cannot continue!");

                dispatcher.InvokeAsync(() =>
                {
                    selfUpdateWindow = new SelfUpdateWindow { Owner = mainWindow };
                    windowCreated.SetResult(selfUpdateWindow);
                    selfUpdateWindow.ShowDialog();
                }).Forget();

                var movedFiles = new List<string>();

                // Download package
                var installedPackage = await store.InstallPackage(package.Id, package.Version, null);

                // Copy files from tools\ to the current directory
                var inputFiles = installedPackage.GetFiles();

                var window = windowCreated.Task.Result;
                dispatcher.Invoke(window.LockWindow);

                // TODO: We should get list of previous files from nuspec (store it as a resource and open it with NuGet API maybe?)
                // TODO: For now, we deal only with the App.config file since we won't be able to fix it afterward.
                var exeLocation = Assembly.GetEntryAssembly().Location;
                var exeDirectory = Path.GetDirectoryName(exeLocation);
                const string directoryRoot = "tools/"; // Important!: this is matching where files are store in the nuspec
                try
                {
                    if (File.Exists(exeLocation))
                    {
                        Move(exeLocation, exeLocation + ".old");
                        movedFiles.Add(exeLocation);
                    }
                    var configLocation = exeLocation + ".config";
                    if (File.Exists(configLocation))
                    {
                        Move(configLocation, configLocation + ".old");
                        movedFiles.Add(configLocation);
                    }
                    foreach (var file in inputFiles.Where(file => file.Path.StartsWith(directoryRoot) && !file.Path.EndsWith("/")))
                    {
                        var fileName = Path.Combine(exeDirectory, file.Path.Substring(directoryRoot.Length));

                        // Move previous files to .old
                        if (File.Exists(fileName))
                        {
                            Move(fileName, fileName + ".old");
                            movedFiles.Add(fileName);
                        }

                        // Update the file
                        UpdateFile(fileName, file);
                    }
                }
                catch (Exception)
                {
                    // Revert all olds files if a file didn't work well
                    foreach (var oldFile in movedFiles)
                    {
                        Move(oldFile + ".old", oldFile);
                    }
                    throw;
                }


                // Remove .old files
                foreach (var oldFile in movedFiles)
                {
                    try
                    {
                        var renamedPath = oldFile + ".old";

                        if (File.Exists(renamedPath))
                        {
                            File.Delete(renamedPath);
                        }
                    }
                    catch (Exception)
                    {
                        // All the files have been replaced, we let it go even if we cannot remove all the old files.
                    }
                }

                // Clean cache from files obtain via package.GetFiles above.
                store.PurgeCache();

                dispatcher.Invoke(RestartApplication);
            }
        }

        private static void Move(string oldPath, string newPath)
        {
            EnsureDirectory(newPath);
            try
            {
                if (File.Exists(newPath))
                {
                    File.Delete(newPath);
                }
            }
            catch (FileNotFoundException)
            {

            }

            File.Move(oldPath, newPath);
        }

        private static void EnsureDirectory(string filePath)
        {
            // Create dest directory if it exists
            var directory = Path.GetDirectoryName(filePath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private static void UpdateFile(string newFilePath, PackageFile file)
        {
            EnsureDirectory(newFilePath);
            using (Stream fromStream = file.GetStream(), toStream = File.Create(newFilePath))
            {
                fromStream.CopyTo(toStream);
            }
        }

        public static void RestartApplication()
        {
            var args = Environment.GetCommandLineArgs().ToList();
            args.Add("/UpdateTargets");
            var startInfo = new ProcessStartInfo(Assembly.GetEntryAssembly().Location)
            {
                Arguments = string.Join(" ", args.Skip(1)),
                WorkingDirectory = Environment.CurrentDirectory,
                UseShellExecute = true
            };
            // Release the mutex before starting the new process
            Launcher.Mutex.Dispose();
            Process.Start(startInfo);
            Environment.Exit(0);
        }
    }
}
