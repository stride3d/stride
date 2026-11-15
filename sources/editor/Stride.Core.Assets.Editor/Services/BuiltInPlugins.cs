// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// The Editor packages of the engine's own plugins (Audio, Physics, Particles, SpriteStudio, UI, Video, Voxels): built into
/// the editor, they can't be switched off and stay on in safe mode.
/// </summary>
/// <remarks>
/// A package is built-in by its name and its version, not by a name prefix: the list is compiled into the editor, and a
/// package of the list counts only at the editor's major and minor version (a game made with another Stride version brings
/// the engine's plugins of that version, which this editor did not ship).
/// </remarks>
[Experimental("STREXP002")]
public static class BuiltInPlugins
{
    /// <summary>
    /// The names of the engine's Editor packages, the toolkit companions included.
    /// </summary>
    public static IReadOnlyCollection<string> PackageNames { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Stride.Audio.Editor",
        "Stride.Audio.Editor.Avalonia",
        "Stride.Audio.Editor.Wpf",
        "Stride.Particles.Editor",
        "Stride.Physics.Editor",
        "Stride.SpriteStudio.Editor",
        "Stride.SpriteStudio.Editor.Avalonia",
        "Stride.SpriteStudio.Editor.Wpf",
        "Stride.UI.Editor",
        "Stride.UI.Editor.Avalonia",
        "Stride.UI.Editor.Wpf",
        "Stride.Video.Editor",
        "Stride.Voxels.Editor",
    };

    /// <summary>
    /// The major and minor version of the running editor, at which the packages of <see cref="PackageNames"/> are built-in.
    /// </summary>
    public static Version EditorVersion { get; } = typeof(BuiltInPlugins).Assembly.GetName().Version is { } version ? new Version(version.Major, version.Minor) : new Version(0, 0);

    /// <summary>
    /// Whether a package is one of the engine's Editor packages at the editor's version.
    /// </summary>
    /// <param name="name">The name of the package.</param>
    /// <param name="version">Its version, or null when it is not known (a project of the solution): then it is not built-in.</param>
    public static bool IsBuiltIn(string? name, PackageVersion? version)
    {
        return name is not null && version is not null && PackageNames.Contains(name)
            && version.Version.Major == EditorVersion.Major && version.Version.Minor == EditorVersion.Minor;
    }
}
