// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
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
        ThemeAccent = new SettingsKey<Color>("Interface/Theme/Accent", SettingsContainer, default(Color))
        {
            DisplayName = $"{Interface}/{Tr._p("Settings", "Theme Accent")}",
        };
        ThemeVariant = new SettingsKey<string>("Interface/Theme/Variant", SettingsContainer, "Default")
        {
            DisplayName = $"{Interface}/{Tr._p("Settings", "Theme Variant")}",
            GetAcceptableValues = () => ["Default", "Dark", "Light"],
        };
    }

    public static SettingsKey<string> Language { get; }

    public static SettingsKey<Color> ThemeAccent { get; }

    public static SettingsKey<string> ThemeVariant { get; }

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
