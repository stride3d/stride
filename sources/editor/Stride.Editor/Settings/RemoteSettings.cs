// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Settings;
using Stride.Core.Translation;

#nullable enable

namespace Stride.Editor.Settings;

/// <summary>
/// The Remote settings: the host where a game is deployed and started from the editor.
/// </summary>
public static class RemoteSettings
{
    // Categories
    public static readonly string Remote = Tr._p("Settings", "Remote");

    static RemoteSettings()
    {
        // The category name is assigned first
        Host = new SettingsKey<string>("Remote/Credentials/Host", SharedEditorSettings.SettingsContainer, "")
        {
            DisplayName = $"{Remote}/{Tr._p("Settings", "Host")}"
        };
        Port = new SettingsKey<int>("Remote/Credentials/Port", SharedEditorSettings.SettingsContainer, 22)
        {
            DisplayName = $"{Remote}/{Tr._p("Settings", "Port")}"
        };
        Username = new SettingsKey<string>("Remote/Credentials/Username", SharedEditorSettings.SettingsContainer, "")
        {
            DisplayName = $"{Remote}/{Tr._p("Settings", "Username")}"
        };
        Password = new SettingsKey<string>("Remote/Credentials/Password", SharedEditorSettings.SettingsContainer, "")
        {
            DisplayName = $"{Remote}/{Tr._p("Settings", "Password")}"
        };
        Location = new SettingsKey<string>("Remote/Location", SharedEditorSettings.SettingsContainer, "Projects")
        {
            DisplayName = $"{Remote}/{Tr._p("Settings", "Location")}"
        };
        AskForCredentials = new SettingsKey<bool>("Remote/Credentials/AskForCredentials", SharedEditorSettings.SettingsContainer, true)
        {
            DisplayName = $"{Remote}/{Tr._p("Settings", "Ask for credentials when deploying game")}"
        };
        Display = new SettingsKey<string>("Remote/Display", SharedEditorSettings.SettingsContainer, ":0.0")
        {
            DisplayName = $"{Remote}/{Tr._p("Settings", "X Display")}"
        };
    }

    /// <summary>
    /// Remote host we are interacting with by default.
    /// </summary>
    public static SettingsKey<string> Host { get; }

    /// <summary>
    /// Port to connect to remote <seealso cref="Host"/>.
    /// </summary>
    public static SettingsKey<int> Port { get; }

    /// <summary>
    /// Username to connect to remote <seealso cref="Host"/>.
    /// </summary>
    public static SettingsKey<string> Username { get; }

    /// <summary>
    /// Encrypted password saved in Base64 to connect to remote <seealso cref="Host"/>.
    /// </summary>
    public static SettingsKey<string> Password { get; }

    /// <summary>
    /// Location on remote <seealso cref="Host"/> where files will be saved.
    /// </summary>
    public static SettingsKey<string> Location { get; }

    /// <summary>
    /// When launching a game on a remote <seealso cref="Host"/> specifies if dialog asking for credentials should be shown.
    /// </summary>
    public static SettingsKey<bool> AskForCredentials { get; }

    /// <summary>
    /// Name of display where game will be launched on remote <seealso cref="Host"/>.
    /// </summary>
    public static SettingsKey<string> Display { get; }
}
