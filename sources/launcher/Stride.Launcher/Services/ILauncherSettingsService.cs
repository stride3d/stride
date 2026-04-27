// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;

namespace Stride.Launcher.Services;

public interface ILauncherSettingsService
{
    bool CloseLauncherAutomatically { get; set; }
    string ActiveVersion { get; set; }
    string PreferredFramework { get; set; }
    int CurrentTab { get; set; }
    IReadOnlyCollection<UDirectory> DeveloperVersions { get; }
    bool IsTaskCompleted(string taskName);
    /// <summary>Marks the task as completed and persists immediately (same contract as <see cref="LauncherSettings.MarkTaskCompleted"/>).</summary>
    void MarkTaskCompleted(string taskName);
    void Save();
}
