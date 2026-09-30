// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Launcher.Assets.Localization;

namespace Stride.Launcher.ViewModels;

/// <summary>
/// A version a recent project can be opened with: a version of the list, or one of its installed builds.
/// </summary>
public sealed class OpenWithOption
{
    internal OpenWithOption(StrideVersionViewModel version, StrideStoreAlternateVersionViewModel? build, PackageVersion? projectVersion)
    {
        Version = version;
        Build = build;
        TargetVersion = build?.Version ?? version.InstalledVersion;
        IsUpgrade = RecentProjectViewModel.IsUpgrade(projectVersion, TargetVersion);
        Name = build is not null ? $"{version.PackageSimpleName} {build.Version}" : version.DisplayName;
    }

    /// <summary>
    /// Gets the version of the list this option opens the project with.
    /// </summary>
    public StrideVersionViewModel Version { get; }

    /// <summary>
    /// Gets the installed build of <see cref="Version"/> to select first, if not the one it has.
    /// </summary>
    public StrideStoreAlternateVersionViewModel? Build { get; }

    /// <summary>
    /// Gets the version of the build the project is opened with, if installed.
    /// </summary>
    public PackageVersion? TargetVersion { get; }

    /// <summary>
    /// Gets whether opening with this option upgrades the project, rather than switch it to or from a local build.
    /// </summary>
    public bool IsUpgrade { get; }

    public string Name { get; }

    public string Text => string.Format(IsUpgrade ? Strings.OpenProjectWithVersionUpgrade : Strings.OpenProjectWithVersion, Name);
}
