// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stride.Core.Assets.Editor;
using Stride.Core.IO;
using Stride.Core.Settings;

namespace Stride.LauncherApp.Services
{
    public static class LauncherSettings
    {
        private static readonly SettingsContainer SettingsContainer = new SettingsContainer();

        private static readonly SettingsKey<bool> CloseLauncherAutomaticallyKey = new SettingsKey<bool>("Internal/Launcher/CloseLauncherAutomatically", SettingsContainer, false);
        private static readonly SettingsKey<string> ActiveVersionKey = new SettingsKey<string>("Internal/Launcher/ActiveVersion", SettingsContainer, "");
        private static readonly SettingsKey<string> PreferredFrameworkKey = new SettingsKey<string>("Internal/Launcher/PreferredFramework", SettingsContainer, "netcoreapp3.1");
        private static readonly SettingsKey<int> CurrentTabKey = new SettingsKey<int>("Internal/Launcher/CurrentTabSessions", SettingsContainer, 0);
        private static readonly SettingsKey<List<UDirectory>> DeveloperVersionsKey = new SettingsKey<List<UDirectory>>("Internal/Launcher/DeveloperVersions", SettingsContainer, () => new List<UDirectory>());

        private static readonly string LauncherConfigPath = Path.Combine(EditorPath.UserDataPath, "LauncherSettings.conf");

        static LauncherSettings()
        {
            SettingsContainer.LoadSettingsProfile(GetLatestLauncherConfigPath(), true);
            CloseLauncherAutomatically = CloseLauncherAutomaticallyKey.GetValue();
            ActiveVersion = ActiveVersionKey.GetValue();
            PreferredFramework = PreferredFrameworkKey.GetValue();
            CurrentTab = CurrentTabKey.GetValue();
            DeveloperVersions = DeveloperVersionsKey.GetValue();
        }

        public static void Save()
        {
            CloseLauncherAutomaticallyKey.SetValue(CloseLauncherAutomatically);
            ActiveVersionKey.SetValue(ActiveVersion);
            PreferredFrameworkKey.SetValue(PreferredFramework);
            CurrentTabKey.SetValue(CurrentTab);
            SettingsContainer.SaveSettingsProfile(SettingsContainer.CurrentProfile, LauncherConfigPath);
        }

        public static IReadOnlyCollection<UDirectory> DeveloperVersions { get; private set; }
        
        public static bool CloseLauncherAutomatically { get; set; }
        
        public static string ActiveVersion { get; set; }

        public static string PreferredFramework { get; set; }
        
        public static int CurrentTab { get; set; }

        private static string GetLatestLauncherConfigPath()
        {
            return GetLauncherConfigPaths().FirstOrDefault(File.Exists) ?? LauncherConfigPath;
        }

        private static IEnumerable<string> GetLauncherConfigPaths()
        {
            yield return LauncherConfigPath;
        }
    }
}
