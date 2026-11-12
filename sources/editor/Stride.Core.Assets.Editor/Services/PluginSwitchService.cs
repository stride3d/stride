// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.IO;
using Stride.Core.Translation;

#nullable enable

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// Switches an Editor package off or on for a session, from its next opening (a restart in the cross-platform Game Studio, the
/// next opening in the WPF one), and says what goes, for the confirmation. A third-party package can be switched off, and
/// among the built-in ones the Editor package of each engine plugin (<see cref="BuiltInPlugins.SwitchablePackageNames"/>).
/// </summary>
[Experimental("STREXP002")]
public sealed class PluginSwitchService
{
    private readonly DisabledEditorCompanionStore store;

    /// <param name="store">Where the switched-off packages are kept.</param>
    public PluginSwitchService(DisabledEditorCompanionStore store)
    {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
    }

    /// <summary>
    /// The packages switched off for a session.
    /// </summary>
    public IReadOnlyList<string> GetSwitchedOff(UFile sessionPath) => store.Get(sessionPath);

    /// <summary>
    /// Whether a package is switched off for a session.
    /// </summary>
    public bool IsSwitchedOff(UFile sessionPath, string packageName)
    {
        ArgumentException.ThrowIfNullOrEmpty(packageName);
        return store.Get(sessionPath).Contains(packageName, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Switches a package off for a session, from its next opening.
    /// </summary>
    /// <returns>True if it was switched off now; false if it was already.</returns>
    /// <exception cref="InvalidOperationException">The package is built into the editor and is not one that can be switched off.</exception>
    public bool SwitchOff(UFile sessionPath, PluginInfo plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        if (plugin.IsExtension)
            throw new InvalidOperationException($"[{plugin.Id}] is an extension: it is switched off everywhere, with EditorExtensionService.SetOff.");
        if (!plugin.CanSwitchOff || (BuiltInPlugins.IsBuiltIn(plugin.Id, plugin.Version) && !BuiltInPlugins.IsSwitchable(plugin.Id, plugin.Version)))
        {
            var switchedWith = BuiltInPlugins.GetSwitchedWith(plugin.Id);
            throw new InvalidOperationException(switchedWith is not null && !string.Equals(switchedWith, plugin.Id, StringComparison.OrdinalIgnoreCase)
                ? $"[{plugin.Id}] is built into the editor and is switched off with [{switchedWith}], not on its own."
                : $"[{plugin.Id}] is built into the editor and can't be switched off.");
        }
        return store.Set(sessionPath, plugin.Id, switchedOff: true);
    }

    /// <summary>
    /// Switches a package on again for a session, from its next opening.
    /// </summary>
    /// <returns>True if it was switched on now; false if it was not switched off.</returns>
    public bool SwitchOn(UFile sessionPath, string packageName)
    {
        return store.Set(sessionPath, packageName, switchedOff: false);
    }

    /// <summary>
    /// What switching a package off takes away, for the confirmation: its editors, previews, gizmos, property grid additions
    /// and render features, and the other contributions its plugins registered. Its assets still open, with the generic view.
    /// For the Editor package of an engine plugin, what it takes away is known: one line each (<see cref="DescribeBuiltIn"/>).
    /// </summary>
    public static SwitchOffSummary DescribeSwitchOff(PluginInfo plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        if (plugin.IsSwitchableBuiltIn && DescribeBuiltIn(plugin.Id) is { Count: > 0 } builtInLines)
        {
            var builtInMessage = string.Format(Tr._p("Plugins", "Switching off {0} takes away, from the next opening of this project:"), plugin.DisplayName)
                + Environment.NewLine + string.Join(Environment.NewLine, builtInLines.Select(x => "- " + x))
                + Environment.NewLine + Environment.NewLine
                + Tr._p("Plugins", "Its assets and components stay: they build, work in the game and show in the scene editor as before. Its assets open with the generic property grid and no editor of their own.");
            return new SwitchOffSummary(plugin.Id, builtInLines, builtInMessage);
        }

        var counts = plugin.Contributions;
        int Count(params string[] kinds) => kinds.Sum(x => counts.GetValueOrDefault(x));

        // What every Editor package can bring, whether or not the host counts it (gizmos and render features are found by
        // their types, not registered through the context)
        var whatGoes = new List<string>
        {
            Counted(Tr._p("Plugins", "its asset editors"), Count(nameof(IEditorPluginContext.AssetEditorViewModelTypes))),
            Counted(Tr._p("Plugins", "its asset previews"), Count(nameof(IEditorPluginContext.AssetPreviewViewModelTypes))),
            Tr._p("Plugins", "its gizmos in the scene editor"),
            Counted(Tr._p("Plugins", "its additions to the property grid"), Count(nameof(IEditorPluginContext.TemplateProviders), nameof(ISessionPluginContext.NodePresenterCommands), nameof(ISessionPluginContext.NodePresenterUpdaters))),
            Tr._p("Plugins", "its render features: what its components draw may no longer show in the scene editor"),
        };
        if (Count(nameof(IEditorPluginContext.Panels)) is > 0 and var panels)
            whatGoes.Add(Counted(Tr._p("Plugins", "its panels"), panels));
        if (Count(nameof(IEditorPluginContext.Commands), nameof(IEditorPluginContext.ToolbarWidgets)) is > 0 and var commands)
            whatGoes.Add(Counted(Tr._p("Plugins", "its commands and toolbar entries"), commands));
        if (counts.Keys.Count(x => x.StartsWith("SceneView", StringComparison.Ordinal)) > 0)
            whatGoes.Add(Counted(Tr._p("Plugins", "its tools and widgets of the scene view"), counts.Where(x => x.Key.StartsWith("SceneView", StringComparison.Ordinal)).Sum(x => x.Value)));

        var message = string.Format(plugin.IsExtension
                ? Tr._p("Plugins", "Switching off {0} takes away, in every project, from the next opening of a project:")
                : Tr._p("Plugins", "Switching off {0} takes away, from the next opening of this project:"), plugin.DisplayName)
            + Environment.NewLine + string.Join(Environment.NewLine, whatGoes.Select(x => "- " + x))
            + Environment.NewLine + Environment.NewLine
            + Tr._p("Plugins", "Its assets still open, with the generic property grid and no editor of their own.");
        return new SwitchOffSummary(plugin.Id, whatGoes, message);

        static string Counted(string what, int count) => count > 0 ? $"{what} ({count})" : what;
    }

    /// <summary>
    /// What switching off the Editor package of an engine plugin takes away, one line each, or an empty list for another
    /// package. What shows the plugin's content (render features, the editor game's compilers) is in its Assets package and
    /// stays.
    /// </summary>
    /// <param name="packageName">The name of the package, one of <see cref="BuiltInPlugins.SwitchablePackageNames"/>.</param>
    public static IReadOnlyList<string> DescribeBuiltIn(string packageName)
    {
        ArgumentNullException.ThrowIfNull(packageName);
        return packageName.ToLowerInvariant() switch
        {
            "stride.audio.editor" =>
            [
                Tr._p("Plugins", "the sound preview"),
                Tr._p("Plugins", "the audio emitter and listener gizmos in the scene editor"),
                Tr._p("Plugins", "Add entity > Audio"),
                Tr._p("Plugins", "the icons of the audio components, and the sound thumbnail (a generic one instead)"),
            ],
            "stride.particles.editor" =>
            [
                Tr._p("Plugins", "Add entity > Particle system"),
                Tr._p("Plugins", "the particle system gizmo in the scene editor"),
                Tr._p("Plugins", "the position, rotation and scale toggles of particle initializers and updaters in the property grid"),
                Tr._p("Plugins", "the refresh of the scene editor when a particle material's blending changes in the property grid: the change shows once the scene reloads"),
                Tr._p("Plugins", "the icons of the particle system component and its play state"),
            ],
            "stride.physics.editor" =>
            [
                Tr._p("Plugins", "the collider shape and constraint gizmos in the scene editor"),
                Tr._p("Plugins", "the navigation mesh display of the scene editor, and Add entity > Navigation"),
                Tr._p("Plugins", "the heightmap preview and thumbnail"),
                Tr._p("Plugins", "the help of the property grid for physics and navigation (bone names, navigation groups)"),
                Tr._p("Plugins", "the icons of the physics components"),
                Tr._p("Plugins", "Simulation.DisableSimulation is no longer set in Game Studio: game code that reads it to know it runs in the editor sees false. Physics still doesn't run in the scene editor."),
            ],
            "stride.spritestudio.editor" =>
            [
                Tr._p("Plugins", "the SpriteStudio sheet preview, and the sheet and animation thumbnails"),
                Tr._p("Plugins", "dropping a SpriteStudio sheet on the scene to create its entity"),
                Tr._p("Plugins", "Add entity > 2D > SpriteStudio"),
                Tr._p("Plugins", "the node names offered in the property grid for a SpriteStudio node link"),
                Tr._p("Plugins", "the icons of the SpriteStudio components"),
            ],
            "stride.ui.editor" =>
            [
                Tr._p("Plugins", "the UI page and UI library editors"),
                Tr._p("Plugins", "the UI page thumbnails"),
                Tr._p("Plugins", "dropping a UI page on the scene to create its entity, and Add entity > 2D > UI"),
                Tr._p("Plugins", "the UI gizmo in the scene editor"),
                Tr._p("Plugins", "the help of the property grid for UI elements"),
                Tr._p("Plugins", "the icons of the UI components"),
            ],
            "stride.video.editor" =>
            [
                Tr._p("Plugins", "dropping a video on the scene to create its entity"),
                Tr._p("Plugins", "the pixel width and height fields of a video in the property grid (its size shows as plain numbers instead)"),
                Tr._p("Plugins", "the icon of the video component, and the video thumbnail (a generic one instead)"),
            ],
            "stride.voxels.editor" =>
            [
                Tr._p("Plugins", "Add entity > Light > Voxel light and Voxel volume"),
                Tr._p("Plugins", "the voxel volume gizmo in the scene editor"),
            ],
            _ => [],
        };
    }
}

/// <summary>
/// What switching a plugin off takes away (<see cref="PluginSwitchService.DescribeSwitchOff"/>).
/// </summary>
/// <param name="PackageName">The package switched off.</param>
/// <param name="WhatGoes">What goes, one item per line.</param>
/// <param name="Message">The text of the confirmation.</param>
[Experimental("STREXP002")]
public sealed record SwitchOffSummary(string PackageName, IReadOnlyList<string> WhatGoes, string Message);
