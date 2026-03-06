// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using NuGet.Frameworks;
using Stride.Core.Packages;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Launcher.Services;

namespace Stride.Launcher.ViewModels;

/// <summary>
/// An implementation of the <see cref="PackageVersionViewModel"/> that represents a major version of Stride.
/// </summary>
public abstract class StrideVersionViewModel : PackageVersionViewModel, IComparable<StrideVersionViewModel>, IComparable<Tuple<int, int>>
{
    private bool isVisible;
    private bool canStart;
    private string? selectedFramework;

    internal StrideVersionViewModel(MainViewModel launcher, NugetStore store, NugetLocalPackage? localPackage, string packageId, int major, int minor)
        : base(launcher, store, localPackage)
    {
        PackageSimpleName = packageId
            .Replace(".GameStudio", string.Empty)
            .Replace(".Avalonia.Desktop", string.Empty);
        Major = major;
        Minor = minor;
        SetAsActiveCommand = new AnonymousCommand(ServiceProvider, () => launcher.ActiveVersion = this);
        // Update status if the user changes whether to display beta versions.
        launcher.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(MainViewModel.ShowBetaVersions)) UpdateStatus(); };
    }

    protected static string[] GetExecutableNames()
    {
        return OperatingSystem.IsWindows()
            ? [
                $"{GameStudioNames.StrideAvalonia}.exe",
                $"{GameStudioNames.Stride}.exe",
                $"{GameStudioNames.Xenko}.exe",
            ]
            : [$"{GameStudioNames.StrideAvalonia}.dll"];
    }

    protected void UpdateFrameworks()
    {
        Frameworks.Clear();
        if (LocalPackage is null || InstallPath is null)
            return;

        foreach (var toplevelFolder in new[] { "tools", "lib" })
        {
            var libDirectory = Path.Combine(InstallPath, toplevelFolder);
            if (!Directory.Exists(libDirectory))
                continue;

            foreach (var frameworkPath in Directory.EnumerateDirectories(libDirectory))
            {
                foreach (var gameStudioExecutable in GetExecutableNames())
                {
                    if (File.Exists(Path.Combine(frameworkPath, gameStudioExecutable)))
                    {
                        Frameworks.Add(new DirectoryInfo(frameworkPath).Name);
                    }
                }
            }
        }
        UpdateSelectedFramework();
    }

    internal void UpdateSelectedFramework()
    {
        if (Frameworks.Count > 0)
        {
            try
            {
                // If preferred framework exists in our list, select it
                var preferredFramework = LauncherSettings.PreferredFramework;
                if (Frameworks.Contains(preferredFramework))
                {
                    SelectedFramework = preferredFramework;
                }
                else
                {
                    // Otherwise, try to find a framework of the same kind (.NET Core or .NET Framework)
                    var nugetFramework = NuGetFramework.ParseFolder(preferredFramework);
                    SelectedFramework =
                        Frameworks.FirstOrDefault(x => NuGetFramework.ParseFolder(preferredFramework).Framework == nugetFramework.Framework)
                        ?? Frameworks.First(); // otherwise fallback to first choice
                }
            }
            catch
            {
                SelectedFramework = Frameworks.First();
            }
        }
    }

    public string PackageSimpleName { get; }

    /// <summary>
    /// Gets the major number of this version.
    /// </summary>
    public int Major { get; }

    /// <summary>
    /// Gets the minor number of this version.
    /// </summary>
    public int Minor { get; }

    /// <summary>
    /// Gets the name of this version.
    /// </summary>
    public override string Name => GetName(PackageSimpleName, Major, Minor);

    /// <summary>
    /// Gets the display name of this version.
    /// </summary>
    public virtual string DisplayName => GetName(PackageSimpleName, Major, Minor, true);

    /// <summary>
    /// Gets the command that will set the associated package as active.
    /// </summary>
    public CommandBase SetAsActiveCommand { get; }

    /// <summary>
    /// Gets whether this version is a beta version.
    /// </summary>
    public bool IsBeta => IsBetaVersion(Major, Minor);

    /// <summary>
    /// Gets whether this version should be displayed.
    /// </summary>
    public bool IsVisible { get { return isVisible; } private set { SetValue(ref isVisible, value); } }

    /// <summary>
    /// Gets whether this version can be started.
    /// </summary>
    public bool CanStart { get { return canStart; } private set { SetValue(ref canStart, value); } }

    public ObservableList<string> Frameworks { get; } = [];

    public string? SelectedFramework { get { return selectedFramework; } set { SetValue(ref selectedFramework, value); } }

    /// <summary>
    /// Builds a string that represents the given version numbers.
    /// </summary>
    /// <param name="majorVersion">The major version number.</param>
    /// <param name="minorVersion">The minor version number.</param>
    /// <param name="isDisplayName">Indicates whether the name to compute is a display name, or a string token used to build urls.</param>
    /// <returns>A string representing the given version numbers.</returns>
    public static string GetName(string packageSimpleName, int majorVersion, int minorVersion, bool isDisplayName = false)
    {
        if (isDisplayName && IsBetaVersion(majorVersion, minorVersion))
            return $"{packageSimpleName} {majorVersion}.{minorVersion}-beta";

        return $"{packageSimpleName} {majorVersion}.{minorVersion}";
    }

    /// <summary>
    /// Indicates if the given version corresponds to a beta version.
    /// </summary>
    /// <param name="majorVersion">The major number of the version.</param>
    /// <param name="minorVersion">The minor nimber of the version.</param>
    /// <returns>True if the given version is a beta, false otherwise.</returns>
    public static bool IsBetaVersion(int majorVersion, int minorVersion)
    {
        return majorVersion < 3;
    }

    /// <inheritdoc/>
    protected override void UpdateStatus()
    {
        base.UpdateStatus();
        // It is visible if it's installed, or if it's not a beta, or if user want to see be available betas
        IsVisible = Launcher.ShowBetaVersions || !IsBeta || CanDelete;
        SetAsActiveCommand.IsEnabled = CanDelete;
        DeleteCommand.IsEnabled = CanDelete;
        CanStart = CanDelete;

        if (Launcher.ActiveVersion == this)
            Launcher.StartStudioCommand.IsEnabled = CanStart;
    }

    /// <summary>
    /// Locate the main executable from a given package installation path. It throws exceptions if not found.
    /// </summary>
    /// <returns>The main executable.</returns>
    public string? LocateMainExecutable()
    {
        if (InstallPath is null)
            return null;

        // First, try to use the selected framework
        if (SelectedFramework is not null)
        {
            foreach (var toplevelFolder in new[] { "tools", "lib" })
            {
                var gameStudioDirectory = Path.Combine(InstallPath, toplevelFolder, SelectedFramework);
                foreach (var gameStudioExecutable in GetExecutableNames())
                {
                    var gameStudioPath = Path.Combine(gameStudioDirectory, gameStudioExecutable);
                    if (File.Exists(gameStudioPath))
                        return gameStudioPath;
                }
            }
        }

        // Otherwise, old-style fallback
        return GetMainExecutables().Select(x => Path.Combine(InstallPath, x)).FirstOrDefault(File.Exists)
            ?? throw new InvalidOperationException("Unable to locate the executable for the selected version");

        static IEnumerable<string> GetMainExecutables()
        {
            // some old paths used in previous versions
            if (OperatingSystem.IsWindows())
            {
                yield return @$"lib\net472\{GameStudioNames.Stride}.exe";
                yield return @$"lib\net472\{GameStudioNames.Xenko}.exe";
                yield return @$"Bin\Windows\{GameStudioNames.Xenko}.exe";
                yield return @$"Bin\Windows-Direct3D11\{GameStudioNames.Xenko}.exe";
            }
        }
    }

    public int CompareTo(StrideVersionViewModel? other)
    {
        var r = Major.CompareTo(other?.Major);
        return r != 0 ? -r : -Minor.CompareTo(other?.Minor);
    }

    public int CompareTo(Tuple<int, int>? other)
    {
        var r = Major.CompareTo(other?.Item1);
        return r != 0 ? -r : -Minor.CompareTo(other?.Item2);
    }
}
