// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.IO;
using Stride.Core.Settings;
using Stride.Core.VisualStudio;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.Settings
{
    public static class EditorSettings
    {
        private static SettingsProfile profile;
        public static SettingsContainer SettingsContainer = new SettingsContainer();
        /// <summary>
        /// Always delete {0} without asking
        /// </summary>
        public static readonly string AlwaysDeleteWithoutAsking = Tr._p("Settings", "Always delete {0} without asking");
        public static readonly string AlwaysSaveNewScriptsWithoutAsking = Tr._p("Settings", "Always save new scripts without asking");

        // Categories
        public static readonly string Environment = Tr._p("Settings", "Environment");
        public static readonly string ExternalTools = Tr._p("Settings", "External tools");
        public static readonly string Interface = Tr._p("Settings", "Interface");
        public static readonly string Tools = Tr._p("Settings", "Tools");

        static EditorSettings()
        {
            DefaultTextEditor = new SettingsKey<UFile>("ExternalTools/DefaultTextEditor", SettingsContainer, new UFile(@"%windir%\system32\notepad.exe"))
            {
                DisplayName = $"{ExternalTools}/{Tr._p("Settings", "Default text editor")}",
            };
            ShaderEditor = new SettingsKey<UFile>("ExternalTools/ShaderEditor", SettingsContainer, new UFile(@"%windir%\system32\notepad.exe"))
            {
                DisplayName = $"{ExternalTools}/{Tr._p("Settings", "Shader editor")}",
            };
            DefaultIDE = new SettingsKey<string>("ExternalTools/DefaultIDE", SettingsContainer, VisualStudioVersions.DefaultIDE.DisplayName)
            {
                GetAcceptableValues = () =>
                {
                    var names = new List<string> { VisualStudioVersions.DefaultIDE.DisplayName };
                    names.AddRange(VisualStudioVersions.AvailableVisualStudioInstances.Where(x => x.HasDevenv).Select(x => x.DisplayName));
                    return names;
                },
                DisplayName = $"{ExternalTools}/{Tr._p("Settings", "Default IDE")}",
            };
            AskBeforeDeletingAssets = new SettingsKey<bool>("Interface/AskBeforeDeletingAssets", SettingsContainer, true)
            {
                DisplayName = $"{Interface}/{Tr._p("Settings", "Ask before deleting assets")}",
                Description = Tr._p("Settings", "Ask before deleting assets"),
            };
            AskBeforeReloadingAssemblies = new SettingsKey<bool>("Interface/AskBeforeReloadingAssemblies", SettingsContainer, true)
            {
                DisplayName = $"{Interface}/{Tr._p("Settings", "Ask before reloading assemblies")}",
                Description = Tr._p("Settings", "Ask before reloading assemblies"),
            };
            AutoReloadAssemblies = new SettingsKey<bool>("Interface/AutoReloadAssemblies", SettingsContainer, true)
            {
                DisplayName = $"{Interface}/{Tr._p("Settings", "Automatically reload assemblies")}",
                Description = Tr._p("Settings", "Automatically reload assemblies"),
            };
            AskBeforeSavingNewScripts = new SettingsKey<bool>("Interface/AskBeforeSavingNewScripts", SettingsContainer, true)
            {
                DisplayName = $"{Interface}/{Tr._p("Settings", "Ask before saving new scripts")}",
                Description = Tr._p("Settings", "Ask before saving new scripts"),
            };
            StoreCrashEmail = new SettingsKey<string>("Interface/StoreCrashEmail", SettingsContainer, "")
            {
                DisplayName = $"{Interface}/{Tr._p("Settings", "Crash report e-mail")}",
                Description = Tr._p("Settings", "Crash report e-mail"),
            };
            Language = new SettingsKey<SupportedLanguage>("Interface/Language", SettingsContainer, SupportedLanguage.MachineDefault)
            {
                DisplayName = $"{Interface}/{Tr._p("Settings", "Language")}",
            };
            ResetEditorLayout = new SettingsCommand("Interface/ResetEditorLayout")
            {
                ActionName = Tr._p("Settings", "Reset"),
                DisplayName = $"{Interface}/{Tr._p("Settings", "Reset Game Studio layout")}"
            };
            FallbackBuildCacheDirectory = new UDirectory(Path.Combine(EditorPath.DefaultTempPath, "BuildCache"));

            UseEffectCompilerServer = new SettingsKey<bool>("Tools/UseEffectCompilerServer", SettingsContainer, false)
            {
                DisplayName = $"{Tools}/{Tr._p("Settings", "Use effect compiler server for mobile platforms")}",
            };
            ReloadLastSession = new SettingsKey<bool>("Interface/ReloadLastSession", SettingsContainer, false)
            {
                DisplayName = $"{Interface}/{Tr._p("Settings", "Automatically reload last session at startup")}",
            };
        }

        public static SettingsKey<UFile> DefaultTextEditor { get; }

        public static SettingsKey<UFile> ShaderEditor { get; }

        public static SettingsKey<string> DefaultIDE { get; }

        public static SettingsKey<bool> AskBeforeDeletingAssets { get; }

        public static SettingsKey<bool> AskBeforeReloadingAssemblies { get; }

        public static SettingsKey<bool> AutoReloadAssemblies { get; }

        public static SettingsKey<bool> AskBeforeSavingNewScripts { get; }

        public static SettingsKey<string> StoreCrashEmail { get; }

        public static SettingsKey<SupportedLanguage> Language { get; }

        public static SettingsCommand ResetEditorLayout { get; }

        public static UDirectory FallbackBuildCacheDirectory { get; }

        public static SettingsKey<bool> UseEffectCompilerServer { get; }

        public static SettingsKey<bool> ReloadLastSession { get; }

        public static bool NeedRestart { get; set; }

        public static void Initialize()
        {
            profile = SettingsContainer.LoadSettingsProfile(EditorPath.EditorConfigPath, true) ?? SettingsContainer.CreateSettingsProfile(true);
            Presentation.Themes.ThemesSettings.Initialize();

            // Settings that requires a restart must register here:
            UseEffectCompilerServer.ChangesValidated += (s, e) => NeedRestart = true;
            Language.ChangesValidated += (s, e) => NeedRestart = true;

            Presentation.Themes.ThemesSettings.ThemeName.ChangesValidated += (s, e) => NeedRestart = true;
        }

        public static void Save()
        {
            SettingsContainer.SaveSettingsProfile(profile, EditorPath.EditorConfigPath);
        }

        [NotNull]
        public static IEnumerable<SettingsCommand> GetAllCommands()
        {
            yield return ResetEditorLayout;
        }
    }
}
