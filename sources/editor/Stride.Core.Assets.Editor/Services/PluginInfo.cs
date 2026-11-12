// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// Where a plugin comes from.
/// </summary>
[Experimental("STREXP002")]
public enum PluginSource
{
    /// <summary>
    /// Built into the editor: the editor's own plugins, and the engine's plugin packages at the editor's version
    /// (<see cref="BuiltInPlugins"/>).
    /// </summary>
    BuiltIn,

    /// <summary>
    /// Part of the editor itself: one of its own assemblies with plugins (not a package). Built in too: it can't be switched
    /// off.
    /// </summary>
    Editor,

    /// <summary>
    /// A package restored from a feed, at the version the game's package declares.
    /// </summary>
    Package,

    /// <summary>
    /// A project of the game's solution.
    /// </summary>
    SolutionProject,

    /// <summary>
    /// A dev-redirect package, loaded from its source tree (a development build of the package).
    /// </summary>
    DevRedirect,

    /// <summary>
    /// An extension the user installed (<see cref="EditorExtensionService"/>): it loads with every session.
    /// </summary>
    Extension,
}

/// <summary>
/// What the editor knows of one plugin package, for the Plugins panel: its identity and description from the restored
/// <c>.nuspec</c>, where it comes from, the editor version it was built for, what it contributes, and its state. Read-only:
/// a snapshot taken by <see cref="PluginCatalog.Collect"/>.
/// </summary>
/// <remarks>
/// One package can hold several plugins (several <see cref="AssetsPlugin"/> types, a toolkit companion next to its neutral
/// package); their statuses are in <see cref="Statuses"/>, and the package's state is the worst of them.
/// </remarks>
[Experimental("STREXP002")]
public sealed class PluginInfo
{
    private readonly bool canSwitchOff = true;

    /// <summary>
    /// The package id (or the assembly name of one of the editor's own plugins).
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The package version, or null when it is not known (a project of the solution without a version).
    /// </summary>
    public PackageVersion? Version { get; init; }

    /// <summary>
    /// The name to show: the <c>.nuspec</c> title, else the id; the project's name for a project of the solution.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// The authors, from the <c>.nuspec</c>.
    /// </summary>
    public string? Authors { get; init; }

    /// <summary>
    /// The description, from the <c>.nuspec</c>.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The full path of the package's icon file, when the <c>.nuspec</c> names one that exists.
    /// </summary>
    public string? IconPath { get; init; }

    /// <summary>
    /// The project's web page, from the <c>.nuspec</c>.
    /// </summary>
    public string? ProjectUrl { get; init; }

    /// <summary>
    /// The license: its SPDX expression, the name of its file in the package, or its URL, from the <c>.nuspec</c>.
    /// </summary>
    public string? License { get; init; }

    /// <summary>
    /// What the package carries (<see cref="PackageKind.Editor"/> for a plugin).
    /// </summary>
    public PackageKind Kind { get; init; } = PackageKind.Editor;

    /// <summary>
    /// The UI toolkit of its views (Wpf or Avalonia), or null for a toolkit-neutral package.
    /// </summary>
    public string? Toolkit { get; init; }

    /// <summary>
    /// Where it comes from.
    /// </summary>
    public PluginSource Source { get; init; }

    /// <summary>
    /// Whether it is built into the editor (an engine plugin package at the editor's version, or part of the editor itself):
    /// it loads in safe mode, and only the engine's Editor packages can be switched off (<see cref="IsSwitchableBuiltIn"/>).
    /// </summary>
    public bool IsBuiltIn => Source is PluginSource.BuiltIn or PluginSource.Editor;

    /// <summary>
    /// Whether it is a built-in package the user can switch off for a project (<see cref="BuiltInPlugins.SwitchablePackageNames"/>):
    /// the Editor package of an engine plugin, which takes its toolkit companions with it.
    /// </summary>
    public bool IsSwitchableBuiltIn => Source == PluginSource.BuiltIn && BuiltInPlugins.SwitchablePackageNames.Contains(Id);

    /// <summary>
    /// Whether it is an extension the user installed: it is switched off everywhere (<see cref="EditorExtensionService.SetOff"/>),
    /// not per session.
    /// </summary>
    public bool IsExtension => Source == PluginSource.Extension;

    /// <summary>
    /// Whether the user can switch it off for the session (<see cref="PluginSwitchService"/>): a built-in one only when it is
    /// the Editor package of an engine plugin (<see cref="IsSwitchableBuiltIn"/>), never one known only by the name of an
    /// assembly (not by its package), nor an extension (switched off everywhere instead).
    /// </summary>
    public bool CanSwitchOff
    {
        get => (!IsBuiltIn || IsSwitchableBuiltIn) && !IsExtension && canSwitchOff;
        init => canSwitchOff = value;
    }

    /// <summary>
    /// The major and minor version of the editor its assemblies were built against (the highest editor assembly they
    /// reference, <see cref="EditorApiCheck.GetReferencedEditorVersion"/>), or null when none was read (not loaded, or no
    /// editor reference).
    /// </summary>
    public Version? EditorApiVersion { get; init; }

    /// <summary>
    /// What its plugins registered and still have in place, counted by kind: the member of the plugin context it came
    /// through (<c>AssetEditorViewModelTypes</c>, <c>Panels</c>, <c>Commands</c>...) or the type of a registry of another
    /// layer (<c>SceneViewToolDescriptor</c>...).
    /// </summary>
    public IReadOnlyDictionary<string, int> Contributions { get; init; } = new Dictionary<string, int>();

    /// <summary>
    /// The state of the package: <see cref="PluginState.Off"/> or <see cref="PluginState.Incompatible"/> when it was not
    /// loaded, <see cref="PluginState.Failed"/> when one of its plugins could not join, else <see cref="PluginState.Joined"/>.
    /// </summary>
    public PluginState State { get; init; }

    /// <summary>
    /// Why it is <see cref="PluginState.Off"/>, or null.
    /// </summary>
    public PluginOffReason? OffReason { get; init; }

    /// <summary>
    /// The statuses of its plugins (one per plugin type, or one for the package when it was not loaded).
    /// </summary>
    public IReadOnlyList<PluginStatus> Statuses { get; init; } = [];

    /// <summary>
    /// The errors of its plugins, oldest first.
    /// </summary>
    public IReadOnlyList<PluginError> Errors => Statuses.SelectMany(x => x.Errors).OrderBy(x => x.Time).ToList();

    /// <inheritdoc />
    public override string ToString() => $"{Id} {Version} ({Source}): {State}";
}
