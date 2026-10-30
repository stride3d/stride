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
/// The internal (not user-facing) settings of the Avalonia Game Studio, in the same file as the WPF Game Studio's: the most
/// recently used sessions and what each of them keeps, under the same keys as WPF; the main window placement and the panel
/// sizes, under keys of its own.
/// </summary>
internal static class GameStudioInternalSettings
{
    public static SettingsContainer SettingsContainer => InternalSettings.SettingsContainer;

    /// <summary>
    /// The value of <see cref="WindowLeft"/> and <see cref="WindowTop"/> before the window was ever closed.
    /// </summary>
    public const int NoPosition = int.MinValue;

    // The window and the panels of this editor have their own keys: the WPF Game Studio keeps its window under Internal/Window*
    // and its panels in the docking layouts of each session, so neither editor changes the other's
    public static readonly SettingsKey<bool> WindowMaximized = new("Internal/Avalonia/WindowMaximized", SettingsContainer, false);
    public static readonly SettingsKey<int> WindowWidth = new("Internal/Avalonia/WindowWidth", SettingsContainer, (int)WorkArea.Value.Width);
    public static readonly SettingsKey<int> WindowHeight = new("Internal/Avalonia/WindowHeight", SettingsContainer, (int)WorkArea.Value.Height);
    /// <summary>
    /// The position of the window, in pixels of the desktop; <see cref="NoPosition"/> centers it.
    /// </summary>
    public static readonly SettingsKey<int> WindowLeft = new("Internal/Avalonia/WindowLeft", SettingsContainer, NoPosition);
    /// <inheritdoc cref="WindowLeft"/>
    public static readonly SettingsKey<int> WindowTop = new("Internal/Avalonia/WindowTop", SettingsContainer, NoPosition);
    public static readonly SettingsKey<int> WorkAreaWidth = new("Internal/Avalonia/WorkAreaWidth", SettingsContainer, (int)WorkArea.Value.Width);
    public static readonly SettingsKey<int> WorkAreaHeight = new("Internal/Avalonia/WorkAreaHeight", SettingsContainer, (int)WorkArea.Value.Height);

    /// <summary>
    /// The sizes of the panels of the main window, set with its splitters: for each grid of panels, the sizes of its rows or
    /// columns (e.g. "2*,2,1*"). Docking will replace it.
    /// </summary>
    public static readonly SettingsKey<Dictionary<string, string>> PanelSizes = new("Internal/Avalonia/PanelSizes", SettingsContainer, () => new Dictionary<string, string>());

    /// <summary>
    /// The most recently used sessions, per editor version (the WPF Game Studio reads and writes the same key).
    /// </summary>
    public static readonly SettingsKey<MRUDictionary> MostRecentlyUsedSessions = new("Internal/MostRecentlyUsedSessions", SettingsContainer, () => new MRUDictionary());

    /// <summary>
    /// What the editors keep for each recent session: the assets open in an editor, which both editors restore, and the docking
    /// layouts of the WPF Game Studio, which this editor keeps as they are.
    /// </summary>
    public static readonly SettingsKey<List<MRUAdditionalData>> MostRecentlyUsedSessionsData = new("Internal/MostRecentlyUsedSessionsData2", SettingsContainer, () => new List<MRUAdditionalData>());

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

    /// <summary>
    /// Creates the collection of what the editors keep for each recent session, over the settings file.
    /// </summary>
    public static MRUAdditionalDataCollection CreateMostRecentlyUsedSessionsData()
    {
        return new MRUAdditionalDataCollection(LoadProfileCopy, MostRecentlyUsedSessionsData, WriteFile);
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
