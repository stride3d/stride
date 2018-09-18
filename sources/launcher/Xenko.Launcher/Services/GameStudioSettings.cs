// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xenko.Core.Assets.Editor;
using Xenko.Core.Extensions;
using Xenko.Core.MostRecentlyUsedFiles;
using Xenko.Core.IO;
using Xenko.Core.Settings;
using Xenko.Core.Yaml;

namespace Xenko.LauncherApp.Services
{
    public static class GameStudioSettings
    {
        private static readonly SettingsProfile GameStudioProfile;

        private static readonly SettingsContainer InternalSettingsContainer = new SettingsContainer();

        private static readonly SettingsContainer GameStudioSettingsContainer = new SettingsContainer();

        private static readonly SettingsKey<MRUDictionary> MostRecentlyUsedSessionsKey = new SettingsKey<MRUDictionary>("Internal/MostRecentlyUsedSessions", InternalSettingsContainer, () => new MRUDictionary());

        private static readonly SettingsKey<string> StoreCrashEmail = new SettingsKey<string>("Interface/StoreCrashEmail", GameStudioSettingsContainer, "");

        private static readonly object LockObject = new object();

        private static readonly MostRecentlyUsedFileCollection MRU;

        private static IReadOnlyCollection<UFile> mostRecentlyUsed;

        private static bool updating;

        static GameStudioSettings()
        {
            MRU = new MostRecentlyUsedFileCollection(() => InternalSettingsContainer.LoadSettingsProfile(GetLatestInternalConfigPath(), false, null, false), MostRecentlyUsedSessionsKey, () => InternalSettingsContainer.SaveSettingsProfile(InternalSettingsContainer.CurrentProfile, GetLatestInternalConfigPath()));
            MostRecentlyUsedSessionsKey.FallbackDeserializers.Add(LegacyMRUDeserializer);
            InternalSettingsContainer.LoadSettingsProfile(GetLatestInternalConfigPath(), true);
            InternalSettingsContainer.CurrentProfile.MonitorFileModification = true;
            InternalSettingsContainer.CurrentProfile.FileModified += (sender, e) => { GameStudioSettingsFileChanged(sender, e); };
            GameStudioProfile = GameStudioSettingsContainer.LoadSettingsProfile(GetLatestGameStudioConfigPath(), true);
            UpdateMostRecentlyUsed();
        }

        public static event EventHandler<EventArgs> RecentProjectsUpdated;

        public static string CrashReportEmail
        {
            get
            {
                try
                {
                    lock (LockObject)
                    {
                        GameStudioSettingsContainer.ReloadSettingsProfile(GameStudioProfile);
                        return StoreCrashEmail.GetValue();
                    }
                }
                catch (Exception)
                {
                    return "";
                }
            }
            set
            {
                try
                {
                    lock (LockObject)
                    {
                        GameStudioSettingsContainer.ReloadSettingsProfile(GameStudioProfile);
                        StoreCrashEmail.SetValue(value);
                        GameStudioSettingsContainer.SaveSettingsProfile(GameStudioProfile, GetLatestGameStudioConfigPath());
                    }
                }
                catch (Exception e)
                {
                    e.Ignore();
                }
            }
        }

        public static IReadOnlyCollection<UFile> GetMostRecentlyUsed()
        {
            List<UFile> result;
            lock (LockObject)
            {
                result = new List<UFile>(mostRecentlyUsed);
            }
            return result;
        }

        private static void GameStudioSettingsFileChanged(object sender, FileModifiedEventArgs e)
        {
            e.ReloadFile = true;
            UpdateMostRecentlyUsed();
        }

        public static void RemoveMostRecentlyUsed(UFile filePath, string xenkoVersion)
        {
            lock (LockObject)
            {
                MRU.RemoveFile(filePath, xenkoVersion);
                UpdateMostRecentlyUsed();
            }
        }

        private static void UpdateMostRecentlyUsed()
        {
            if (updating)
                return;

            lock (LockObject)
            {
                updating = true;
                MRU.LoadFromSettings();
                updating = false;
                mostRecentlyUsed = MRU.MostRecentlyUsedFiles.Select(x => x.FilePath).ToList();
            }
            RecentProjectsUpdated?.Invoke(null, EventArgs.Empty);
        }

        private static string GetLatestInternalConfigPath()
        {
            return GetInternalConfigPaths().FirstOrDefault(File.Exists) ?? EditorPath.InternalConfigPath;
        }

        private static string GetLatestGameStudioConfigPath()
        {
            return GetGameStudioConfigPaths().FirstOrDefault(File.Exists) ?? EditorPath.EditorConfigPath;
        }

        private static IEnumerable<string> GetInternalConfigPaths()
        {
            yield return EditorPath.InternalConfigPath;
        }

        private static IEnumerable<string> GetGameStudioConfigPaths()
        {
            yield return EditorPath.EditorConfigPath;
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
    }
}
