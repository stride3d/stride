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
        // The theme of this editor, under keys of its own (the other editors keep theirs elsewhere), so not in EditorSettings
        ThemeAccent = new SettingsKey<Color>("Interface/Theme/Accent", EditorSettings.SettingsContainer, default(Color))
        {
            DisplayName = $"{EditorSettings.Interface}/{Tr._p("Settings", "Theme Accent")}",
        };
        ThemeVariant = new SettingsKey<string>("Interface/Theme/Variant", EditorSettings.SettingsContainer, "Default")
        {
            DisplayName = $"{EditorSettings.Interface}/{Tr._p("Settings", "Theme Variant")}",
            GetAcceptableValues = () => ["Default", "Dark", "Light"],
        };
        // A way out on a system where the windows of the OS misbehave (the panels cannot leave the main window then)
        FloatingWindowsInsideMainWindow = new SettingsKey<bool>("Interface/FloatingWindowsInsideMainWindow", EditorSettings.SettingsContainer, false)
        {
            DisplayName = $"{EditorSettings.Interface}/{Tr._p("Settings", "Floating panels inside the main window")}",
        };
        // The user's shortcuts, by command identifier. Edited in the settings file for now: a display name without a category
        // keeps it out of the dialog
        ShortcutOverrides = new SettingsKey<Dictionary<string, string>>("Shortcuts/Overrides", EditorSettings.SettingsContainer, () => new Dictionary<string, string>())
        {
            DisplayName = "ShortcutOverrides",
        };
    }

    /// <summary>
    /// The session to open at startup when no path is given on the command line.
    /// </summary>
    public static SettingsKey<UFile> StartupSession { get; }

    public static SettingsKey<Color> ThemeAccent { get; }

    public static SettingsKey<string> ThemeVariant { get; }

    /// <summary>
    /// Whether a floating panel or editor is drawn inside the main window rather than in a window of its own.
    /// </summary>
    public static SettingsKey<bool> FloatingWindowsInsideMainWindow { get; }

    /// <summary>
    /// The shortcuts the user gave to commands, or took from them: a command identifier ("Stride.File.Save") to a shortcut
    /// ("Ctrl+Alt+S"), or to "None" (or nothing) for none. They come before the default shortcuts of the editor and of the
    /// plugins (<see cref="Core.Assets.Editor.Commands.CommandGestureBindings"/>); a command that is not loaded is skipped.
    /// </summary>
    public static SettingsKey<Dictionary<string, string>> ShortcutOverrides { get; }
}
