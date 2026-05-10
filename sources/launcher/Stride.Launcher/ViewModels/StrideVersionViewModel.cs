// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using NuGet.Frameworks;
using Stride.Core.Packages;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;

namespace Stride.Launcher.ViewModels;

/// <summary>
/// An implementation of the <see cref="PackageVersionViewModel"/> that represents a major version of Stride.
/// </summary>
public abstract class StrideVersionViewModel : PackageVersionViewModel, IComparable<StrideVersionViewModel>, IComparable<Tuple<int, int>>
{
    private bool isVisible;
    private bool canStart;
    private string? selectedFramework;
    private string? selectedEditor;
    // Maps each discovered editor name to its fully-resolved directory (including TFM subfolder).
    // e.g. "Stride.GameStudio.Avalonia.Desktop" → ".../lib/net10.0"
    // Populated by UpdateAvailableEditors; consumed by LocateMainExecutable.
    private readonly Dictionary<string, string> _editorToDir = [];

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

    /// <summary>
    /// Returns all local install base paths that belong to this version slot.
    /// The base implementation returns only the primary <see cref="PackageVersionViewModel.InstallPath"/>.
    /// Subclasses such as <see cref="StrideStoreVersionViewModel"/> override this to also
    /// include alternate package paths (e.g. a sibling Avalonia or WPF package).
    /// </summary>
    protected virtual IEnumerable<string> GetAllInstalledPaths()
    {
        if (InstallPath is not null)
            yield return InstallPath;
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
                var preferredFramework = Launcher.Settings.PreferredFramework;
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
        // Always refresh: even if the selected framework didn't change, alternate packages
        // may have been added or removed since the last call.
        UpdateAvailableEditors();
    }

    /// <summary>
    /// Updates the list of editors available across all installed package paths and restores
    /// the last preferred editor if it is still available.
    /// </summary>
    /// <remarks>
    /// Each editor (Avalonia, WPF) may live in a different NuGet package directory <em>and</em>
    /// a different TFM subfolder (e.g. <c>net10.0</c> vs <c>net10.0-windows7.0</c>). This
    /// method therefore enumerates every <c>tools/&lt;tfm&gt;</c> and <c>lib/&lt;tfm&gt;</c>
    /// subdirectory under every path returned by <see cref="GetAllInstalledPaths"/> rather than
    /// filtering by <see cref="SelectedFramework"/>. The resolved per-editor directory is stored
    /// in <c>_editorToDir</c> and consumed by <see cref="LocateMainExecutable"/>.
    /// </remarks>
    private void UpdateAvailableEditors()
    {
        AvailableEditors.Clear();
        _editorToDir.Clear();

        var ext = OperatingSystem.IsWindows() ? ".exe" : ".dll";
        foreach (var basePath in GetAllInstalledPaths())
        {
            foreach (var toplevelFolder in new[] { "tools", "lib" })
            {
                var topDir = Path.Combine(basePath, toplevelFolder);
                if (!Directory.Exists(topDir))
                    continue;

                foreach (var frameworkDir in Directory.EnumerateDirectories(topDir))
                {
                    foreach (var name in AllEditorNames())
                    {
                        // First discovery wins: don't overwrite an already-found editor.
                        if (!_editorToDir.ContainsKey(name) &&
                            File.Exists(Path.Combine(frameworkDir, $"{name}{ext}")))
                        {
                            AvailableEditors.Add(name);
                            _editorToDir[name] = frameworkDir;
                        }
                    }
                }
            }
        }

        UpdateSelectedEditor();
    }

    private static IEnumerable<string> AllEditorNames()
    {
        yield return GameStudioNames.StrideAvalonia;
        if (OperatingSystem.IsWindows())
            yield return GameStudioNames.Stride;
    }

    private void UpdateSelectedEditor()
    {
        var preferred = Launcher.Settings.PreferredEditor;
        if (AvailableEditors.Contains(preferred))
            SelectedEditor = preferred;
        else
            SelectedEditor = AvailableEditors.FirstOrDefault();
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

    public string? SelectedFramework
    {
        get => selectedFramework;
        set
        {
            if (SetValue(ref selectedFramework, value))
                UpdateAvailableEditors();
        }
    }

    /// <summary>
    /// Gets the editors available for the currently selected framework.
    /// Only populated when the version is installed locally.
    /// </summary>
    public ObservableList<string> AvailableEditors { get; } = [];

    /// <summary>
    /// Gets or sets the editor that will be launched for this version.
    /// Reflects and writes back to the global <c>PreferredEditor</c> setting.
    /// </summary>
    public string? SelectedEditor { get => selectedEditor; set => SetValue(ref selectedEditor, value); }

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

        // Use the pre-computed editor → directory map from UpdateAvailableEditors.
        // Each editor may live in a different package directory and a different TFM subfolder
        // (e.g. net10.0 for Avalonia, net10.0-windows7.0 for WPF), so we store the fully
        // resolved directory rather than re-applying SelectedFramework here.
        foreach (var gameStudioExecutable in GetPreferredExecutableNames())
        {
            var editorName = Path.GetFileNameWithoutExtension(gameStudioExecutable);
            if (_editorToDir.TryGetValue(editorName, out var dir))
            {
                var gameStudioPath = Path.Combine(dir, gameStudioExecutable);
                if (File.Exists(gameStudioPath))
                    return gameStudioPath;
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

    /// <summary>
    /// Returns the executable file names in preference order: the selected editor first, then the
    /// remaining editors in their default priority order.
    /// </summary>
    private IEnumerable<string> GetPreferredExecutableNames()
    {
        var ext = OperatingSystem.IsWindows() ? ".exe" : ".dll";
        string[] allNames = OperatingSystem.IsWindows()
            ? [GameStudioNames.StrideAvalonia, GameStudioNames.Stride, GameStudioNames.Xenko]
            : [GameStudioNames.StrideAvalonia];

        if (SelectedEditor is not null)
        {
            yield return $"{SelectedEditor}{ext}";
            foreach (var name in allNames.Where(n => n != SelectedEditor))
                yield return $"{name}{ext}";
        }
        else
        {
            foreach (var name in allNames)
                yield return $"{name}{ext}";
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
