// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Settings;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Settings;
using Stride.Core.Translation;
using Stride.GameStudio.Avalonia.Helpers;

namespace Stride.GameStudio.Avalonia.Settings;

/// <summary>
/// The user settings of the Game Studio shell (the editor layers declare theirs in <see cref="EditorSettings"/>).
/// </summary>
internal static class GameStudioEditorSettings
{
    static GameStudioEditorSettings()
    {
        // Note: the name of this setting is based on the editor version, because we want to force displaying the release notes on a new version.
        StartupSession = new SettingsKey<UFile>("Interface/StartupSession" + StrideGameStudio.EditorVersion, EditorSettings.SettingsContainer, "")
        {
            DisplayName = $"{EditorSettings.Interface}/{Tr._p("Settings", "Default session to load")}"
        };
        // The theme of this editor: the WPF Game Studio has its own (Themes/ThemeName), so these keys are not in EditorSettings
        ThemeAccent = new SettingsKey<Color>("Interface/Theme/Accent", EditorSettings.SettingsContainer, default(Color))
        {
            DisplayName = $"{EditorSettings.Interface}/{Tr._p("Settings", "Theme Accent")}",
        };
        ThemeVariant = new SettingsKey<string>("Interface/Theme/Variant", EditorSettings.SettingsContainer, "Default")
        {
            DisplayName = $"{EditorSettings.Interface}/{Tr._p("Settings", "Theme Variant")}",
            GetAcceptableValues = () => ["Default", "Dark", "Light"],
        };
    }

    /// <summary>
    /// The session to open at startup when no path is given on the command line.
    /// </summary>
    public static SettingsKey<UFile> StartupSession { get; }

    public static SettingsKey<Color> ThemeAccent { get; }

    public static SettingsKey<string> ThemeVariant { get; }
}
