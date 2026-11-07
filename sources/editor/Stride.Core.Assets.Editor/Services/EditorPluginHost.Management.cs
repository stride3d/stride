// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Core.Assets.Editor.Settings;
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
    /// Where the Editor packages switched off per session are kept, or null when nothing is switched off (tests, other hosts).
    /// The editors set the user's store (<see cref="DisabledEditorCompanionStore.CreateDefault"/>).
    /// </summary>
    [Experimental("STREXP002")]
    public DisabledEditorCompanionStore? DisabledCompanions { get; set; }

    /// <summary>
    /// Creates the filter of the Editor companions a session loads: none of those switched off for that session, only the
    /// built-in ones in safe mode. Each one left out gets the <see cref="PluginState.Off"/> state.
    /// </summary>
    /// <param name="sessionPath">The session's file, or null for a session that has none yet (a new project).</param>
    [Experimental("STREXP002")]
    public ICompanionPackageFilter GetCompanionPackageFilter(UFile? sessionPath)
    {
        var switchedOff = sessionPath is not null && DisabledCompanions is { } store ? store.Get(sessionPath) : [];
        return new EditorCompanionFilter(switchedOff, SafeMode, RecordOff);
    }

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
    private void RecordOff(string packageName, PluginOffReason reason)
    {
        PluginStatus status;
        lock (statusLock)
        {
            if (unloadedStatuses.TryGetValue(packageName, out var existing) && existing.Entry.State == PluginState.Off && existing.Entry.OffReason == reason)
                return;
            var entry = new StatusEntry { State = PluginState.Off, OffReason = reason };
            unloadedStatuses[packageName] = (packageName, entry);
            status = entry.Snapshot(packageName);
        }
        PluginStatusChanged?.Invoke(this, new PluginStatusChangedEventArgs(null, status.Name, status));
    }
}
