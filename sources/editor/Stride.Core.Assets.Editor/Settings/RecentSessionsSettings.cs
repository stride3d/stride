// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;
using Stride.Core.MostRecentlyUsedFiles;
using Stride.Core.Settings;
using Stride.Core.Yaml;

namespace Stride.Core.Assets.Editor.Settings;

/// <summary>
/// The most recently used sessions and what the editors keep for each of them, in the internal settings that both Game Studios
/// (and the launcher) read and write, under the same keys.
/// </summary>
public static class RecentSessionsSettings
{
    /// <summary>
    /// The most recently used sessions, per editor version.
    /// </summary>
    public static readonly SettingsKey<MRUDictionary> MostRecentlyUsedSessions = new("Internal/MostRecentlyUsedSessions", InternalSettings.SettingsContainer, () => new MRUDictionary());

    /// <summary>
    /// What the editors keep for each recent session: the assets open in an editor, which both editors restore, and the docking
    /// layouts of the WPF Game Studio, which the Avalonia editor keeps as they are.
    /// </summary>
    public static readonly SettingsKey<List<MRUAdditionalData>> MostRecentlyUsedSessionsData = new("Internal/MostRecentlyUsedSessionsData2", InternalSettings.SettingsContainer, () => new List<MRUAdditionalData>());

    static RecentSessionsSettings()
    {
        MostRecentlyUsedSessions.FallbackDeserializers.Add(LegacyMRUDeserializer);
        // Another editor instance may have changed the list since this one started
        InternalSettings.KeepFileValueOnSave(MostRecentlyUsedSessions);
    }

    /// <summary>
    /// Creates the collection of the most recently used sessions over the settings file.
    /// </summary>
    public static MostRecentlyUsedFileCollection CreateMostRecentlyUsedFiles()
    {
        var mru = new MostRecentlyUsedFileCollection(InternalSettings.LoadProfileCopy, MostRecentlyUsedSessions, InternalSettings.SaveProfile);
        mru.LoadFromSettings();
        return mru;
    }

    /// <summary>
    /// Creates the collection of what the editors keep for each recent session, over the settings file.
    /// </summary>
    /// <param name="initializeNewData">Fills the data of a session that has none yet, or <c>null</c>.</param>
    public static MRUAdditionalDataCollection CreateMostRecentlyUsedSessionsData(Action<MRUAdditionalData>? initializeNewData = null)
    {
        return new MRUAdditionalDataCollection(InternalSettings.LoadProfileCopy, MostRecentlyUsedSessionsData, InternalSettings.SaveProfile, initializeNewData);
    }

    // The MRU of Stride 1.3 was a plain list of files
    private static object LegacyMRUDeserializer(EventReader eventReader)
    {
        const string legacyVersion = "1.3";
        var mru = (List<UFile>)SettingsYamlSerializer.Default.Deserialize(eventReader, typeof(List<UFile>))!;
        var initialTimestamp = DateTime.UtcNow.Ticks;
        return new MRUDictionary
        {
            { legacyVersion, mru.Select(x => new MostRecentlyUsedFile(x) { Timestamp = initialTimestamp-- }).ToList() }
        };
    }
}
