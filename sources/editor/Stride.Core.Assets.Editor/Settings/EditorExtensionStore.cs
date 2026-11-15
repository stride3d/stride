// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Stride.Core.Settings;

namespace Stride.Core.Assets.Editor.Settings;

/// <summary>
/// An extension the user installed: an Editor package that loads with every session of the editors of one major.minor version.
/// </summary>
[DataContract("EditorExtension")]
[Experimental("STREXP002")]
public sealed class EditorExtension
{
    /// <summary>
    /// The package id.
    /// </summary>
    [DataMember(10)]
    public string? Id { get; set; }

    /// <summary>
    /// The package version.
    /// </summary>
    [DataMember(20)]
    public string? Version { get; set; }

    /// <summary>
    /// The UI toolkit of the package's views (Wpf or Avalonia), or null for a toolkit-neutral package: only the editor of that
    /// toolkit loads it.
    /// </summary>
    [DataMember(30)]
    [DefaultValue(null)]
    public string? Toolkit { get; set; }

    /// <summary>
    /// Whether the user switched it off: it stays installed, and no session loads it.
    /// </summary>
    [DataMember(40)]
    [DefaultValue(false)]
    public bool IsOff { get; set; }

    /// <summary>
    /// A copy of this entry.
    /// </summary>
    public EditorExtension Clone() => new() { Id = Id, Version = Version, Toolkit = Toolkit, IsOff = IsOff };

    /// <inheritdoc />
    public override string ToString() => $"{Id} {Version}{(Toolkit is not null ? $" ({Toolkit})" : "")}{(IsOff ? ", off" : "")}";
}

/// <summary>
/// The extensions installed for the editors of one major.minor version, under <c>Internal/EditorExtensions</c>.
/// </summary>
[DataContract("EditorExtensionList")]
[Experimental("STREXP002")]
public sealed class EditorExtensionList
{
    /// <summary>
    /// The major.minor version of the editors that load them (<c>4.5</c>).
    /// </summary>
    [DataMember(10)]
    public string? EditorVersion { get; set; }

    /// <summary>
    /// The extensions.
    /// </summary>
    [DataMember(20)]
    public List<EditorExtension> Packages { get; set; } = [];
}

/// <summary>
/// The extensions a user installed, per editor major.minor version, kept in the internal settings that both editors read
/// (<see cref="InternalSettings.ExtensionPackages"/>, key <c>Internal/EditorExtensions</c>). Each change reads the file first and
/// writes it at once, as <see cref="DisabledEditorCompanionStore"/> does, so that several editors running at the same time
/// keep each other's entries.
/// </summary>
[Experimental("STREXP002")]
public sealed class EditorExtensionStore
{
    private static readonly Lock SyncRoot = new();

    private readonly Func<SettingsProfile> loadLatestProfile;
    private readonly SettingsKey<List<EditorExtensionList>> settingsKey;
    private readonly Action save;

    /// <param name="loadLatestProfile">Loads a copy of the internal settings from the file.</param>
    /// <param name="settingsKey">The key of the lists in the internal settings.</param>
    /// <param name="save">Writes the internal settings to the file.</param>
    public EditorExtensionStore(Func<SettingsProfile> loadLatestProfile, SettingsKey<List<EditorExtensionList>> settingsKey, Action save)
    {
        this.loadLatestProfile = loadLatestProfile ?? throw new ArgumentNullException(nameof(loadLatestProfile));
        this.settingsKey = settingsKey ?? throw new ArgumentNullException(nameof(settingsKey));
        this.save = save ?? throw new ArgumentNullException(nameof(save));
    }

    /// <summary>
    /// The store of the editor: the internal settings file of the user.
    /// </summary>
    public static EditorExtensionStore CreateDefault()
    {
        return new EditorExtensionStore(InternalSettings.LoadProfileCopy, InternalSettings.ExtensionPackages, InternalSettings.SaveProfile);
    }

    /// <summary>
    /// The extensions installed for the editors of a major.minor version (copies).
    /// </summary>
    /// <param name="editorVersion">The editor's version; only its major and minor parts count.</param>
    public IReadOnlyList<EditorExtension> Get(Version editorVersion)
    {
        ArgumentNullException.ThrowIfNull(editorVersion);
        lock (SyncRoot)
        {
            return Find(LoadList(), editorVersion)?.Packages.Where(x => !string.IsNullOrEmpty(x?.Id)).Select(x => x.Clone()).ToList() ?? [];
        }
    }

    /// <summary>
    /// Adds an extension, or replaces the one with the same id.
    /// </summary>
    /// <returns>True if the list changed.</returns>
    public bool Set(Version editorVersion, EditorExtension extension)
    {
        ArgumentNullException.ThrowIfNull(editorVersion);
        ArgumentNullException.ThrowIfNull(extension);
        ArgumentException.ThrowIfNullOrEmpty(extension.Id);
        return Change(editorVersion, list =>
        {
            var index = list.Packages.FindIndex(x => string.Equals(x.Id, extension.Id, StringComparison.OrdinalIgnoreCase));
            if (index >= 0 && list.Packages[index] is { } existing && existing.Version == extension.Version && existing.Toolkit == extension.Toolkit && existing.IsOff == extension.IsOff)
                return false;
            if (index >= 0)
                list.Packages[index] = extension.Clone();
            else
                list.Packages.Add(extension.Clone());
            return true;
        });
    }

    /// <summary>
    /// Removes an extension.
    /// </summary>
    /// <returns>True if it was there.</returns>
    public bool Remove(Version editorVersion, string id)
    {
        ArgumentNullException.ThrowIfNull(editorVersion);
        ArgumentException.ThrowIfNullOrEmpty(id);
        return Change(editorVersion, list => list.Packages.RemoveAll(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase)) > 0);
    }

    /// <summary>
    /// Switches an extension off or on, from the next start of the editor.
    /// </summary>
    /// <returns>True if it changed; false if it was already so, or is not installed.</returns>
    public bool SetOff(Version editorVersion, string id, bool off)
    {
        ArgumentNullException.ThrowIfNull(editorVersion);
        ArgumentException.ThrowIfNullOrEmpty(id);
        return Change(editorVersion, list =>
        {
            if (list.Packages.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase)) is not { } extension || extension.IsOff == off)
                return false;
            extension.IsOff = off;
            return true;
        });
    }

    /// <summary>
    /// The key of an editor version in the lists: its major and minor parts.
    /// </summary>
    public static string GetVersionKey(Version editorVersion)
    {
        ArgumentNullException.ThrowIfNull(editorVersion);
        return $"{editorVersion.Major}.{editorVersion.Minor}";
    }

    private bool Change(Version editorVersion, Func<EditorExtensionList, bool> change)
    {
        lock (SyncRoot)
        {
            var lists = LoadList();
            var list = Find(lists, editorVersion);
            if (list is null)
                lists.Add(list = new EditorExtensionList { EditorVersion = GetVersionKey(editorVersion) });
            list.Packages.RemoveAll(x => string.IsNullOrEmpty(x?.Id));
            if (!change(list))
                return false;
            if (list.Packages.Count == 0)
                lists.Remove(list);
            settingsKey.SetValue(lists);
            save();
            return true;
        }
    }

    // A copy of the file's lists
    private List<EditorExtensionList> LoadList()
    {
        var lists = settingsKey.GetValue(loadLatestProfile(), true) ?? [];
        return lists.Where(x => x?.EditorVersion is not null).ToList();
    }

    private static EditorExtensionList? Find(List<EditorExtensionList> lists, Version editorVersion)
    {
        var key = GetVersionKey(editorVersion);
        return lists.FirstOrDefault(x => string.Equals(x.EditorVersion, key, StringComparison.Ordinal));
    }
}
