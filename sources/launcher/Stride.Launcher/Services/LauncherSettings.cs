// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor;
using Stride.Core.IO;
using Stride.Core.Settings;

namespace Stride.Launcher.Services;

public static class LauncherSettings
{
    private static readonly SettingsContainer SettingsContainer = new();

    private static readonly SettingsKey<bool> CloseLauncherAutomaticallyKey = new("Internal/Launcher/CloseLauncherAutomatically", SettingsContainer, false);
    private static readonly SettingsKey<string> ActiveVersionKey = new("Internal/Launcher/ActiveVersion", SettingsContainer, "");
    private static readonly SettingsKey<string> PreferredFrameworkKey = new("Internal/Launcher/PreferredFramework", SettingsContainer, "net10.0");
    private static readonly SettingsKey<int> CurrentTabKey = new("Internal/Launcher/CurrentTabSessions", SettingsContainer, 0);
    private static readonly SettingsKey<List<UDirectory>> DeveloperVersionsKey = new("Internal/Launcher/DeveloperVersions", SettingsContainer, () => new List<UDirectory>());
    private static readonly SettingsKey<List<string>> CompletedTasksKey = new("Internal/Launcher/CompletedTasks", SettingsContainer, () => new List<string>());

    private static readonly string LauncherConfigPath = Path.Combine(EditorPath.UserDataPath, "LauncherSettings.conf");

    private static List<string> completedTasks = [];

    static LauncherSettings()
    {
        SettingsContainer.LoadSettingsProfile(GetLatestLauncherConfigPath(), true);
        CloseLauncherAutomatically = CloseLauncherAutomaticallyKey.GetValue();
        ActiveVersion = ActiveVersionKey.GetValue();
        PreferredFramework = PreferredFrameworkKey.GetValue();
        CurrentTab = CurrentTabKey.GetValue();
        DeveloperVersions = DeveloperVersionsKey.GetValue();
        completedTasks = CompletedTasksKey.GetValue();
    }

    public static void Save()
    {
        CloseLauncherAutomaticallyKey.SetValue(CloseLauncherAutomatically);
        ActiveVersionKey.SetValue(ActiveVersion);
        PreferredFrameworkKey.SetValue(PreferredFramework);
        CurrentTabKey.SetValue(CurrentTab);
        CompletedTasksKey.SetValue(completedTasks);
        SettingsContainer.SaveSettingsProfile(SettingsContainer.CurrentProfile, LauncherConfigPath);
    }

    public static IReadOnlyCollection<UDirectory> DeveloperVersions { get; private set; }

    public static bool CloseLauncherAutomatically { get; set; }

    public static string ActiveVersion { get; set; }

    public static string PreferredFramework { get; set; }

    public static int CurrentTab { get; set; }

    public static IReadOnlyCollection<string> CompletedTasks => completedTasks;

    public static bool IsTaskCompleted(string taskName) => completedTasks.Contains(taskName);

    public static void MarkTaskCompleted(string taskName)
    {
        if (!completedTasks.Contains(taskName))
        {
            completedTasks.Add(taskName);
            Save();
        }
    }

    private static string GetLatestLauncherConfigPath()
    {
        return GetLauncherConfigPaths().FirstOrDefault(File.Exists) ?? LauncherConfigPath;
    }

    private static IEnumerable<string> GetLauncherConfigPaths()
    {
        yield return LauncherConfigPath;
    }
}
