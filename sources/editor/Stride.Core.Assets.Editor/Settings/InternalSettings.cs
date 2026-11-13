// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable

using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Settings;
using Stride.Core.Assets.Presentation.ViewModels.Logs;

namespace Stride.Core.Assets.Editor.Settings;

/// <summary>
/// The internal (not user-facing) settings of the editor, in the file the WPF Game Studio uses too.
/// </summary>
public static class InternalSettings
{
    private static readonly SettingsProfile profile;
    private static readonly ChangedSettingsWriter writer;
    private static readonly List<Action<SettingsProfile>> fileValueRestorers = [];

    static InternalSettings()
    {
        profile = LoadProfile(true);
        SettingsContainer.CurrentProfile = profile;
        writer = new ChangedSettingsWriter(SettingsContainer, profile, EditorPath.InternalConfigPath);
        // Changed by the other editors running at the same time: each change reads the file first (DisabledEditorCompanionStore)
        KeepFileValueOnSave(DisabledEditorCompanions);
        KeepFileValueOnSave(ExtensionPackages);
    }

    public static SettingsContainer SettingsContainer { get; } = new();

    public static SettingsKey<bool> LoadingStartupSession { get; } = new("Internal/LoadingStartupSession", SettingsContainer, false);

    public static SettingsKey<string> FileDialogLastImportDirectory { get; } = new("Internal/FileDialogLastImportDirectory", SettingsContainer, "");

    public static SettingsKey<string> FileDialogLastOpenSessionDirectory { get; } = new("Internal/FileDialogLastOpenSessionDirectory", SettingsContainer, "");

    public static SettingsKey<string> TemplatesWindowDialogLastNewSessionTemplateDirectory { get; } = new("Internal/TemplatesWindowDialogLastNewSessionTemplateDirectory", SettingsContainer, "");

    public static SettingsKey<SortRule> AssetViewSortRule { get; } = new("Internal/AssetViewSortRule", SettingsContainer, SortRule.TypeOrderThenName);

    public static SettingsKey<DisplayAssetMode> AssetViewDisplayMode { get; } = new("Internal/AssetViewDisplayMode", SettingsContainer, DisplayAssetMode.AssetAndFolderInSelectedFolder);

    public static SettingsKey<double> AssetViewTileThumbnailZoom { get; } = new("Internal/AssetViewTileThumbnailZoom", SettingsContainer, 96.0);

    public static SettingsKey<double> AssetViewGridThumbnailZoom { get; } = new("Internal/AssetViewGridThumbnailZoom", SettingsContainer, 16.0);

    public static SettingsKey<List<AssetFilterViewModelData>> ViewFilters { get; } = new("Internal/CurrentAssetFilters", SettingsContainer, () => new List<AssetFilterViewModelData>());

    public static SettingsKey<AssetLogScope> AssetLogDisplayScope { get; } = new("Internal/AssetLogScope", SettingsContainer, AssetLogScope.AllAssets);

    public static SettingsKey<List<string>> AssetLogHiddenSources { get; } = new("Internal/AssetLogHiddenSources", SettingsContainer, () => new List<string>());

    /// <summary>
    /// The Editor packages switched off per session (<see cref="DisabledEditorCompanionStore"/>), read by both editors.
    /// </summary>
    public static SettingsKey<List<DisabledEditorCompanions>> DisabledEditorCompanions { get; } = new("Internal/DisabledEditorCompanions", SettingsContainer, () => new List<DisabledEditorCompanions>());

    /// <summary>
    /// The extensions the user installed, per editor major.minor version (<see cref="EditorExtensionStore"/>), read by both
    /// editors.
    /// </summary>
    public static SettingsKey<List<EditorExtensionList>> ExtensionPackages { get; } = new("Internal/EditorExtensions", SettingsContainer, () => new List<EditorExtensionList>());

    /// <summary>
    /// Loads a copy of the internal settings from the file, without registering it as the current profile.
    /// </summary>
    public static SettingsProfile LoadProfileCopy()
    {
        return LoadProfile(false);
    }

    /// <summary>
    /// Registers a key that other instances of the editors also write, such as the most recently used sessions: <see cref="Save"/>
    /// writes the value the file has for it, not the one this instance read at startup.
    /// </summary>
    public static void KeepFileValueOnSave<T>(SettingsKey<T> key)
    {
        ArgumentNullException.ThrowIfNull(key);
        lock (fileValueRestorers)
        {
            fileValueRestorers.Add(latest => key.SetValue(key.GetValue(latest, true)));
        }
    }

    /// <summary>
    /// Saves the internal settings into the file, keeping the file's value of the keys registered with <see cref="KeepFileValueOnSave"/>.
    /// </summary>
    public static void Save()
    {
        Action<SettingsProfile>[] restorers;
        lock (fileValueRestorers)
        {
            restorers = [.. fileValueRestorers];
        }
        if (restorers.Length > 0)
        {
            // These keys are modified and saved by the classes that own them (MostRecentlyUsedFileCollection...): reload the latest version from the file
            var latest = LoadProfileCopy();
            foreach (var restore in restorers)
                restore(latest);
        }
        SaveProfile();
    }

    /// <summary>
    /// Writes the settings this editor changed to the file: the values another editor wrote since stay.
    /// </summary>
    public static void SaveProfile()
    {
        writer.Write();
    }

    private static SettingsProfile LoadProfile(bool registerProfile)
    {
        return SettingsContainer.LoadSettingsProfile(EditorPath.InternalConfigPath, false, null, registerProfile) ?? SettingsContainer.CreateSettingsProfile(false);
    }
}
