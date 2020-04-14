// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Extensions;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Settings;
using Stride.Core.MostRecentlyUsedFiles;
using Stride.Core.Presentation.Collections;

namespace Stride.GameStudio
{
    internal class MRUAdditionalDataCollection
    {
        private class MostRecentlyUsedFileEqualityComparer : IEqualityComparer<MRUAdditionalData>
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
        private readonly ObservableSet<MRUAdditionalData> mruList = new ObservableSet<MRUAdditionalData>(new MostRecentlyUsedFileEqualityComparer());

        public MRUAdditionalDataCollection(Func<SettingsProfile> loadLatestProfile, SettingsKey<List<MRUAdditionalData>> settingsKey, Action save)
        {
            if (loadLatestProfile == null) throw new ArgumentNullException(nameof(loadLatestProfile));
            if (settingsKey == null) throw new ArgumentNullException(nameof(settingsKey));
            if (save == null) throw new ArgumentNullException(nameof(save));

            this.loadLatestProfile = loadLatestProfile;
            this.settingsKey = settingsKey;
            this.save = save;
        }

        /// <summary>
        /// The max number of files in the MRU list
        /// </summary>
        public static int MaxMRUCount => MostRecentlyUsedFileCollection.MaxMRUCount;

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

        public void ResetAllLayouts(UFile filePath)
        {
            // Ensure we are properly synchronized with the file.
            LoadFromSettings();

            var data = GetOrCreateDataPrivate(filePath);
            data.DockingLayoutVersion = GameStudioInternalSettings.CurrentLayoutVersion;
            data.DockingLayout = GameStudioInternalSettings.DefaultLayout;
            data.DockingLayoutEditors = GameStudioInternalSettings.DefaultEditorLayout;

            // Save immediately
            SaveToSettings();
        }

        public void ResetLayout(UFile filePath)
        {
            // Ensure we are properly synchronized with the file.
            LoadFromSettings();

            var data = GetOrCreateDataPrivate(filePath);
            data.DockingLayout = GameStudioInternalSettings.DefaultLayout;

            // Save immediately
            SaveToSettings();
        }

        public void ResetEditorsLayout(UFile filePath)
        {
            // Ensure we are properly synchronized with the file.
            LoadFromSettings();

            var data = GetOrCreateDataPrivate(filePath);
            data.DockingLayoutEditors = GameStudioInternalSettings.DefaultEditorLayout;

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

        public void UpdateLayout(UFile filePath, string layout)
        {
            // Ensure we are properly synchronized with the file.
            LoadFromSettings();

            var data = GetOrCreateDataPrivate(filePath);
            data.DockingLayout = layout;

            // Save immediately
            SaveToSettings();
        }

        public void UpdateEditorsLayout(UFile filePath, string layout)
        {
            // Ensure we are properly synchronized with the file.
            LoadFromSettings();

            var data = GetOrCreateDataPrivate(filePath);
            data.DockingLayoutEditors = layout;

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
                data = new MRUAdditionalData(filePath)
                {
                    DockingLayout = GameStudioInternalSettings.DefaultLayout,
                    DockingLayoutEditors = GameStudioInternalSettings.DefaultEditorLayout,
                    DockingLayoutVersion = GameStudioInternalSettings.CurrentLayoutVersion,
                };
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
