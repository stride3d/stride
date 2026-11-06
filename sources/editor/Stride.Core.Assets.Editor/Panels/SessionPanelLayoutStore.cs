// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.IO;
using Stride.Core.Settings;

namespace Stride.Core.Assets.Editor.Panels;

/// <summary>
/// The two panel layouts of a recent session.
/// </summary>
[DataContract("SessionPanelLayouts")]
public sealed class SessionPanelLayouts
{
    /// <summary>
    /// The path of the session.
    /// </summary>
    [DataMember(10)]
    public UFile? FilePath { get; set; }

    /// <summary>
    /// When the layouts were last saved, in UTC ticks: only the most recent sessions are kept.
    /// </summary>
    [DataMember(20)]
    public long Timestamp { get; set; }

    /// <summary>
    /// The layout while no editor is open.
    /// </summary>
    [DataMember(30)]
    [DefaultValue(null)]
    public PanelLayout? Normal { get; set; }

    /// <summary>
    /// The layout while editors are open.
    /// </summary>
    [DataMember(40)]
    [DefaultValue(null)]
    public PanelLayout? Editors { get; set; }

    /// <summary>
    /// The layout of the given kind.
    /// </summary>
    public PanelLayout? Get(PanelLayoutKind kind) => kind == PanelLayoutKind.Normal ? Normal : Editors;

    /// <summary>
    /// Sets the layout of the given kind.
    /// </summary>
    public void Set(PanelLayoutKind kind, PanelLayout? layout)
    {
        if (kind == PanelLayoutKind.Normal)
            Normal = layout;
        else
            Editors = layout;
    }
}

/// <summary>
/// The panel layouts of the most recent sessions, kept in the internal settings under a key of their own. Each change reads
/// the file first and writes it at once, as <see cref="MRUAdditionalDataCollection"/> does, so that several editors running
/// at the same time keep each other's sessions.
/// </summary>
public sealed class SessionPanelLayoutStore
{
    /// <summary>
    /// The number of sessions whose layouts are kept, as for the other data of the recent sessions.
    /// </summary>
    public const int MaxSessionCount = MRUAdditionalDataCollection.MaxMRUCount;

    private static readonly Lock SyncRoot = new();

    private readonly Func<SettingsProfile> loadLatestProfile;
    private readonly SettingsKey<List<SessionPanelLayouts>> settingsKey;
    private readonly Action save;

    /// <param name="loadLatestProfile">Loads a copy of the internal settings from the file.</param>
    /// <param name="settingsKey">The key of the layouts in the internal settings.</param>
    /// <param name="save">Writes the internal settings to the file.</param>
    public SessionPanelLayoutStore(Func<SettingsProfile> loadLatestProfile, SettingsKey<List<SessionPanelLayouts>> settingsKey, Action save)
    {
        this.loadLatestProfile = loadLatestProfile ?? throw new ArgumentNullException(nameof(loadLatestProfile));
        this.settingsKey = settingsKey ?? throw new ArgumentNullException(nameof(settingsKey));
        this.save = save ?? throw new ArgumentNullException(nameof(save));
    }

    /// <summary>
    /// The saved layout of a session, or null.
    /// </summary>
    public PanelLayout? Load(UFile sessionPath, PanelLayoutKind kind)
    {
        ArgumentNullException.ThrowIfNull(sessionPath);
        lock (SyncRoot)
        {
            return Find(LoadList(), sessionPath)?.Get(kind);
        }
    }

    /// <summary>
    /// Saves a layout of a session, which becomes the most recent one; the oldest sessions beyond
    /// <see cref="MaxSessionCount"/> lose their layouts.
    /// </summary>
    public void Save(UFile sessionPath, PanelLayoutKind kind, PanelLayout layout)
    {
        ArgumentNullException.ThrowIfNull(sessionPath);
        ArgumentNullException.ThrowIfNull(layout);
        Update(sessionPath, data => data.Set(kind, layout.Clone()));
    }

    /// <summary>
    /// Forgets both layouts of a session: it opens with the default ones next time.
    /// </summary>
    public void Reset(UFile sessionPath)
    {
        ArgumentNullException.ThrowIfNull(sessionPath);
        lock (SyncRoot)
        {
            var list = LoadList();
            if (list.RemoveAll(x => IsSession(x, sessionPath)) > 0)
                SaveList(list);
        }
    }

    private void Update(UFile sessionPath, Action<SessionPanelLayouts> update)
    {
        lock (SyncRoot)
        {
            var list = LoadList();
            var data = Find(list, sessionPath) ?? new SessionPanelLayouts { FilePath = sessionPath };
            list.Remove(data);
            update(data);
            data.Timestamp = DateTime.UtcNow.Ticks;
            list.Insert(0, data);
            SaveList(list);
        }
    }

    // A copy of the file's list, the most recent first
    private List<SessionPanelLayouts> LoadList()
    {
        var list = settingsKey.GetValue(loadLatestProfile(), true) ?? [];
        return list.Where(x => x?.FilePath is not null).OrderByDescending(x => x.Timestamp).ToList();
    }

    private void SaveList(List<SessionPanelLayouts> list)
    {
        settingsKey.SetValue(list.Take(MaxSessionCount).ToList());
        save();
    }

    private static SessionPanelLayouts? Find(List<SessionPanelLayouts> list, UFile sessionPath) => list.FirstOrDefault(x => IsSession(x, sessionPath));

    private static bool IsSession(SessionPanelLayouts data, UFile sessionPath) => string.Equals(data.FilePath, sessionPath, StringComparison.OrdinalIgnoreCase);
}
