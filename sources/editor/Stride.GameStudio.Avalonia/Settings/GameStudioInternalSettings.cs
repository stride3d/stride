// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Stride.Core.Assets.Editor;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.IO;
using Stride.Core.MostRecentlyUsedFiles;
using Stride.Core.Presentation.Avalonia.Extensions;
using Stride.Core.Presentation.Avalonia.Services;
using Stride.Core.Settings;
using Stride.Core.Yaml;

namespace Stride.GameStudio.Avalonia.Settings;

/// <summary>
/// The internal (not user-facing) settings of the Avalonia Game Studio: the main window placement and the most recently
/// used sessions, in the same file and under the same keys as the WPF Game Studio's.
/// </summary>
internal static class GameStudioInternalSettings
{
    public static SettingsContainer SettingsContainer => InternalSettings.SettingsContainer;

    public static readonly SettingsKey<bool> WindowMaximized = new("Internal/WindowMaximized", SettingsContainer, false);
    public static readonly SettingsKey<int> WindowWidth = new("Internal/WindowWidth", SettingsContainer, (int)WorkArea.Value.Width);
    public static readonly SettingsKey<int> WindowHeight = new("Internal/WindowHeight", SettingsContainer, (int)WorkArea.Value.Height);
    public static readonly SettingsKey<int> WorkAreaWidth = new("Internal/WorkAreaWidth", SettingsContainer, (int)WorkArea.Value.Width);
    public static readonly SettingsKey<int> WorkAreaHeight = new("Internal/WorkAreaHeight", SettingsContainer, (int)WorkArea.Value.Height);

    /// <summary>
    /// The most recently used sessions, per editor version (the WPF Game Studio reads and writes the same key).
    /// </summary>
    public static readonly SettingsKey<MRUDictionary> MostRecentlyUsedSessions = new("Internal/MostRecentlyUsedSessions", SettingsContainer, () => new MRUDictionary());

    static GameStudioInternalSettings()
    {
        MostRecentlyUsedSessions.FallbackDeserializers.Add(LegacyMRUDeserializer);
    }

    /// <summary>
    /// Loads a copy of the internal settings from the file, without registering it as the current profile.
    /// </summary>
    public static SettingsProfile LoadProfileCopy()
    {
        return SettingsContainer.LoadSettingsProfile(EditorPath.InternalConfigPath, false, null, false) ?? SettingsContainer.CreateSettingsProfile(false);
    }

    /// <summary>
    /// Writes the current profile of the internal settings to the file.
    /// </summary>
    public static void WriteFile()
    {
        InternalSettings.SaveProfile();
    }

    /// <summary>
    /// Creates the collection of the most recently used sessions over the settings file.
    /// </summary>
    public static MostRecentlyUsedFileCollection CreateMostRecentlyUsedFiles()
    {
        var mru = new MostRecentlyUsedFileCollection(LoadProfileCopy, MostRecentlyUsedSessions, WriteFile);
        mru.LoadFromSettings();
        return mru;
    }

    private static Lazy<Rect> WorkArea => new(() =>
    {
        if (DialogService.MainWindow is { } mainWindow)
        {
            return mainWindow.GetWorkingArea().area;
        }

        // fallback to a reasonable value
        return new Rect(0, 0, 800, 600);
    });

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
