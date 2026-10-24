// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Settings;

namespace Stride.Core.Assets.Editor.Settings;

public static class InternalSettings
{
    private static readonly SettingsProfile profile;

    static InternalSettings()
    {
        profile = LoadProfile(true);
        SettingsContainer.CurrentProfile = profile;
    }

    public static SettingsContainer SettingsContainer { get; } = new();

    /// <summary>
    /// Loads the settings from the file.
    /// </summary>
    /// <returns></returns>
    private static SettingsProfile LoadProfile(bool registerProfile)
    {
        return SettingsContainer.LoadSettingsProfile(EditorPath.InternalConfigPath, false, null, registerProfile) ?? SettingsContainer.CreateSettingsProfile(false);
    }

    /// <summary>
    /// Saves the settings into the settings file.
    /// </summary>
    public static void SaveProfile()
    {
        SettingsContainer.SaveSettingsProfile(profile, EditorPath.InternalConfigPath);
    }
}
