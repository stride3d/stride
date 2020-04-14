// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.IO;
using Stride.Core.Settings;
using Stride.Core.Yaml;
using Stride.Core.MostRecentlyUsedFiles;

namespace Stride.Core.Assets.Editor.Settings
{
    public static class InternalSettings
    {
        public static SettingsContainer SettingsContainer = new SettingsContainer();

        public static SettingsKey<MRUDictionary> MostRecentlyUsedSessions = new SettingsKey<MRUDictionary>("Internal/MostRecentlyUsedSessions", SettingsContainer, () => new MRUDictionary());
        public static SettingsKey<bool> LoadingStartupSession = new SettingsKey<bool>("Internal/LoadingStartupSession", SettingsContainer, false);
        public static SettingsKey<string> FileDialogLastImportDirectory = new SettingsKey<string>("Internal/FileDialogLastImportDirectory", SettingsContainer, "");
        public static SettingsKey<string> FileDialogLastOpenSessionDirectory = new SettingsKey<string>("Internal/FileDialogLastOpenSessionDirectory", SettingsContainer, "");
        public static SettingsKey<string> TemplatesWindowDialogLastNewSessionTemplateDirectory = new SettingsKey<string>("Internal/TemplatesWindowDialogLastNewSessionTemplateDirectory", SettingsContainer, "");
        public static SettingsKey<SortRule> AssetViewSortRule = new SettingsKey<SortRule>("Internal/AssetViewSortRule", SettingsContainer, SortRule.TypeOrderThenName);
        public static SettingsKey<DisplayAssetMode> AssetViewDisplayMode = new SettingsKey<DisplayAssetMode>("Internal/AssetViewDisplayMode", SettingsContainer, DisplayAssetMode.AssetAndFolderInSelectedFolder);

        private static readonly SettingsProfile Profile;

        static InternalSettings()
        {
            MostRecentlyUsedSessions.FallbackDeserializers.Add(LegacyMRUDeserializer);
            Profile = LoadProfile(true);
            SettingsContainer.CurrentProfile = Profile;
        }

        /// <summary>
        /// Loads a copy of the internal settings from the file.
        /// </summary>
        /// <returns></returns>
        public static SettingsProfile LoadProfileCopy()
        {
            return LoadProfile(false);
        }

        /// <summary>
        /// Loads a copy of the internal settings from the file.
        /// </summary>
        /// <returns></returns>
        private static SettingsProfile LoadProfile(bool registerProfile)
        {
            return SettingsContainer.LoadSettingsProfile(GetLatestInternalConfigPath(), false, null, registerProfile) ?? SettingsContainer.CreateSettingsProfile(false);
        }

        /// <summary>
        /// Saves the settings into the settings file.
        /// </summary>
        public static void Save()
        {
            // Special case for MRU: we always reload the latest version from the file.
            // Actually modifying and saving MRU is done in a specific class.
            var profileCopy = LoadProfileCopy();
            var mruList = MostRecentlyUsedSessions.GetValue(profileCopy, true);
            MostRecentlyUsedSessions.SetValue(mruList);
            WriteFile();
        }

        /// <summary>
        /// Saves the settings into the settings file.
        /// </summary>
        public static void WriteFile()
        {
            SettingsContainer.SaveSettingsProfile(Profile, EditorPath.InternalConfigPath);
        }

        private static object LegacyMRUDeserializer(EventReader eventReader)
        {
            const string legacyVersion = "1.3";
            var mru = (List<UFile>)SettingsYamlSerializer.Default.Deserialize(eventReader, typeof(List<UFile>));
            var initialTimestamp = DateTime.UtcNow.Ticks;
            return new Dictionary<string, List<MostRecentlyUsedFile>>
            {
                { legacyVersion, mru.Select(x => new MostRecentlyUsedFile(x) { Timestamp = initialTimestamp-- }).ToList() }
            };
        }

        private static string GetLatestInternalConfigPath()
        {
            return GetInternalConfigPaths().FirstOrDefault(File.Exists) ?? EditorPath.InternalConfigPath;
        }

        private static IEnumerable<string> GetInternalConfigPaths()
        {
            yield return EditorPath.InternalConfigPath;
        }
    }
}
