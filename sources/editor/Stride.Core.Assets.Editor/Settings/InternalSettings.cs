// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.ViewModel.Logs;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Settings;

namespace Stride.Core.Assets.Editor.Settings;

public static class InternalSettings
{
    private static readonly SettingsProfile profile;

    static InternalSettings()
    {
        profile = LoadProfile(true);
        SettingsContainer.CurrentProfile = profile;
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

    private static SettingsProfile LoadProfile(bool registerProfile)
    {
        return SettingsContainer.LoadSettingsProfile(EditorPath.InternalConfigPath, false, null, registerProfile) ?? SettingsContainer.CreateSettingsProfile(false);
    }

    public static void SaveProfile()
    {
        SettingsContainer.SaveSettingsProfile(profile, EditorPath.InternalConfigPath);
    }
}
