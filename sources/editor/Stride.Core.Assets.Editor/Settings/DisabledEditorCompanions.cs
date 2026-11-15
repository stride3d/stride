// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Core.IO;
using Stride.Core.Settings;

namespace Stride.Core.Assets.Editor.Settings;

/// <summary>
/// The Editor packages a user switched off for one session, under <c>Internal/DisabledEditorCompanions</c>.
/// </summary>
[DataContract("DisabledEditorCompanions")]
[Experimental("STREXP002")]
public sealed class DisabledEditorCompanions
{
    /// <summary>
    /// The path of the session (solution or package file).
    /// </summary>
    [DataMember(10)]
    public UFile? FilePath { get; set; }

    /// <summary>
    /// The names of the Editor packages switched off for it.
    /// </summary>
    [DataMember(20)]
    public List<string> Packages { get; set; } = [];
}

/// <summary>
/// The Editor packages switched off per session, kept per user in the internal settings that both editors read
/// (<see cref="InternalSettings.DisabledEditorCompanions"/>). Each change reads the file first and writes it at once, as
/// <see cref="Panels.SessionPanelLayoutStore"/> does, so that several editors running at the same time keep each other's entries.
/// </summary>
[Experimental("STREXP002")]
public sealed class DisabledEditorCompanionStore
{
    private static readonly Lock SyncRoot = new();

    private readonly Func<SettingsProfile> loadLatestProfile;
    private readonly SettingsKey<List<DisabledEditorCompanions>> settingsKey;
    private readonly Action save;

    /// <param name="loadLatestProfile">Loads a copy of the internal settings from the file.</param>
    /// <param name="settingsKey">The key of the entries in the internal settings.</param>
    /// <param name="save">Writes the internal settings to the file.</param>
    public DisabledEditorCompanionStore(Func<SettingsProfile> loadLatestProfile, SettingsKey<List<DisabledEditorCompanions>> settingsKey, Action save)
    {
        this.loadLatestProfile = loadLatestProfile ?? throw new ArgumentNullException(nameof(loadLatestProfile));
        this.settingsKey = settingsKey ?? throw new ArgumentNullException(nameof(settingsKey));
        this.save = save ?? throw new ArgumentNullException(nameof(save));
    }

    /// <summary>
    /// The store of the editor: the internal settings file of the user.
    /// </summary>
    public static DisabledEditorCompanionStore CreateDefault()
    {
        return new DisabledEditorCompanionStore(InternalSettings.LoadProfileCopy, InternalSettings.DisabledEditorCompanions, InternalSettings.SaveProfile);
    }

    /// <summary>
    /// The names of the Editor packages switched off for a session.
    /// </summary>
    public IReadOnlyList<string> Get(UFile sessionPath)
    {
        ArgumentNullException.ThrowIfNull(sessionPath);
        lock (SyncRoot)
        {
            return Find(LoadList(), sessionPath)?.Packages.ToList() ?? [];
        }
    }

    /// <summary>
    /// Switches an Editor package off or on for a session, from its next opening.
    /// </summary>
    /// <returns>True if the entry changed; false if it was already so.</returns>
    public bool Set(UFile sessionPath, string packageName, bool switchedOff)
    {
        ArgumentNullException.ThrowIfNull(sessionPath);
        ArgumentException.ThrowIfNullOrEmpty(packageName);
        lock (SyncRoot)
        {
            var list = LoadList();
            var data = Find(list, sessionPath);
            var present = data?.Packages.Contains(packageName, StringComparer.OrdinalIgnoreCase) == true;
            if (present == switchedOff)
                return false;

            if (switchedOff)
            {
                if (data is null)
                    list.Add(data = new DisabledEditorCompanions { FilePath = sessionPath });
                data.Packages.Add(packageName);
            }
            else
            {
                data!.Packages.RemoveAll(x => string.Equals(x, packageName, StringComparison.OrdinalIgnoreCase));
                if (data.Packages.Count == 0)
                    list.Remove(data);
            }
            settingsKey.SetValue(list);
            save();
            return true;
        }
    }

    // A copy of the file's list
    private List<DisabledEditorCompanions> LoadList()
    {
        var list = settingsKey.GetValue(loadLatestProfile(), true) ?? [];
        return list.Where(x => x?.FilePath is not null).ToList();
    }

    private static DisabledEditorCompanions? Find(List<DisabledEditorCompanions> list, UFile sessionPath)
    {
        return list.FirstOrDefault(x => string.Equals(x.FilePath, sessionPath, StringComparison.OrdinalIgnoreCase));
    }
}
