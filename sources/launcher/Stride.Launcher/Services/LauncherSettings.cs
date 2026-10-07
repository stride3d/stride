// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor;
using Stride.Core.IO;
using Stride.Core.Settings;

namespace Stride.Launcher.Services;

public static class LauncherSettings
{
    private static readonly SettingsContainer SettingsContainer = new();

    // Preferences, chosen by the user
    private static readonly SettingsKey<bool> CloseLauncherAutomaticallyKey = new("Launcher/CloseLauncherAutomatically", SettingsContainer, false);
    private static readonly SettingsKey<string> PreferredEditorKey = new("Launcher/PreferredEditor", SettingsContainer, "");
    private static readonly SettingsKey<string> PreferredRuntimeKey = new("Launcher/PreferredRuntime", SettingsContainer, "");
    private static readonly SettingsKey<List<UDirectory>> DeveloperVersionsKey = new("Launcher/DeveloperVersions", SettingsContainer, () => new List<UDirectory>());
    private static readonly SettingsKey<bool> IncludePrereleaseUpdatesKey = new("Launcher/IncludePrereleaseUpdates", SettingsContainer, false);
    private static readonly SettingsKey<string> ThemeVariantKey = new("Launcher/ThemeVariant", SettingsContainer, "Dark");

    // State the launcher remembers by itself
    private static readonly SettingsKey<string> ActiveVersionKey = new("Internal/Launcher/ActiveVersion", SettingsContainer, "");
    private static readonly SettingsKey<int> CurrentTabKey = new("Internal/Launcher/CurrentTabSessions", SettingsContainer, 0);
    private static readonly SettingsKey<List<string>> CompletedTasksKey = new("Internal/Launcher/CompletedTasks", SettingsContainer, () => new List<string>());

    // Before 6.0.1: read when the new key isn't saved yet, e.g. after an update from 5.x, and removed at the next save
    private static readonly SettingsKey<bool> LegacyCloseLauncherAutomaticallyKey = new("Internal/Launcher/CloseLauncherAutomatically", SettingsContainer, false);

    private static readonly string LauncherConfigPath = Path.Combine(EditorPath.UserDataPath, "LauncherSettings.conf");

    private static List<string> completedTasks = [];

    static LauncherSettings()
    {
        SettingsContainer.LoadSettingsProfile(GetLatestLauncherConfigPath(), true);
        CloseLauncherAutomatically = SettingsContainer.CurrentProfile.ContainsKey(CloseLauncherAutomaticallyKey)
            ? CloseLauncherAutomaticallyKey.GetValue()
            : LegacyCloseLauncherAutomaticallyKey.GetValue();
        ActiveVersion = ActiveVersionKey.GetValue();
        PreferredEditor = PreferredEditorKey.GetValue();
        PreferredRuntime = PreferredRuntimeKey.GetValue();
        CurrentTab = CurrentTabKey.GetValue();
        DeveloperVersions = DeveloperVersionsKey.GetValue();
        completedTasks = CompletedTasksKey.GetValue();
        IncludePrereleaseUpdates = IncludePrereleaseUpdatesKey.GetValue();
        ThemeVariant = SettingsContainer.CurrentProfile.ContainsKey(ThemeVariantKey) ? ThemeVariantKey.GetValue() : null;
    }

    public static void Save()
    {
        CloseLauncherAutomaticallyKey.SetValue(CloseLauncherAutomatically);
        ActiveVersionKey.SetValue(ActiveVersion);
        PreferredEditorKey.SetValue(PreferredEditor);
        PreferredRuntimeKey.SetValue(PreferredRuntime);
        CurrentTabKey.SetValue(CurrentTab);
        CompletedTasksKey.SetValue(completedTasks);
        IncludePrereleaseUpdatesKey.SetValue(IncludePrereleaseUpdates);
        if (ThemeVariant is not null)
            ThemeVariantKey.SetValue(ThemeVariant);
        else
            SettingsContainer.CurrentProfile.Remove(ThemeVariantKey);
        SettingsContainer.CurrentProfile.Remove(LegacyCloseLauncherAutomaticallyKey);
        SettingsContainer.SaveSettingsProfile(SettingsContainer.CurrentProfile, LauncherConfigPath);
    }

    public static IReadOnlyCollection<UDirectory> DeveloperVersions { get; private set; }

    public static bool CloseLauncherAutomatically { get; set; }

    public static string ActiveVersion { get; set; }

    public static string PreferredEditor { get; set; }

    /// <summary>.NET major to start Game Studio on; empty = the one the project needs.</summary>
    public static string PreferredRuntime { get; set; }

    public static int CurrentTab { get; set; }

    /// <summary>Whether the launcher updates itself to pre-release versions too.</summary>
    public static bool IncludePrereleaseUpdates { get; set; }

    /// <summary>
    /// The theme variant: Dark, Light (not tuned yet, shown as a preview), or System (follows the light or dark mode of
    /// the system). Null until the user picks one: the launcher's default then (Dark), and a later change of that default
    /// reaches them, as it isn't saved.
    /// </summary>
    public static string? ThemeVariant { get; set; }

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
