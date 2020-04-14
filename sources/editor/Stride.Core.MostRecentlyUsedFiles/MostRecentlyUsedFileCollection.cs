// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core.Annotations;
using Xenko.Core.Extensions;
using Xenko.Core.IO;
using Xenko.Core.Settings;
using Xenko.Core.Presentation.Collections;

namespace Xenko.Core.MostRecentlyUsedFiles
{
    // TODO: this is a hack because YamlSerializer is static and there is no way to disable serialization with Id for Settings at the moment. This is temporary!
    [NonIdentifiableCollectionItems]
    public class MRUDictionary : Dictionary<string, List<MostRecentlyUsedFile>>
    {

    }

    /// <summary>
    /// A class that handles a list of most recently used (MRU) files.
    /// </summary>
    public class MostRecentlyUsedFileCollection
    {
        private class MostRecentlyUsedFileEqualityComparer : IEqualityComparer<MostRecentlyUsedFile>
        {
            public bool Equals(MostRecentlyUsedFile x, MostRecentlyUsedFile y)
            {
                return ReferenceEquals(x, y) || string.Equals(x?.FilePath, y?.FilePath, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(MostRecentlyUsedFile obj)
            {
                return obj.FilePath.GetHashCode();
            }
        }

        /// <summary>
        /// The max number of files in the MRU list
        /// </summary>
        public static readonly int MaxMRUCount = 20;

        private readonly SettingsKey<MRUDictionary> settingsKey;
        private readonly Action save;
        private readonly ObservableSet<MostRecentlyUsedFile> mostRecentlyUsedFiles = new ObservableSet<MostRecentlyUsedFile>(new MostRecentlyUsedFileEqualityComparer());

        public MostRecentlyUsedFileCollection([NotNull] Func<SettingsProfile> loadLatestProfile, [NotNull] SettingsKey<MRUDictionary> settingsKey, Action save)
        {
            LoadLatestProfile = loadLatestProfile ?? throw new ArgumentNullException(nameof(loadLatestProfile));
            this.settingsKey = settingsKey ?? throw new ArgumentNullException(nameof(settingsKey));
            this.save = save;
        }

        public Func<SettingsProfile> LoadLatestProfile;

        public IReadOnlyObservableCollection<MostRecentlyUsedFile> MostRecentlyUsedFiles => mostRecentlyUsedFiles;

        public void AddFile(UFile filePath, string xenkoVersion)
        {
            // Reload settings in case concurrent Game Studio instances are running.
            LoadFromSettings();
            // Remove it if it was already in the list
            mostRecentlyUsedFiles.RemoveWhere(x => string.Equals(x.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
            // Add it on top of the list
            mostRecentlyUsedFiles.Insert(0, new MostRecentlyUsedFile(filePath) { Version = xenkoVersion });
            // Save immediately
            SaveToSettings(xenkoVersion);
        }

        public void RemoveFile(UFile filePath, string xenkoVersion)
        {
            // Reload settings in case concurrent Game Studio instances are running.
            LoadFromSettings();
            mostRecentlyUsedFiles.RemoveWhere(x => string.Equals(x.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
            // Save immediately
            SaveToSettings(xenkoVersion);
        }

        public void Clear(string xenkoVersion)
        {
            // Reload settings in case concurrent Game Studio instances are running.
            LoadFromSettings();
            mostRecentlyUsedFiles.Clear();
            // Save immediately
            SaveToSettings(xenkoVersion);
        }

        public void LoadFromSettings()
        {
            mostRecentlyUsedFiles.Clear();

            try
            {
                // We load a copy of the profile in case concurrent Game Studio instances are running.
                var profileCopy = LoadLatestProfile();
                var mruList = settingsKey.GetValue(profileCopy, true);

                // If the version is null, we take all versions, otherwise we take version equal or older to the current version.
                var files = mruList.SelectMany(x =>
                {
                    // Set version information
                    x.Value.ForEach(f => f.Version = x.Key);
                    return x.Value;
                }).ToList();
                // Sort by descending timestamp
                files.Sort((x, y) => -x.Timestamp.CompareTo(y.Timestamp));
                mostRecentlyUsedFiles.AddRange(files);
            }
            catch (Exception e)
            {
                e.Ignore();
            }
        }

        private void SaveToSettings(string xenkoVersion)
        {
            // We load a copy of the profile in case concurrent Game Studio instances are running.
            // Note that when this is called, the collection should be sync for the current version,
            // but not for other versions.
            var profileCopy = LoadLatestProfile();
            var mruList = settingsKey.GetValue(profileCopy, true);
            mruList[xenkoVersion] = MostRecentlyUsedFiles.Where(x => x.Version.Equals(xenkoVersion)).Take(MaxMRUCount).ToList();
            // Update the current profile with the new values so we can properly save it.
            settingsKey.SetValue(mruList);
            save?.Invoke();

            // Ensure we are properly synchronized with the file.
            LoadFromSettings();
        }
    }
}
