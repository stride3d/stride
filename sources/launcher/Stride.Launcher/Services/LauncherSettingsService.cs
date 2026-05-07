// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;

namespace Stride.Launcher.Services;

internal sealed class LauncherSettingsService : ILauncherSettingsService
{
    public bool CloseLauncherAutomatically
    {
        get => LauncherSettings.CloseLauncherAutomatically;
        set => LauncherSettings.CloseLauncherAutomatically = value;
    }

    public string ActiveVersion
    {
        get => LauncherSettings.ActiveVersion;
        set => LauncherSettings.ActiveVersion = value;
    }

    public string PreferredFramework
    {
        get => LauncherSettings.PreferredFramework;
        set => LauncherSettings.PreferredFramework = value;
    }

    public int CurrentTab
    {
        get => LauncherSettings.CurrentTab;
        set => LauncherSettings.CurrentTab = value;
    }

    public IReadOnlyCollection<UDirectory> DeveloperVersions => LauncherSettings.DeveloperVersions;

    public bool IsTaskCompleted(string taskName) => LauncherSettings.IsTaskCompleted(taskName);

    public void MarkTaskCompleted(string taskName) => LauncherSettings.MarkTaskCompleted(taskName);

    public void Save() => LauncherSettings.Save();
}
