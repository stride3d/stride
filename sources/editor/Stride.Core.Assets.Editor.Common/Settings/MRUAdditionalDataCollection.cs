// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Settings;
using Stride.Core.Presentation.Collections;

namespace Stride.Core.Assets.Editor.Settings
{
    /// <summary>
    /// The <see cref="MRUAdditionalData"/> of the most recently used sessions, kept in the internal settings file. Each change reads
    /// the file first and writes it at once, so that several editors running at the same time (WPF and Avalonia among them) keep
    /// each other's changes.
    /// </summary>
    public sealed class MRUAdditionalDataCollection
    {
        private sealed class MostRecentlyUsedFileEqualityComparer : IEqualityComparer<MRUAdditionalData>
        {
            public bool Equals(MRUAdditionalData x, MRUAdditionalData y)
            {
                return ReferenceEquals(x, y) || string.Equals(x?.FilePath, y?.FilePath, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(MRUAdditionalData obj)
            {
                return obj?.FilePath?.GetHashCode() ?? 0;
            }
        }

        private static readonly object SyncRoot = new object();

        private readonly Func<SettingsProfile> loadLatestProfile;
        private readonly SettingsKey<List<MRUAdditionalData>> settingsKey;
        private readonly Action save;
        private readonly Action<MRUAdditionalData> initializeNewData;
        private readonly ObservableSet<MRUAdditionalData> mruList = new ObservableSet<MRUAdditionalData>(new MostRecentlyUsedFileEqualityComparer());

        /// <param name="loadLatestProfile">Loads a copy of the internal settings from the file.</param>
        /// <param name="settingsKey">The key of the data in the internal settings.</param>
        /// <param name="save">Writes the internal settings to the file.</param>
        /// <param name="initializeNewData">Sets what an editor needs in the data of a session that has none yet (the default layouts of WPF).</param>
        public MRUAdditionalDataCollection(Func<SettingsProfile> loadLatestProfile, SettingsKey<List<MRUAdditionalData>> settingsKey, Action save, Action<MRUAdditionalData> initializeNewData = null)
        {
            if (loadLatestProfile == null) throw new ArgumentNullException(nameof(loadLatestProfile));
            if (settingsKey == null) throw new ArgumentNullException(nameof(settingsKey));
            if (save == null) throw new ArgumentNullException(nameof(save));

            this.loadLatestProfile = loadLatestProfile;
            this.settingsKey = settingsKey;
            this.save = save;
            this.initializeNewData = initializeNewData;
        }

        /// <summary>
        /// The max number of files in the MRU list
        /// </summary>
        public const int MaxMRUCount = 20;

        public MRUAdditionalData GetData(UFile filePath)
        {
            // Ensure we are properly synchronized with the file.
            LoadFromSettings();

            return GetOrCreateDataPrivate(filePath, false);
        }

        public void RemoveFile(UFile filePath)
        {
            // Reload settings in case concurrent Game Studio instances are running.
            LoadFromSettings();

            if (RemoveFilePrivate(filePath))
            {
                // Save immediately
                SaveToSettings();
            }
        }

        /// <summary>
        /// Changes the data of a session, created if it has none, and saves it immediately.
        /// </summary>
        /// <param name="filePath">The session.</param>
        /// <param name="update">The change.</param>
        public void Update(UFile filePath, Action<MRUAdditionalData> update)
        {
            if (update == null) throw new ArgumentNullException(nameof(update));

            // Ensure we are properly synchronized with the file.
            LoadFromSettings();

            update(GetOrCreateDataPrivate(filePath));

            // Save immediately
            SaveToSettings();
        }

        public void UpdateData(MRUAdditionalData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            // Ensure we are properly synchronized with the file.
            LoadFromSettings();

            UpdateDataPrivate(data);

            // Save immediately
            SaveToSettings();
        }

        public void UpdateOpenedAssets(UFile filePath, IEnumerable<AssetId> openedAssets)
        {
            // Ensure we are properly synchronized with the file.
            LoadFromSettings();

            var data = GetOrCreateDataPrivate(filePath);
            data.OpenedAssets.Clear();
            data.OpenedAssets.AddRange(openedAssets);
            UpdateDataPrivate(data);

            // Save immediately
            SaveToSettings();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MRUAdditionalData GetOrCreateDataPrivate(UFile filePath, bool createIfNotExist = true)
        {
            var data = mruList.FirstOrDefault(m => string.Equals(m?.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
            if (data == null && createIfNotExist)
            {
                data = new MRUAdditionalData(filePath);
                initializeNewData?.Invoke(data);
                mruList.Insert(0, data);
            }
            return data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool RemoveFilePrivate(UFile filePath)
        {
            return mruList.RemoveWhere(x => string.Equals(x.FilePath, filePath, StringComparison.OrdinalIgnoreCase)) > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateDataPrivate(MRUAdditionalData data)
        {
            if (!IsDataValid(data))
                return;

            // Remove it if it was already in the list
            mruList.RemoveWhere(x => string.Equals(x.FilePath, data.FilePath, StringComparison.OrdinalIgnoreCase));
            // Add again
            mruList.Add(data);
        }

        private void LoadFromSettings()
        {
            lock (SyncRoot)
            {
                // We load a copy of the profile in case concurrent Game Studio instances are running.
                var profileCopy = loadLatestProfile.Invoke();
                var loadedList = settingsKey.GetValue(profileCopy, true);
                // Sort by descending timestamp
                loadedList.Sort((x, y) => -x.Timestamp.CompareTo(y.Timestamp));
                mruList.Clear();
                mruList.AddRange(loadedList.Where(IsDataValid));
            }
        }

        private static bool IsDataValid(MRUAdditionalData data)
        {
            return data?.FilePath != null;
        }

        private void SaveToSettings()
        {
            lock (SyncRoot)
            {
                // Update the current profile with the new values so we can properly save it.
                settingsKey.SetValue(mruList.Take(MaxMRUCount).ToList());
                save.Invoke();
                // Ensure we are properly synchronized with the file.
                LoadFromSettings();
            }
        }
    }
}
