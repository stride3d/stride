// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
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

    internal RecentProjectViewModel(MainViewModel launcher, UFile path)
        : base(launcher.SafeArgument(nameof(launcher)).ServiceProvider)
    {
        Name = path.GetFileNameWithoutExtension();
        Launcher = launcher;
        fullPath = path;
        strideVersionName = Strings.ReportDiscovering;
        OpenCommand = new AnonymousTaskCommand(ServiceProvider, () => OpenWith(null)) { IsEnabled = false };
        OpenWithCommand = new AnonymousTaskCommand<StrideVersionViewModel>(ServiceProvider, OpenWith);
        ExploreCommand = new AnonymousCommand(ServiceProvider, Explore);
        RemoveCommand = new AnonymousCommand(ServiceProvider, Remove);
        CompatibleVersions = [];
        DiscoverStrideVersion();
    }

    public string Name { get; private set; }

    public string FullPath => fullPath.ToOSPath();

    public string StrideVersionName { get { return strideVersionName; } private set { SetValue(ref strideVersionName, value); } }

    public Version? StrideVersion { get { return strideVersion; } private set { SetValue(ref strideVersion, value); } }

    public MainViewModel Launcher { get; }

    public ObservableList<StrideVersionViewModel> CompatibleVersions { get; private set; }

    public ICommandBase ExploreCommand { get; }

    public ICommandBase OpenCommand { get; }

    public ICommandBase OpenWithCommand { get; }

    public ICommandBase RemoveCommand { get; }

    private void DiscoverStrideVersion()
    {
        Task.Run(async () =>
        {
            var packageVersion = await PackageSessionHelper.GetPackageVersion(fullPath);
            StrideVersion = packageVersion is not null ? new Version(packageVersion.Version.Major, packageVersion.Version.Minor) : null;
            StrideVersionName = StrideVersion?.ToString();

            Dispatcher.Invoke(() => OpenCommand.IsEnabled = StrideVersionName is not null);
        });
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

    private async Task OpenWith(StrideVersionViewModel? version)
    {
        string message;
        version ??= Launcher.StrideVersions.FirstOrDefault(x => new Version(x.Major, x.Minor) == StrideVersion);
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
        Launcher.ActiveVersion = version;
        Launcher.StartStudio($"\"{FullPath}\"").Forget();
    }
}
