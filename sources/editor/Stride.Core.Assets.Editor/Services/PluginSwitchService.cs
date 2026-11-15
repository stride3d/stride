// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.IO;
using Stride.Core.Translation;

#nullable enable

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// Switches a third-party Editor package off or on for a session, from its next opening (a restart in the cross-platform
/// Game Studio, the next opening in the WPF one), and says what goes, for the confirmation. A built-in package can't be
/// switched off (<see cref="BuiltInPlugins"/>).
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
    /// <exception cref="InvalidOperationException">The package is built into the editor.</exception>
    public bool SwitchOff(UFile sessionPath, PluginInfo plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        if (plugin.IsExtension)
            throw new InvalidOperationException($"[{plugin.Id}] is an extension: it is switched off everywhere, with EditorExtensionService.SetOff.");
        if (!plugin.CanSwitchOff || BuiltInPlugins.IsBuiltIn(plugin.Id, plugin.Version))
            throw new InvalidOperationException($"[{plugin.Id}] is built into the editor and can't be switched off.");
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
    /// </summary>
    public static SwitchOffSummary DescribeSwitchOff(PluginInfo plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);
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
}

/// <summary>
/// What switching a plugin off takes away (<see cref="PluginSwitchService.DescribeSwitchOff"/>).
/// </summary>
/// <param name="PackageName">The package switched off.</param>
/// <param name="WhatGoes">What goes, one item per line.</param>
/// <param name="Message">The text of the confirmation.</param>
[Experimental("STREXP002")]
public sealed record SwitchOffSummary(string PackageName, IReadOnlyList<string> WhatGoes, string Message);
