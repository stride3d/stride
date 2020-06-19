// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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

using Stride.Core;
using Stride.Core.Extensions;
using Stride.LauncherApp.Views;
using Stride.Core.Packages;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModel;
using MessageBoxButton = Stride.Core.Presentation.Services.MessageBoxButton;
using MessageBoxImage = Stride.Core.Presentation.Services.MessageBoxImage;
using System.Net;
using Stride.LauncherApp.Resources;
using System.Text.RegularExpressions;

namespace Stride.LauncherApp.Services
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
                    await UpdateLauncherFiles(dispatcher, services.Get<IDialogService>(), store, CancellationToken.None);
                }
                catch (Exception e)
                {
                    dispatcher.Invoke(() => selfUpdateWindow?.ForceClose());
                    throw;
                }
            });
        }

        private static async Task UpdateLauncherFiles(IDispatcherService dispatcher, IDialogService dialogService, NugetStore store, CancellationToken cancellationToken)
        {
            var version = new PackageVersion(Version);
            var productAttribute = (typeof(SelfUpdater).Assembly).GetCustomAttribute<AssemblyProductAttribute>();
            var packageId = productAttribute.Product;
            var packages = (await store.GetUpdates(new PackageName(packageId, version), true, true, cancellationToken)).OrderBy(x => x.Version);

            try
            {
                // First, check if there is a package forcing us to download new installer
                const string ReinstallUrlPattern = @"force-reinstall:\s*(\S+)\s*(\S+)";
                var reinstallPackage = packages.LastOrDefault(x => x.Version > version && Regex.IsMatch(x.Description, ReinstallUrlPattern));
                if (reinstallPackage != null)
                {
                    var regexMatch = Regex.Match(reinstallPackage.Description, ReinstallUrlPattern);
                    var minimumVersion = PackageVersion.Parse(regexMatch.Groups[1].Value);
                    if (version < minimumVersion)
                    {
                        var installerDownloadUrl = regexMatch.Groups[2].Value;
                        await DownloadAndInstallNewVersion(dispatcher, dialogService, installerDownloadUrl);
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                await dialogService.MessageBox(string.Format(Strings.NewVersionDownloadError, e.Message), MessageBoxButton.OK, MessageBoxImage.Error);
            }

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
                var exeLocation = Launcher.GetExecutablePath();
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

        internal static async Task DownloadAndInstallNewVersion(IDispatcherService dispatcher, IDialogService dialogService, string strideInstallerUrl)
        {
            try
            {
                // Diplay progress window
                var mainWindow = dispatcher.Invoke(() => Application.Current.MainWindow as LauncherWindow);
                dispatcher.InvokeAsync(() =>
                {
                    selfUpdateWindow = new SelfUpdateWindow { Owner = mainWindow };
                    selfUpdateWindow.LockWindow();
                    selfUpdateWindow.ShowDialog();
                }).Forget();


                var strideInstaller = Path.Combine(Path.GetTempPath(), $"StrideSetup-{Guid.NewGuid()}.exe");
                using (WebClient webClient = new WebClient())
                {
                    webClient.DownloadFile(strideInstallerUrl, strideInstaller);
                }

                var startInfo = new ProcessStartInfo(strideInstaller)
                {
                    UseShellExecute = true
                };
                // Release the mutex before starting the new process
                Launcher.Mutex.Dispose();

                Process.Start(startInfo);

                Environment.Exit(0);
            }
            catch (Exception e)
            {
                await dispatcher.InvokeAsync(() =>
                {
                    if (selfUpdateWindow != null)
                    {
                        selfUpdateWindow.ForceClose();
                    }
                });

                await dialogService.MessageBox(string.Format(Strings.NewVersionDownloadError, e.Message), MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            var exeLocation = Launcher.GetExecutablePath();
            var startInfo = new ProcessStartInfo(exeLocation)
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
