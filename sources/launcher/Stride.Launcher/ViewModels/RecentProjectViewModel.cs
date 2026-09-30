// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Avalonia.Input.Platform;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Launcher.Assets.Localization;
using Stride.Launcher.Services;

namespace Stride.Launcher.ViewModels;

public sealed class RecentProjectViewModel : DispatcherViewModel
{
    private readonly UFile fullPath;
    private string strideVersionName;
    private Version? strideVersion;
    // The exact version the project references (4.4.0-dev3); an installed version with that name is preferred.
    private string? stridePackageVersion;
    private PackageVersion? projectVersion;
    private string? upgradeToolTip;
    private bool hasUpgradeOption;
    private OpenWithOption? defaultOption;

    internal RecentProjectViewModel(MainViewModel launcher, UFile path)
        : base(launcher.SafeArgument(nameof(launcher)).ServiceProvider)
    {
        Name = path.GetFileNameWithoutExtension();
        Launcher = launcher;
        fullPath = path;
        strideVersionName = Strings.ReportDiscovering;
        OpenCommand = new AnonymousTaskCommand(ServiceProvider, () => OpenWith(null)) { IsEnabled = false };
        OpenWithCommand = new AnonymousTaskCommand<OpenWithOption>(ServiceProvider, OpenWith);
        ExploreCommand = new AnonymousCommand(ServiceProvider, Explore);
        OpenInIDECommand = new AnonymousCommand(ServiceProvider, OpenInIDE);
        CopySolutionPathCommand = new AnonymousTaskCommand(ServiceProvider, () => CopyToClipboard(FullPath));
        CopyFolderPathCommand = new AnonymousTaskCommand(ServiceProvider, () => CopyToClipboard(FolderPath));
        RemoveCommand = new AnonymousCommand(ServiceProvider, Remove);
        CompatibleVersions = [];
        DiscoverStrideVersion();
    }

    public string Name { get; private set; }

    public string FullPath => fullPath.ToOSPath();

    /// <summary>
    /// Gets the folder of the project's solution.
    /// </summary>
    public string FolderPath => Path.GetDirectoryName(FullPath) ?? FullPath;

    public string StrideVersionName { get { return strideVersionName; } private set { SetValue(ref strideVersionName, value); } }

    /// <summary>
    /// Gets the version shown for the project, as the list of versions names it: its major.minor (4.4), or the full
    /// version of a local build (4.4.0-dev3).
    /// </summary>
    public string? StrideVersionDisplayName => projectVersion is { IsLocalBuild: true } ? stridePackageVersion : StrideVersionName;

    public Version? StrideVersion { get { return strideVersion; } private set { SetValue(ref strideVersion, value); } }

    public MainViewModel Launcher { get; }

    /// <summary>
    /// Gets the other versions the project can be opened with.
    /// </summary>
    public ObservableList<OpenWithOption> CompatibleVersions { get; private set; }

    /// <summary>
    /// Gets the tooltip telling that a click upgrades the project, or null when it doesn't.
    /// </summary>
    public string? UpgradeToolTip { get { return upgradeToolTip; } private set { SetValue(ref upgradeToolTip, value); } }

    /// <summary>
    /// Gets whether one of <see cref="CompatibleVersions"/> upgrades the project to a release.
    /// </summary>
    public bool HasUpgradeOption { get { return hasUpgradeOption; } private set { SetValue(ref hasUpgradeOption, value, nameof(HasUpgradeOption), nameof(OpenWithToolTip)); } }

    public string OpenWithToolTip => HasUpgradeOption ? Strings.ToolTipOpenWithUpgrade : Strings.ToolTipOpenWithAnotherVersion;

    /// <summary>
    /// Gets the tooltip of the project: its path, its full version, and the version a click opens it with when another.
    /// </summary>
    public string ToolTip
    {
        get
        {
            var lines = new List<string> { FullPath };
            if (projectVersion is not null)
                lines.Add(string.Format(Strings.ToolTipProjectVersion, projectVersion));
            else if (StrideVersionName is null)
                lines.Add(Strings.UnknownVersion);
            if (defaultOption?.TargetVersion is { } target && target != projectVersion)
                lines.Add(string.Format(defaultOption.IsUpgrade ? Strings.ToolTipProjectOpensWithUpgrade : Strings.ToolTipProjectOpensWith, target));
            return string.Join(Environment.NewLine, lines);
        }
    }

    public ICommandBase ExploreCommand { get; }

    /// <summary>
    /// Gets the command that opens the solution of the project with its default application (Visual Studio, Rider...).
    /// </summary>
    public ICommandBase OpenInIDECommand { get; }

    public ICommandBase CopySolutionPathCommand { get; }

    public ICommandBase CopyFolderPathCommand { get; }

    public ICommandBase OpenCommand { get; }

    public ICommandBase OpenWithCommand { get; }

    public ICommandBase RemoveCommand { get; }

    private void DiscoverStrideVersion()
    {
        // A recent project can be moved or deleted: then there is no version to find (shown as unknown), and it can't be opened
        if (!File.Exists(FullPath))
        {
            StrideVersionName = null;
            OnPropertyChanged(nameof(StrideVersionDisplayName), nameof(ToolTip));
            return;
        }

        Task.Run(async () =>
        {
            var packageVersion = await PackageSessionHelper.GetPackageVersion(fullPath);
            stridePackageVersion = packageVersion?.ToString();
            projectVersion = packageVersion;
            StrideVersion = packageVersion is not null ? new Version(packageVersion.Version.Major, packageVersion.Version.Minor) : null;
            StrideVersionName = StrideVersion?.ToString();
            OnPropertyChanged(nameof(StrideVersionDisplayName));

            Dispatcher.Invoke(() =>
            {
                OpenCommand.IsEnabled = StrideVersionName is not null;
                // The versions can be listed before the project's version is known
                UpdateOpenOptions(Launcher.StrideVersions);
            });
        });
    }

    private void OpenInIDE()
    {
        // The project can be moved or deleted since it was listed
        if (!File.Exists(FullPath))
            return;

        try
        {
            Process.Start(new ProcessStartInfo(FullPath) { UseShellExecute = true });
        }
        catch (Exception e)
        {
            // No application for solutions: nothing the user can act on here
            e.Ignore();
        }
    }

    // Through the main window: in Avalonia, the clipboard belongs to a window
    private static async Task CopyToClipboard(string text)
    {
        if (Avalonia.Application.Current is not App { MainWindow.Clipboard: { } clipboard })
            return;

        try
        {
            await clipboard.SetTextAsync(text);
        }
        catch (Exception e)
        {
            // Clipboard in use by another application: nothing the user can act on here
            e.Ignore();
        }
    }

    private void Explore()
    {
        // FullPath already resolves to the OS-native string path (see FullPath property above).
        if (!File.Exists(FullPath))
        {
            return;
        }

        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{FullPath}\"")
                {
                    UseShellExecute = true,
                });
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start(new ProcessStartInfo("open", $"-R \"{FullPath}\"")
                {
                    UseShellExecute = false,
                });
            }
            else // Linux and any other Unix
            {
                if (!TryRevealFileDBus(FullPath))
                {
                    var parent = Path.GetDirectoryName(FullPath);
                    if (parent is not null)
                    {
                        Process.Start(new ProcessStartInfo("xdg-open", parent)
                        {
                            UseShellExecute = false,
                        });
                    }
                }
            }
        }
        catch
        {
            // File-manager failures are not actionable for the user — silently ignore.
        }
    }

    private static bool TryRevealFileDBus(string path)
    {
        try
        {
            var uri = new Uri(path).AbsoluteUri; // "file:///…" with correct percent-encoding

            var psi = new ProcessStartInfo("dbus-send", string.Join(' ',
                "--session",
                "--type=method_call",
                "--dest=org.freedesktop.FileManager1",
                "/org/freedesktop/FileManager1",
                "org.freedesktop.FileManager1.ShowItems",
                $"array:string:\"{uri}\"",
                "string:\"\""))
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using var process = Process.Start(psi);
            if (process is null) return false;

            process.WaitForExit(2000); // 2s ceiling; healthy DBus round-trips are sub-10ms.
            return process.HasExited && process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private void Remove()
    {
        //Remove files that's was deleted or upgraded by stride versions <= 3.0
        if (string.IsNullOrEmpty(StrideVersionName) || string.Compare(StrideVersionName, "3.0", StringComparison.Ordinal) <= 0)
        {
            //Get all installed versions 
            var strideInstalledVersions = Launcher.StrideVersions.Where(x => x.CanDelete)
                .Select(x => $"{x.Major}.{x.Minor}").ToList();

            //If original version of files is not in list get and to add it.
            if (!string.IsNullOrEmpty(StrideVersionName) && !strideInstalledVersions.Any(x => x.Equals(StrideVersionName)))
                strideInstalledVersions.Add(StrideVersionName);

            foreach (var item in strideInstalledVersions)
            {
                GameStudioSettings.RemoveMostRecentlyUsed(fullPath, item);
            }
        }
        else
        {
            GameStudioSettings.RemoveMostRecentlyUsed(fullPath, StrideVersionName);
        }
    }

    private async Task OpenWith(OpenWithOption? option)
    {
        string message;
        option ??= DefaultOption(Launcher.StrideVersions);
        var version = option?.Version;
        if (version is null)
        {
            message = string.Format(Strings.ErrorDoNotFindVersion, StrideVersion);
            await ServiceProvider.Get<IDialogService>().MessageBoxAsync(message, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (version.IsProcessing)
        {
            message = string.Format(Strings.ErrorVersionBeingUpdated, StrideVersion);
            await ServiceProvider.Get<IDialogService>().MessageBoxAsync(message, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!version.CanDelete)
        {
            message = string.Format(Strings.ErrorVersionNotInstalled, StrideVersion);
            var result = await ServiceProvider.Get<IDialogService>().MessageBoxAsync(message, MessageBoxButton.YesNoCancel, MessageBoxImage.Information);
            if (result == MessageBoxResult.Yes)
            {
                version.DownloadCommand.Execute();
            }
            return;
        }
        // Game Studio doesn't open a project on an older version, unless one of them is a local build
        if (option!.Build is null && version is StrideStoreVersionViewModel && projectVersion is { IsLocalBuild: false }
            && version.InstalledVersion < projectVersion)
        {
            message = string.Format(Strings.ErrorVersionTooOld, projectVersion, version.InstalledVersion);
            await ServiceProvider.Get<IDialogService>().MessageBoxAsync(message, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        option.Build?.SetAsActiveCommand.Execute();
        Launcher.ActiveVersion = version;
        Launcher.StartStudio(FullPath).Forget();
    }

    /// <summary>
    /// Updates the versions the project can be opened with, and whether they upgrade it.
    /// </summary>
    internal void UpdateOpenOptions(IEnumerable<StrideVersionViewModel> versions)
    {
        CompatibleVersions.Clear();
        defaultOption = DefaultOption(versions);
        UpgradeToolTip = defaultOption is { IsUpgrade: true }
            ? string.Format(Strings.ToolTipOpenUpgrades, projectVersion, defaultOption.TargetVersion)
            : null;
        OnPropertyChanged(nameof(ToolTip));

        // Manually discarding the possibility to upgrade from 1.0
        if (StrideVersionName == "1.0")
        {
            HasUpgradeOption = false;
            return;
        }

        // Every installed version but the one a click opens the project with, and none that Game Studio doesn't
        // open it with: below its major.minor, or an older build of it (unless one of them is a local build).
        // The builds of the project's major.minor are listed one by one, as they are the ones it's on
        var olderRefused = projectVersion is { IsLocalBuild: false };
        foreach (var version in versions)
        {
            if (version is StrideStoreVersionViewModel storeVersion && new Version(version.Major, version.Minor) == StrideVersion)
            {
                foreach (var build in storeVersion.AlternateVersions.Where(x => x.LocalPackage is not null).OrderByDescending(x => x.Version))
                {
                    if (defaultOption?.Version == version && defaultOption.TargetVersion == build.Version)
                        continue;
                    if (olderRefused && build.Version < projectVersion && !build.Version.IsLocalBuild)
                        continue;
                    CompatibleVersions.Add(new(version, build, projectVersion));
                }
                continue;
            }

            if (version == defaultOption?.Version)
                continue;
            // A dev version added by hand may have no package: we suppose it works with any project
            var installedVersion = version.InstalledVersion;
            if (installedVersion is null && version is not StrideDevVersionViewModel)
                continue;
            if (installedVersion is not null && StrideVersion is not null
                && new Version(installedVersion.Version.Major, installedVersion.Version.Minor) < StrideVersion)
                continue;

            CompatibleVersions.Add(new(version, null, projectVersion));
        }
        HasUpgradeOption = CompatibleVersions.Any(IsSuggestedUpgrade);
    }

    /// <summary>
    /// Gets the option a click on this project opens it with: the exact build it references when that one is installed,
    /// else the newest installed build of its major.minor (an upgrade, when new enough).
    /// </summary>
    private OpenWithOption? DefaultOption(IEnumerable<StrideVersionViewModel> versions)
    {
        var exact = versions.FirstOrDefault(x => x is not StrideStoreVersionViewModel && x.CanDelete && x.FullName == stridePackageVersion);
        if (exact is not null)
            return new(exact, null, projectVersion);

        var sameMinor = versions.OfType<StrideStoreVersionViewModel>().FirstOrDefault(x => new Version(x.Major, x.Minor) == StrideVersion);
        if (sameMinor is null)
            return null;

        var builds = sameMinor.AlternateVersions.Where(x => x.LocalPackage is not null).ToList();
        var build = builds.FirstOrDefault(x => x.Version == projectVersion)
            ?? builds.Where(x => projectVersion is null || projectVersion.IsLocalBuild || x.Version >= projectVersion).MaxBy(x => x.Version);
        return new(sameMinor, build, projectVersion);
    }

    /// <summary>
    /// Gets whether opening a project on <paramref name="projectVersion"/> with <paramref name="targetVersion"/> upgrades
    /// it, that is Game Studio doesn't open it with <paramref name="projectVersion"/> anymore: a newer version, unless
    /// one of them is a local build and the target has the same major.minor (4.4.0-beta8 and 4.4.0-dev3 switch).
    /// </summary>
    internal static bool IsUpgrade(PackageVersion? projectVersion, PackageVersion? targetVersion)
    {
        if (projectVersion is null || targetVersion is null || targetVersion <= projectVersion)
            return false;
        var switchesBack = (projectVersion.IsLocalBuild || targetVersion.IsLocalBuild)
            && new Version(projectVersion.Version.Major, projectVersion.Version.Minor) >= new Version(targetVersion.Version.Major, targetVersion.Version.Minor);
        return !switchesBack;
    }

    /// <summary>
    /// Gets whether <paramref name="option"/> is an upgrade to suggest: to a release, as a local build is for its developer.
    /// </summary>
    private static bool IsSuggestedUpgrade(OpenWithOption option)
        => option.IsUpgrade && !option.TargetVersion!.IsLocalBuild;
}
