// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Diagnostics;
using Stride.Core.IO;

#nullable enable

namespace Stride.Core.Assets.Editor.Services;

public abstract partial class EditorPluginHost
{
    /// <summary>
    /// Whether the editor runs in safe mode (<c>--safe-mode</c>): the sessions it opens load only the built-in Editor packages
    /// (<see cref="BuiltInPlugins"/>), and the others are <see cref="PluginState.Off"/> with <see cref="PluginOffReason.SafeMode"/>.
    /// Set by the editor before it opens a session.
    /// </summary>
    [Experimental("STREXP002")]
    public bool SafeMode { get; set; }

    /// <summary>
    /// The marker written while the plugins join a session and removed once it has opened (<see cref="InitializeSession"/>),
    /// or null to write none (tests, other hosts). The editors set the user's (<see cref="PluginLoadingMarker.CreateDefault"/>).
    /// </summary>
    [Experimental("STREXP002")]
    public PluginLoadingMarker? LoadingMarker { get; set; }

    /// <summary>
    /// Where the Editor packages switched off per session are kept, or null when nothing is switched off (tests, other hosts).
    /// The editors set the user's store (<see cref="DisabledEditorCompanionStore.CreateDefault"/>).
    /// </summary>
    [Experimental("STREXP002")]
    public DisabledEditorCompanionStore? DisabledCompanions { get; set; }

    /// <summary>
    /// The extensions of the user, which every session the editor opens loads, or null for none (tests, other hosts). The
    /// editors set the user's (<see cref="EditorExtensionService.CreateDefault"/>).
    /// </summary>
    [Experimental("STREXP002")]
    public EditorExtensionService? Extensions { get; set; }

    /// <summary>
    /// Creates the filter of the Editor companions a session loads: none of those switched off for that session, nor the
    /// extensions switched off, only the built-in ones in safe mode. Each one left out gets the <see cref="PluginState.Off"/> state.
    /// </summary>
    /// <param name="sessionPath">The session's file, or null for a session that has none yet (a new project).</param>
    [Experimental("STREXP002")]
    public ICompanionPackageFilter GetCompanionPackageFilter(UFile? sessionPath)
    {
        var switchedOff = sessionPath is not null && DisabledCompanions is { } store ? store.Get(sessionPath) : [];
        return companionFilter = new EditorCompanionFilter(switchedOff, SafeMode, RecordOff, Extensions?.GetSwitchedOff());
    }

    // The filter of the session being opened: a plugin class of a package it leaves out stays out, however it was loaded
    private EditorCompanionFilter? companionFilter;

    /// <summary>
    /// Whether an assembly is part of the editor itself (its plugins always join): by default, one loaded in the default load
    /// context, where the packages of a session never load. Tests replace it.
    /// </summary>
    internal Func<Assembly, bool> IsEditorAssembly { get; set; } = assembly => AssemblyLoadContext.GetLoadContext(assembly) == AssemblyLoadContext.Default;

    // The filter only sees the Editor companions: a plugin class can also come in another assembly of the session (the game's
    // own, a package it references, a package switched off but still reached as a NuGet dependency). In safe mode only the
    // editor's own plugins and those of the built-in packages join; otherwise the plugins of a package switched off don't.
    private bool IsLeftOut(AssetsPlugin plugin)
    {
        var assembly = plugin.GetType().Assembly;
        if (assembly.GetName() is not { Name: { } name } assemblyName || IsEditorAssembly(assembly))
            return false;
        var version = assemblyName.Version is { } v ? new PackageVersion(new Version(v.Major, v.Minor, Math.Max(v.Build, 0))) : null;
        if (BuiltInPlugins.IsBuiltIn(name, version))
            return false;

        PluginOffReason reason;
        if (SafeMode)
            reason = PluginOffReason.SafeMode;
        else if (companionFilter is { } filter && (filter.SwitchedOff.Contains(name) || filter.SwitchedOffExtensions.Contains(name)))
            reason = PluginOffReason.SwitchedOff;
        else
            return false;

        if (RecordOffOnce(name, reason))
        {
            var why = reason == PluginOffReason.SafeMode ? "Game Studio runs in safe mode" : "its package is switched off";
            PluginsLogger.Info($"The plugin [{plugin.GetType().FullName}] of [{name}] does not join the session: {why}.");
        }
        return true;
    }

    /// <summary>
    /// What a session adds to the companions its packages declare: the extensions of the user (<see cref="Extensions"/>), or
    /// null when there are none.
    /// </summary>
    [Experimental("STREXP002")]
    public ICompanionPackageSource? GetCompanionPackageSource() => Extensions?.CreateSource();

    /// <summary>
    /// What a plugin registered and still has in place, counted by kind of contribution: the member of the plugin context it
    /// came through (<c>AssetEditorViewModelTypes</c>, <c>Panels</c>, <c>Commands</c>...), or the type of a registry that a
    /// layer of the editor declared (<see cref="DeclareRegistry{T}"/>).
    /// </summary>
    [Experimental("STREXP002")]
    public IReadOnlyDictionary<string, int> GetContributionCounts(AssetsPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        return records.TryGetValue(plugin, out var record) ? record.CountByKind() : new Dictionary<string, int>();
    }

    // A package left out by the session's filter: nothing of it was loaded, so it is known by its package name. Called on the
    // thread that loads the session, before the plugins join it.
    private void RecordOff(string packageName, PluginOffReason reason) => RecordOffOnce(packageName, reason);

    // False when it was already recorded with that reason
    private bool RecordOffOnce(string packageName, PluginOffReason reason)
    {
        PluginStatus status;
        lock (statusLock)
        {
            if (unloadedStatuses.TryGetValue(packageName, out var existing) && existing.Entry.State == PluginState.Off && existing.Entry.OffReason == reason)
                return false;
            var entry = new StatusEntry { State = PluginState.Off, OffReason = reason };
            unloadedStatuses[packageName] = (packageName, entry);
            status = entry.Snapshot(packageName);
        }
        PluginStatusChanged?.Invoke(this, new PluginStatusChangedEventArgs(null, status.Name, status));
        return true;
    }
}
