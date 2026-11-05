// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;
using Stride.Core.Settings;

#nullable enable

namespace Stride.Core.Assets.Editor.Settings;

/// <summary>
/// The user settings of Game Studio (<c>GameStudioSettings.conf</c>), which both editors read and write: the keys declared in the
/// shared projects (<see cref="ExternalToolsSettings"/>, the remote, scene editor and script editor settings) and those of each
/// editor's <c>EditorSettings</c> live in this one container, so a key has one declaration for both editors.
/// </summary>
public static class SharedEditorSettings
{
    private static SettingsProfile? profile;
    private static UFile? filePath;

    /// <summary>
    /// The container of the user settings; each editor's <c>EditorSettings.SettingsContainer</c> is this one.
    /// </summary>
    public static SettingsContainer SettingsContainer { get; } = new();

    /// <summary>
    /// Loads the settings file as the current profile, or starts an empty profile when there is none; <see cref="Save"/> writes it back.
    /// </summary>
    /// <param name="path">The settings file.</param>
    public static void Load(UFile path)
    {
        filePath = path;
        profile = SettingsContainer.LoadSettingsProfile(path, true) ?? SettingsContainer.CreateSettingsProfile(true);
    }

    /// <summary>
    /// Writes the current profile to the file it was loaded from; nothing before <see cref="Load"/>. The values of the keys this
    /// editor does not declare (the other editor's) are written back as they were read.
    /// </summary>
    public static void Save()
    {
        if (profile is not null && filePath is not null)
            SettingsContainer.SaveSettingsProfile(profile, filePath);
    }
}
