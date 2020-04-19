// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.IO;
using Stride.Core.Settings;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Translation;

namespace Stride.GameStudio
{
    public static class StrideEditorSettings
    {
        // Categories
        public static readonly string Remote = Tr._p("Settings", "Remote");

        static StrideEditorSettings()
        {
            // Note: assignments cannot be moved to initializer, because category names need to be assigned first.
            StartupSession = new SettingsKey<UFile>("Interface/StartupSession" + StrideGameStudio.EditorVersion, EditorSettings.SettingsContainer, "")
            {
                // Note: the name of this settings is based on the editor version, because we want to force displaying the release notes on a new version.
                DisplayName = $"{EditorSettings.Interface}/{Tr._p("Settings", "Default session to load")}"
            };
            Host = new SettingsKey<string>("Remote/Credentials/Host", EditorSettings.SettingsContainer, "")
            {
                DisplayName = $"{Remote}/{Tr._p("Settings", "Host")}"
            };
            Port = new SettingsKey<int>("Remote/Credentials/Port", EditorSettings.SettingsContainer, 22)
            {
                DisplayName = $"{Remote}/{Tr._p("Settings", "Port")}"
            };
            Username = new SettingsKey<string>("Remote/Credentials/Username", EditorSettings.SettingsContainer, "")
            {
                DisplayName = $"{Remote}/{Tr._p("Settings", "Username")}"
            };
            Password = new SettingsKey<string>("Remote/Credentials/Password", EditorSettings.SettingsContainer, "")
            {
                DisplayName = $"{Remote}/{Tr._p("Settings", "Password")}"
            };
            Location = new SettingsKey<string>("Remote/Location", EditorSettings.SettingsContainer, "Projects")
            {
                DisplayName = $"{Remote}/{Tr._p("Settings", "Location")}"
            };
            AskForCredentials = new SettingsKey<bool>("Remote/Credentials/AskForCredentials", EditorSettings.SettingsContainer, true)
            {
                DisplayName = $"{Remote}/{Tr._p("Settings", "Ask for credentials when deploying game")}"
            };
            Display = new SettingsKey<string>("Remote/Display", EditorSettings.SettingsContainer, ":0.0")
            {
                DisplayName = $"{Remote}/{Tr._p("Settings", "X Display")}"
            };
            UseCoreCLR = new SettingsKey<bool>("Remote/UseCoreCLR", EditorSettings.SettingsContainer, false)
            {
                DisplayName = $"{Remote}/{Tr._p("Settings", "Use CoreCLR")}"
            };
        }

        public static SettingsKey<UFile> StartupSession { get; }

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
        /// Encrypted password saved in Base64 to connect to remote <seealso cref="Host"/>. Use <seealso cref="RemoteFacilities"/> to decrypt it.
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

        /// <summary>
        /// Name of display where game will be launched on remote <seealso cref="Host"/>.
        /// </summary>
        public static SettingsKey<bool> UseCoreCLR { get; }

        /// <summary>
        /// Save settings
        /// </summary>
        public static void Save()
        {
            EditorSettings.Save();
        }
    }
}
