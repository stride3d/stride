// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.CompilerServices;
using Stride.Core.IO;
using Stride.Core.Settings;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Translation;
using Stride.Editor.Settings;
using Stride.GameStudio.Remote;
using Stride.GameStudio.Helpers;

namespace Stride.GameStudio
{
    public static class StrideEditorSettings
    {
        // Categories
        public static readonly string Remote = RemoteSettings.Remote;

        static StrideEditorSettings()
        {
            // Note: assignments cannot be moved to initializer, because category names need to be assigned first.
            StartupSession = new SettingsKey<UFile>("Interface/StartupSession" + StrideGameStudio.EditorVersion, EditorSettings.SettingsContainer, "")
            {
                // Note: the name of this settings is based on the editor version, because we want to force displaying the release notes on a new version.
                DisplayName = $"{EditorSettings.Interface}/{Tr._p("Settings", "Default session to load")}"
            };
            // The Remote keys are declared in the shared project
            RuntimeHelpers.RunClassConstructor(typeof(RemoteSettings).TypeHandle);
        }

        public static SettingsKey<UFile> StartupSession { get; }

        /// <summary>
        /// Remote host we are interacting with by default.
        /// </summary>
        public static SettingsKey<string> Host => RemoteSettings.Host;

        /// <summary>
        /// Port to connect to remote <seealso cref="Host"/>.
        /// </summary>
        public static SettingsKey<int> Port => RemoteSettings.Port;

        /// <summary>
        /// Username to connect to remote <seealso cref="Host"/>.
        /// </summary>
        public static SettingsKey<string> Username => RemoteSettings.Username;

        /// <summary>
        /// Encrypted password saved in Base64 to connect to remote <seealso cref="Host"/>. Use <seealso cref="RemoteFacilities"/> to decrypt it.
        /// </summary>
        public static SettingsKey<string> Password => RemoteSettings.Password;

        /// <summary>
        /// Location on remote <seealso cref="Host"/> where files will be saved.
        /// </summary>
        public static SettingsKey<string> Location => RemoteSettings.Location;

        /// <summary>
        /// When launching a game on a remote <seealso cref="Host"/> specifies if dialog asking for credentials should be shown.
        /// </summary>
        public static SettingsKey<bool> AskForCredentials => RemoteSettings.AskForCredentials;

        /// <summary>
        /// Name of display where game will be launched on remote <seealso cref="Host"/>.
        /// </summary>
        public static SettingsKey<string> Display => RemoteSettings.Display;

        /// <summary>
        /// Save settings
        /// </summary>
        public static void Save()
        {
            EditorSettings.Save();
        }
    }
}
