// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Settings;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.Settings;

public static class EditorSettings
{
    private static SettingsProfile? profile;
    public static SettingsContainer SettingsContainer { get; } = new();

    // Categories
    public static readonly string Interface = Tr._p("Settings", "Interface");

    static EditorSettings()
    {
        Language = new SettingsKey<string>("Interface/Language", SettingsContainer, "MachineDefault")
        {
            DisplayName = $"{Interface}/{Tr._p("Settings", "Language")}",
        };
    }

    public static SettingsKey<string> Language { get; }

    public static bool NeedRestart { get; set; }

    public static void Initialize()
    {
        profile = SettingsContainer.LoadSettingsProfile(EditorPath.EditorConfigPath, true) ?? SettingsContainer.CreateSettingsProfile(true);

        // Settings that requires a restart must register here
        Language.ChangesValidated += (_, _) => NeedRestart = true;
    }

    public static void Save()
    {
        SettingsContainer.SaveSettingsProfile(profile!, EditorPath.EditorConfigPath);
    }
}
