// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Packages;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Launcher.Assets.Localization;
using Stride.Launcher.Services;

namespace Stride.Launcher.ViewModels;

/// <summary>
/// An implementation of the <see cref="PackageVersionViewModel"/> that represents a major version of Stride.
/// </summary>
public abstract class StrideVersionViewModel : PackageVersionViewModel, IComparable<StrideVersionViewModel>, IComparable<(int Major, int Minor, string Channel)>
{
    private bool isVisible;
    private bool canStart;
    private bool isActive;
    private string? selectedEditor;
    private bool updatingEditors;
    // Maps each discovered editor name to its fully-resolved directory (including TFM subfolder).
    // e.g. "Stride.GameStudio.Avalonia.Desktop" → ".../lib/net10.0"
    // Populated by UpdateAvailableEditors; consumed by LocateMainExecutable.
    private readonly Dictionary<string, string> _editorToDir = [];

    internal StrideVersionViewModel(MainViewModel launcher, NugetStore store, NugetLocalPackage? localPackage, string packageId, int major, int minor, string channel = "")
        : base(launcher, store, localPackage)
    {
        PackageSimpleName = packageId
            .Replace(".GameStudio", string.Empty)
            .Replace(".Avalonia.Desktop", string.Empty);
        Major = major;
        Minor = minor;
        Channel = channel;
        DependentProperties.Add(nameof(FullName), [nameof(StartName), nameof(InstallName), nameof(StartCaption), nameof(InstallCaption), nameof(DatedFullName)]);
        SetAsActiveCommand = new AnonymousCommand(ServiceProvider, () => launcher.ActiveVersion = this);
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

    /// <summary>The per-framework folders an editor of this version can live in.</summary>
    protected virtual IEnumerable<string> FrameworkDirectories() => GetAllInstalledPaths().SelectMany(PackageLayout.FrameworkDirectories);

    /// <summary>
    /// Updates the list of editors available across all installed package paths and restores
    /// the last preferred editor if it is still available.
    /// </summary>
    /// <remarks>
    /// Each editor (Avalonia, WPF) may live in a different NuGet package directory <em>and</em>
    /// a different TFM subfolder (e.g. <c>net10.0</c> vs <c>net10.0-windows7.0</c>). This
    /// method therefore enumerates every <c>tools/&lt;tfm&gt;</c> and <c>lib/&lt;tfm&gt;</c>
    /// subdirectory under every path returned by <see cref="GetAllInstalledPaths"/>; the first
    /// folder holding an editor wins. The resolved per-editor directory is stored
    /// in <c>_editorToDir</c> and consumed by <see cref="LocateMainExecutable"/>.
    /// </remarks>
    protected void UpdateAvailableEditors()
    {
        // The selector sets the editor while its items change: not a choice of the user
        updatingEditors = true;
        try
        {
            AvailableEditors.Clear();
            EditorChoices.Clear();
            _editorToDir.Clear();

            var ext = OperatingSystem.IsWindows() ? ".exe" : ".dll";
            foreach (var frameworkDir in FrameworkDirectories())
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

            // Every editor of this OS once this version has one, the missing ones greyed, so that it's clear which one
            // starts. Linux and macOS have only one, which hides the selector
            if (AvailableEditors.Count > 0)
            {
                foreach (var name in AllEditorNames())
                {
                    var isAvailable = AvailableEditors.Contains(name);
                    var label = EditorNameConverter.GetDisplayName(name);
                    EditorChoices.Add(new(name, isAvailable ? label : string.Format(MissingEditorFormat, label), isAvailable));
                }
            }
        }
        finally
        {
            updatingEditors = false;
        }

        UpdateSelectedEditor();
        // The runtime choices depend on which editor was found.
        if (Launcher.ActiveVersion == this)
            Launcher.RefreshRuntimes();
        // On non-Windows the Avalonia editor is the only option. Re-evaluate CanStart now
        // that AvailableEditors is populated (UpdateStatus runs before UpdateAvailableEditors).
        if (!OperatingSystem.IsWindows())
        {
            CanStart = CanDelete && AvailableEditors.Count > 0;
            if (Launcher.ActiveVersion == this)
                Launcher.StartStudioCommand.IsEnabled = CanStart;
        }
    }

    /// <summary>
    /// The editors of this OS, the default first: on Windows the WPF one, as in the CLI; elsewhere only the Avalonia one.
    /// </summary>
    private static IEnumerable<string> AllEditorNames()
    {
        if (OperatingSystem.IsWindows())
            yield return GameStudioNames.Stride;
        yield return GameStudioNames.StrideAvalonia;
    }

    /// <summary>
    /// Gets the entry of the selector for an editor this version doesn't have, where {0} is the name of the editor.
    /// </summary>
    protected virtual string MissingEditorFormat => Strings.EditorNotInVersion;

    /// <summary>
    /// Selects the editor the user last picked when this version has it, else its first one.
    /// </summary>
    internal void UpdateSelectedEditor()
    {
        var preferred = Launcher.Settings.PreferredEditor;
        // The selector may have lost its selection with its items: raised even when the same editor comes back
        selectedEditor = null;
        SetValue(ref selectedEditor, AvailableEditors.Contains(preferred) ? preferred : AllEditorNames().FirstOrDefault(AvailableEditors.Contains), nameof(SelectedEditor));
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
    /// The <see cref="Channel"/> of the nightly builds.
    /// </summary>
    public const string NightlyChannel = "nightly";

    /// <summary>
    /// Gets the builds of its major.minor this version holds (<see cref="ChannelOf"/>): an empty string for the releases and
    /// prereleases, <see cref="NightlyChannel"/> for the nightlies, or the tag of custom builds (mystudio), each a row of its own.
    /// </summary>
    public string Channel { get; }

    /// <summary>
    /// Gets whether this version holds the nightly builds of its major.minor (4.5.0-nightly.20261014.3), a row of their own
    /// next to the releases and prereleases of the same major.minor.
    /// </summary>
    public bool IsNightly => Channel == NightlyChannel;

    /// <summary>
    /// Gets whether this version holds the releases and prereleases of its major.minor, rather than its nightlies or custom builds.
    /// </summary>
    public bool IsReleaseChannel => Channel.Length == 0;

    /// <summary>
    /// Gets the badge of the custom builds this version holds (MYSTUDIO), or null for the other versions.
    /// </summary>
    public string? TagBadge => IsReleaseChannel || IsNightly ? null : TagBadgeOf(Channel);

    /// <summary>
    /// The most characters of a tag in its badge: a longer one is cut, its whole name stays in the version shown on hover.
    /// </summary>
    internal const int MaxTagBadgeLength = 12;

    /// <summary>
    /// Gets the badge of the custom builds of <paramref name="tag"/>: the tag in capitals, cut with an ellipsis past
    /// <see cref="MaxTagBadgeLength"/> characters (MYCOMPANYIN…).
    /// </summary>
    internal static string TagBadgeOf(string tag)
    {
        var badge = tag.ToUpperInvariant();
        return badge.Length <= MaxTagBadgeLength ? badge : $"{badge[..(MaxTagBadgeLength - 1)]}…";
    }

    /// <summary>
    /// Gets the name of this version.
    /// </summary>
    public override string Name => IsNightly ? $"{GetName(PackageSimpleName, Major, Minor)} Nightly"
        : IsReleaseChannel ? GetName(PackageSimpleName, Major, Minor)
        : $"{GetName(PackageSimpleName, Major, Minor)} {Channel}";

    /// <summary>
    /// Gets the display name of this version.
    /// </summary>
    public virtual string DisplayName => IsNightly ? string.Format(Strings.VersionNightly, ListName)
        : IsReleaseChannel ? ListName
        : $"{ListName} ({Channel})";

    /// <summary>
    /// Gets the name of this version in the version list, where a badge next to it tells a nightly or a local build.
    /// </summary>
    public virtual string ListName => GetName(PackageSimpleName, Major, Minor, true);

    /// <summary>
    /// Gets the name of this version on the Start button.
    /// </summary>
    public virtual string StartName => FullName;

    /// <summary>
    /// Gets the name of this version on the Install button: the product and the version (Stride 4.5.3).
    /// </summary>
    public string InstallName => $"{PackageSimpleName} {FullName}";

    /// <summary>
    /// Gets the text of the Start button, with the date of a nightly apart and the badge of a local build or a beta.
    /// </summary>
    public VersionCaption StartCaption => VersionCaption.Create(Strings.StartVersion, "", StartName, LocalPackage?.Version ?? ServerPackage?.Version)
        with { IsLocal = this is StrideDevVersionViewModel, IsBeta = IsBetaVersion(LocalPackage?.Version ?? ServerPackage?.Version), Tag = TagBadge };

    /// <summary>
    /// Gets the text of the Install button, with the date of a nightly apart and the badge of a beta.
    /// </summary>
    public VersionCaption InstallCaption => VersionCaption.Create(Strings.InstallVersion, $"{PackageSimpleName} ", InstallName, LocalPackage?.Version ?? ServerPackage?.Version)
        with { IsBeta = IsBetaVersion(LocalPackage?.Version ?? ServerPackage?.Version), Tag = TagBadge };

    /// <summary>
    /// Gets whether <paramref name="version"/> is a beta, shown with the BETA badge: a prerelease with the releases
    /// (<see cref="PackageVersion.IsPrerelease"/>: 4.5.0-beta06, 4.5.3-beta, an alpha, a preview or an rc too), neither a
    /// nightly, a custom build nor a local build.
    /// </summary>
    internal static bool IsBetaVersion(PackageVersion? version) => version?.IsPrerelease == true;

    /// <summary>
    /// Gets whether <paramref name="version"/> is a release: a version without a label (4.5.3), as in the CLI. A local build
    /// (4.5.4-dev4) isn't one, though it isn't a beta either.
    /// </summary>
    internal static bool IsReleaseVersion(PackageVersion? version) => version is not null && version.SpecialVersion.Length == 0;

    /// <summary>
    /// Gets the <see cref="Channel"/> of <paramref name="version"/>: <see cref="NightlyChannel"/> for a nightly; for a custom
    /// build its tag (<see cref="PackageVersion.CustomTag"/>: mystudio for 4.5.3-mystudio.32 and 4.5.3-mystudio.beta); else
    /// an empty string, with the releases: a prerelease (beta06, beta, rc1), or a local build, which has rows of its own.
    /// </summary>
    internal static string ChannelOf(PackageVersion? version)
        => version is null ? "" : version.IsNightly ? NightlyChannel : version.CustomTag ?? "";

    /// <summary>
    /// Gets the full name of a dated build (Stride 4.5.3-nightly.20261114.60), shown when hovering Start or Install, whose
    /// text has only its version and its date; null for any other version, which they show whole.
    /// </summary>
    public string? DatedFullName => StartCaption.IsDated ? InstallName : null;

    /// <summary>
    /// Gets the command that will set the associated package as active.
    /// </summary>
    public CommandBase SetAsActiveCommand { get; }

    /// <summary>
    /// Gets whether this version should be displayed.
    /// </summary>
    public bool IsVisible { get { return isVisible; } private set { SetValue(ref isVisible, value); } }

    /// <summary>
    /// Gets whether this version can be started.
    /// </summary>
    public bool CanStart { get { return canStart; } private set { SetValue(ref canStart, value); } }

    /// <summary>
    /// Gets whether this version is the active one, the one the Start button starts. Set by <see cref="MainViewModel.ActiveVersion"/>.
    /// </summary>
    public bool IsActive { get { return isActive; } internal set { SetValue(ref isActive, value); } }

    /// <summary>
    /// Gets the editors available for this version.
    /// Only populated when the version is installed locally.
    /// </summary>
    public ObservableList<string> AvailableEditors { get; } = [];

    /// <summary>
    /// Gets the entries of the Game Studio selector: every editor of this OS, those this version doesn't have greyed;
    /// empty when it has none.
    /// </summary>
    public ObservableList<EditorChoice> EditorChoices { get; } = [];

    /// <summary>
    /// Gets or sets the editor that will be launched for this version. Set by the selector, it is the user's choice:
    /// the <c>PreferredEditor</c> setting, that every version starts when it has it.
    /// </summary>
    public string? SelectedEditor
    {
        get => selectedEditor;
        set
        {
            // While its items or its version change, the selector sets null or an editor of its previous items: the choice stays
            if (value is null || !AvailableEditors.Contains(value) || updatingEditors || Launcher.IsSwitchingVersion)
                return;
            if (SetValue(ref selectedEditor, value))
            {
                Launcher.PreferredEditor = value;
                // The runtimes offered are the ones of the selected editor (only the WPF Game Studio takes one)
                if (Launcher.ActiveVersion == this)
                    Launcher.RefreshRuntimes();
            }
        }
    }

    /// <summary>
    /// Builds a string that represents the given version numbers.
    /// </summary>
    /// <param name="majorVersion">The major version number.</param>
    /// <param name="minorVersion">The minor version number.</param>
    /// <param name="isDisplayName">Indicates whether the name to compute is a display name, or a string token used to build urls.</param>
    /// <returns>A string representing the given version numbers.</returns>
    public static string GetName(string packageSimpleName, int majorVersion, int minorVersion, bool isDisplayName = false)
    {
        return $"{packageSimpleName} {majorVersion}.{minorVersion}";
    }

    /// <inheritdoc/>
    protected override void UpdateStatus()
    {
        base.UpdateStatus();
        IsVisible = true;
        // A version not installed can be selected too, as from its alternate versions: the top button then installs it
        // (two steps, so that a misclick doesn't start a download)
        SetAsActiveCommand.IsEnabled = CanDelete || CanBeDownloaded;
        DeleteCommand.IsEnabled = CanDelete;
        // On non-Windows only the Avalonia editor is supported; require it to be present before allowing Start.
        CanStart = CanDelete && (OperatingSystem.IsWindows() || AvailableEditors.Count > 0);

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
        // resolved directory.
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
        var allNames = AllEditorNames();

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
        => other is null ? -1 : Compare((Major, Minor, Channel), (other.Major, other.Minor, other.Channel));

    public int CompareTo((int Major, int Minor, string Channel) other) => Compare((Major, Minor, Channel), other);

    /// <summary>
    /// Orders the rows, newest first: newest major.minor first, then within a major.minor its rows of custom builds by tag
    /// (a studio's own builds, the ones it starts most), its nightly row (its nightlies are newer than its releases and
    /// prereleases), and its release row.
    /// </summary>
    internal static int Compare((int Major, int Minor, string Channel) x, (int Major, int Minor, string Channel) y)
    {
        var r = x.Major.CompareTo(y.Major);
        if (r != 0)
            return -r;
        r = x.Minor.CompareTo(y.Minor);
        if (r != 0)
            return -r;
        r = Rank(x.Channel).CompareTo(Rank(y.Channel));
        return r != 0 ? r : string.CompareOrdinal(x.Channel, y.Channel);

        static int Rank(string channel) => channel.Length == 0 ? 2 : channel == NightlyChannel ? 1 : 0;
    }
}
