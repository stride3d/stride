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

        private readonly string version;
        private readonly bool includeOlder;
        private readonly SettingsKey<MRUDictionary> settingsKey;
        private readonly Action save;
        private readonly ObservableSet<MostRecentlyUsedFile> mostRecentlyUsedFiles = new ObservableSet<MostRecentlyUsedFile>(new MostRecentlyUsedFileEqualityComparer());

        public MostRecentlyUsedFileCollection([NotNull] Func<SettingsProfile> loadLatestProfile, [NotNull] SettingsKey<MRUDictionary> settingsKey, Action save, string version, bool includeOlder)
        {
            LoadLatestProfile = loadLatestProfile ?? throw new ArgumentNullException(nameof(loadLatestProfile));
            this.settingsKey = settingsKey ?? throw new ArgumentNullException(nameof(settingsKey));
            this.save = save;
            this.version = version;
            this.includeOlder = includeOlder;
        }

        public Func<SettingsProfile> LoadLatestProfile;

        public IReadOnlyObservableCollection<MostRecentlyUsedFile> MostRecentlyUsedFiles => mostRecentlyUsedFiles;

        public void AddFile(UFile filePath)
        {
            // Reload settings in case concurrent Game Studio instances are running.
            LoadFromSettings();
            // Remove it if it was already in the list
            mostRecentlyUsedFiles.RemoveWhere(x => string.Equals(x.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
            // Add it on top of the list
            mostRecentlyUsedFiles.Insert(0, new MostRecentlyUsedFile(filePath));
            // Save immediately
            SaveToSettings();
        }

        public void RemoveFile(UFile filePath)
        {
            // Reload settings in case concurrent Game Studio instances are running.
            LoadFromSettings();
            mostRecentlyUsedFiles.RemoveWhere(x => string.Equals(x.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
            // Save immediately
            SaveToSettings();
        }

        public void Clear()
        {
            // Reload settings in case concurrent Game Studio instances are running.
            LoadFromSettings();
            mostRecentlyUsedFiles.Clear();
            // Save immediately
            SaveToSettings();
        }

        public void LoadFromSettings()
        {
            mostRecentlyUsedFiles.Clear();

            try
            {
                // We load a copy of the profile in case concurrent Game Studio instances are running.
                var profileCopy = LoadLatestProfile();
                var mruList = settingsKey.GetValue(profileCopy, true);
                if (includeOlder)
                {
                    // If the version is null, we take all versions, otherwise we take version equal or older to the current version.
                    var files = mruList.Where(x => string.IsNullOrEmpty(version) || string.Compare(x.Key, version, StringComparison.Ordinal) <= 0).SelectMany(x =>
                    {
                        // Set version information
                        x.Value.ForEach(f => f.Version = x.Key);
                        return x.Value;
                    }).ToList();
                    // Sort by descending timestamp
                    files.Sort((x, y) => -x.Timestamp.CompareTo(y.Timestamp));
                    mostRecentlyUsedFiles.AddRange(files);
                }
                else
                {
                    // We just want the current version
                    var files = mruList.TryGetValue(version);
                    if (files != null)
                    {
                        // Sort by descending timestamp
                        files.Sort((x, y) => -x.Timestamp.CompareTo(y.Timestamp));
                        // Set version information
                        files.ForEach(x => x.Version = version);
                        mostRecentlyUsedFiles.AddRange(files);
                    }
                }
            }
            catch (Exception e)
            {
                e.Ignore();
            }
        }

        private void SaveToSettings()
        {
            // We load a copy of the profile in case concurrent Game Studio instances are running.
            // Note that when this is called, the collection should be sync for the current version,
            // but not for other versions.
            var profileCopy = LoadLatestProfile();
            var mruList = settingsKey.GetValue(profileCopy, true);
            mruList[version] = MostRecentlyUsedFiles.Take(MaxMRUCount).ToList();
            // Update the current profile with the new values so we can properly save it.
            settingsKey.SetValue(mruList);
            save?.Invoke();

            // Ensure we are properly synchronized with the file.
            LoadFromSettings();
        }
    }
}
