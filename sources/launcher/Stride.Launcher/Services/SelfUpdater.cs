// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Stride.Core;
using Stride.Core.Extensions;
using Stride.Core.Packages;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Launcher.Assets.Localization;

namespace Stride.Launcher.Services;

public static class SelfUpdater
{
    public static readonly string? Version;

    private static readonly HttpClient httpClient = new();
    private static SelfUpdateWindow? selfUpdateWindow;

    static SelfUpdater()
    {
        var assembly = Assembly.GetEntryAssembly();
        var assemblyInformationalVersion = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        Version = assemblyInformationalVersion?.InformationalVersion;
    }

    public static void RestartApplication()
    {
        var args = Environment.GetCommandLineArgs().ToList();
        args.Add("/UpdateTargets");
        if (Program.GetExecutablePath() is string exeLocation)
        {

            var startInfo = new ProcessStartInfo(exeLocation)
            {
                Arguments = string.Join(" ", args.Skip(1)),
                WorkingDirectory = Environment.CurrentDirectory,
                UseShellExecute = true
            };
            // Release the mutex before starting the new process
            Launcher.Mutex?.Dispose();
            Process.Start(startInfo);
        }
        Environment.Exit(0);
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
            catch (Exception)
            {
                await dispatcher.InvokeAsync(() => selfUpdateWindow?.ForceClose());
                throw;
            }
        });
    }

    private static async Task DownloadAndInstallNewVersion(IDispatcherService dispatcher, IDialogService dialogService, string strideInstallerUrl)
    {
        try
        {
            // Display progress window
            await dispatcher.InvokeAsync(() =>
            {
                selfUpdateWindow = new();
                selfUpdateWindow.LockWindow();
                if (Application.Current is App { MainWindow: Window window })
                {
                    _ = selfUpdateWindow.ShowDialog(window); // we don't await on purpose here
                }
                selfUpdateWindow.Show();
            });

            var strideInstaller = Path.Combine(Path.GetTempPath(), $"StrideSetup-{Guid.NewGuid()}.exe");
            using (var response = await httpClient.GetAsync(strideInstallerUrl))
            {
                response.EnsureSuccessStatusCode();

                await using var responseStream = await response.Content.ReadAsStreamAsync();
                await using var fileStream = File.Create(strideInstaller);
                await responseStream.CopyToAsync(fileStream);
            }

            var startInfo = new ProcessStartInfo(strideInstaller)
            {
                UseShellExecute = true
            };
            // Release the mutex before starting the new process
            Launcher.Mutex?.Dispose();
            Process.Start(startInfo);
            Environment.Exit(0);
        }
        catch (Exception e)
        {
            await dispatcher.InvokeAsync(() =>
            {
                selfUpdateWindow?.ForceClose();
            });

            await dialogService.MessageBoxAsync(string.Format(Strings.NewVersionDownloadError, e.Message), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static async Task UpdateLauncherFiles(IDispatcherService dispatcher, IDialogService dialogService, NugetStore store, CancellationToken cancellationToken)
    {

        var version = new PackageVersion(Version);
        var productAttribute = (typeof(SelfUpdater).Assembly).GetCustomAttribute<AssemblyProductAttribute>();
        var packageId = productAttribute!.Product;
        var packages = (await store.GetUpdates(new(packageId, version), true, true, cancellationToken)).OrderBy(x => x.Version);

        try
        {
            // First, check if there is a package forcing us to download new installer
            const string reinstallUrlPattern = @"force-reinstall:\s*(\S+)\s*(\S+)";
            var reinstallPackage = packages.LastOrDefault(x => x.Version > version && Regex.IsMatch(x.Description, reinstallUrlPattern));
            if (reinstallPackage is not null)
            {
                var regexMatch = Regex.Match(reinstallPackage.Description, reinstallUrlPattern);
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
            await dialogService.MessageBoxAsync(string.Format(Strings.NewVersionDownloadError, e.Message), MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // If there is a mandatory intermediate upgrade, take it, otherwise update straight to latest version
        var package = (packages.FirstOrDefault(x => x.Version > version && x.Version.SpecialVersion == "req") ?? packages.LastOrDefault());

        // Check to see if an update is needed
        if (package is null || version >= new PackageVersion(package.Version.Version, package.Version.SpecialVersion))
        {
            return;
        }

        // Display progress window
        await dispatcher.InvokeAsync(() =>
        {
            selfUpdateWindow = new();
            selfUpdateWindow.LockWindow();
            if (Application.Current is App { MainWindow: Window window })
            {
                _ = selfUpdateWindow.ShowDialog(window); // we don't await on purpose here
            }
            else
            {
                throw new ApplicationException("Update requested without a Launcher Window. Cannot continue!");
            }
        }, cancellationToken);

        var movedFiles = new List<string>();

        // Download package
        var installedPackage = await store.InstallPackage(package.Id, package.Version, package.TargetFrameworks, null);

        // Copy files from tools\ to the current directory
        var inputFiles = installedPackage.GetFiles();

        // TODO: We should get list of previous files from nuspec (store it as a resource and open it with NuGet API maybe?)
        // TODO: For now, we deal only with the App.config file since we won't be able to fix it afterward.
        var exeLocation = Program.GetExecutablePath();
        var exeDirectory = Path.GetDirectoryName(exeLocation)!;
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
        // Restart
        dispatcher.InvokeAsync(RestartApplication, cancellationToken).Forget();
        return;

        static void EnsureDirectory(string filePath)
        {
            // Create dest directory if it exists
            var directory = Path.GetDirectoryName(filePath);
            if (directory is not null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        static void Move(string oldPath, string newPath)
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

        static void UpdateFile(string newFilePath, PackageFile file)
        {
            EnsureDirectory(newFilePath);
            using var fromStream = file.GetStream();
            using var toStream = File.Create(newFilePath);
            fromStream.CopyTo(toStream);
        }
    }
}
