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

    /// <summary>
    /// <see cref="Version"/> without its build metadata (the commit after the '+'), for display.
    /// </summary>
    public static readonly string? DisplayVersion;

    private static SelfUpdateWindow? selfUpdateWindow;

    static SelfUpdater()
    {
        var assembly = Assembly.GetEntryAssembly();
        var assemblyInformationalVersion = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        Version = assemblyInformationalVersion?.InformationalVersion;
        DisplayVersion = Version?.Split('+')[0];
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

    internal static Task SelfUpdate(IViewModelServiceProvider services, NugetStore store, bool includePrerelease)
    {
        return Task.Run(async () =>
        {
            DeleteOldFiles();
            var dispatcher = services.Get<IDispatcherService>();
            try
            {
                await UpdateLauncherFiles(dispatcher, services.Get<IDialogService>(), store, includePrerelease, CancellationToken.None);
            }
            catch (Exception)
            {
                await dispatcher.InvokeAsync(() => selfUpdateWindow?.ForceClose());
                throw;
            }
        });
    }

    /// <summary>
    /// Deletes the files the previous update renamed to ".old": that launcher was running from its exe, so it couldn't.
    /// </summary>
    private static void DeleteOldFiles()
    {
        if (Program.GetExecutablePath() is not string exeLocation)
            return;

        foreach (var oldFile in new[] { exeLocation + ".old", exeLocation + ".config.old" })
        {
            try
            {
                File.Delete(oldFile);
            }
            catch (Exception e)
            {
                // Still in use while the previous launcher exits: the next start deletes it
                e.Ignore();
            }
        }
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
            using (var response = await LauncherHttpClient.Instance.GetAsync(strideInstallerUrl))
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

    /// <summary>
    /// Whether the launcher at <paramref name="current"/> may update itself to <paramref name="candidate"/>: releases always;
    /// pre-releases of its own version when it is one (6.0.1-beta1 gets 6.0.1-beta2, then 6.0.1); other pre-releases only
    /// when the user opted in.
    /// </summary>
    internal static bool IsUpdateCandidate(PackageVersion candidate, PackageVersion current, bool includePrerelease)
    {
        if (includePrerelease || IsRelease(candidate))
            return true;
        return !IsRelease(current) && candidate.Version == current.Version;

        static bool IsRelease(PackageVersion version) => version.SpecialVersion.Length == 0;
    }

    /// <summary>
    /// The update rules of a package, from the <c>update:</c> line of its description:
    /// <c>update: [checkpoint] [reinstall-below=&lt;version&gt;] [setup=&lt;url&gt;]</c>.
    /// </summary>
    /// <param name="Checkpoint">Every older launcher updates to this package before any newer one.</param>
    /// <param name="ReinstallBelow">Launchers below this version install <paramref name="Setup"/> instead of swapping their files.</param>
    /// <param name="Setup">The setup of this package.</param>
    internal sealed record UpdateRules(bool Checkpoint, PackageVersion? ReinstallBelow, string? Setup)
    {
        public static UpdateRules Parse(string? description)
        {
            var match = Regex.Match(description ?? "", @"^update:(.*)$", RegexOptions.Multiline);
            var (checkpoint, reinstallBelow, setup) = (false, default(PackageVersion), default(string));
            // Unknown words are skipped, so that a later launcher version can add rules
            foreach (var word in match.Groups[1].Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            {
                if (word == "checkpoint")
                    checkpoint = true;
                else if (word.StartsWith("reinstall-below=", StringComparison.Ordinal) && PackageVersion.TryParse(word["reinstall-below=".Length..], out var version))
                    reinstallBelow = version;
                else if (word.StartsWith("setup=", StringComparison.Ordinal))
                    setup = word["setup=".Length..];
            }
            return new(checkpoint, reinstallBelow, setup);
        }
    }

    /// <summary>
    /// Chooses the update among <paramref name="candidates"/> (the rules of the update candidates, oldest first): the
    /// first checkpoint, otherwise the newest. It is reached by installing its setup when its rules say that
    /// <paramref name="current"/> can't swap to it, otherwise by swapping the launcher files.
    /// </summary>
    /// <returns>The index of the chosen candidate and whether to install its setup, or null when there is none.</returns>
    internal static (int Index, bool Reinstall)? ChooseUpdate(IReadOnlyList<UpdateRules> candidates, PackageVersion current)
    {
        if (candidates.Count == 0)
            return null;

        var index = candidates.Count - 1;
        for (var i = 0; i < candidates.Count; i++)
        {
            if (candidates[i].Checkpoint)
            {
                index = i;
                break;
            }
        }
        var reinstallBelow = candidates[index].ReinstallBelow;
        return (index, reinstallBelow is not null && current < reinstallBelow);
    }

    private static async Task UpdateLauncherFiles(IDispatcherService dispatcher, IDialogService dialogService, NugetStore store, bool includePrerelease, CancellationToken cancellationToken)
    {
        var version = new PackageVersion(Version);
        var productAttribute = (typeof(SelfUpdater).Assembly).GetCustomAttribute<AssemblyProductAttribute>();
        var packageId = productAttribute!.Product;
        var packages = (await store.GetUpdates(new(packageId, version), true, true, cancellationToken))
            .Where(x => x.Version > version && IsUpdateCandidate(x.Version, version, includePrerelease))
            .OrderBy(x => x.Version)
            .ToList();

        var rules = packages.Select(x => UpdateRules.Parse(x.Description)).ToList();
        if (ChooseUpdate(rules, version) is not { } update)
            return;

        var package = packages[update.Index];
        if (update.Reinstall)
        {
            // Swapping the files isn't enough to reach this package: install its setup instead (a Windows installer)
            if (OperatingSystem.IsWindows() && rules[update.Index].Setup is { } setup)
                await DownloadAndInstallNewVersion(dispatcher, dialogService, setup);
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
