// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.CodeEditorSupport;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Settings;
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
        public static readonly string Logging = Tr._p("Settings", "Logging");
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
            DefaultIDE = new SettingsKey<string>("ExternalTools/DefaultIDE", SettingsContainer, IDEInfo.DefaultIDE.DisplayName)
            {
                GetAcceptableValues = () =>
                {
                    var names = new List<string> { IDEInfo.DefaultIDE.DisplayName };
                    names.AddRange(IDEInfoVersions.AvailableIDEs().Where(x => x.HasProgram).Select(x => x.DisplayName));
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
            GraphicsApi = new SettingsKey<string>("Environment/GraphicsApi", SettingsContainer, GraphicsApiDefault)
            {
                DisplayName = $"{Environment}/{Tr._p("Settings", "Graphics API (Game Studio only)")}",
                // "Default" follows the platform default; Windows also offers all three explicitly, other platforms only Vulkan.
                GetAcceptableValues = () => OperatingSystem.IsWindows()
                    ? new List<string> { GraphicsApiDefault, "Direct3D11", "Direct3D12", "Vulkan" }
                    : new List<string> { GraphicsApiDefault, "Vulkan" },
            };
            DebugOutputLevel = new SettingsKey<LogMessageType>("Logging/DebugOutputLevel", SettingsContainer, LogMessageType.Warning)
            {
                DisplayName = $"{Logging}/{Tr._p("Settings", "Debugger output level (Game Studio only)")}",
                Description = Tr._p("Settings", "Minimum level of the messages Game Studio writes to the attached debugger's output"),
            };
            // The module maps are edited in the settings file only: a display name without a category keeps them out of the dialog.
            DebugOutputModuleLevels = new SettingsKey<Dictionary<string, LogMessageType>>("Logging/DebugOutputModuleLevels", SettingsContainer, () => new Dictionary<string, LogMessageType>
            {
                // The graphics device and its validation layer (GraphicsDevice.DebugLogModule): quiet without a debug device.
                ["GraphicsDevice"] = LogMessageType.Debug,
                ["GraphicsDebug"] = LogMessageType.Debug,
            })
            {
                DisplayName = "DebugOutputModuleLevels",
            };
            SourceModuleLevels = new SettingsKey<Dictionary<string, LogMessageType>>("Logging/SourceModuleLevels", SettingsContainer, () => new Dictionary<string, LogMessageType>
            {
                // The global loggers shown in the debug pages need their full history.
                ["AssetBuilderService"] = LogMessageType.Debug,
                ["EffectCompilerCache"] = LogMessageType.Debug,
                ["Preview"] = LogMessageType.Debug,
                [GraphViewModel.DefaultLoggerName] = LogMessageType.Debug,
            })
            {
                DisplayName = "SourceModuleLevels",
            };
        }

        /// <summary>
        /// Merges the configured entries of a module-level key over its defaults, so an entry in the settings file
        /// overrides one module without having to repeat the others.
        /// </summary>
        public static Dictionary<string, LogMessageType> GetModuleLevels(SettingsKey<Dictionary<string, LogMessageType>> key)
        {
            var levels = key.DefaultValue;
            foreach (var (module, level) in key.GetValue() ?? [])
                levels[module] = level;
            return levels;
        }

        public static SettingsKey<UFile> DefaultTextEditor { get; }

        public static SettingsKey<UFile> ShaderEditor { get; }

        public static SettingsKey<string> DefaultIDE { get; }

        public static SettingsKey<bool> AskBeforeDeletingAssets { get; }

        public static SettingsKey<bool> AskBeforeReloadingAssemblies { get; }

        public static SettingsKey<bool> AutoReloadAssemblies { get; }

        public static SettingsKey<bool> AskBeforeSavingNewScripts { get; }

        public static SettingsKey<SupportedLanguage> Language { get; }

        public static SettingsCommand ResetEditorLayout { get; }

        public static UDirectory FallbackBuildCacheDirectory { get; }

        public static SettingsKey<bool> UseEffectCompilerServer { get; }

        public static SettingsKey<bool> ReloadLastSession { get; }

        /// <summary>Value meaning "follow the platform default" for <see cref="GraphicsApi"/>.</summary>
        public const string GraphicsApiDefault = "Default";

        // Graphics API Game Studio itself loads; applied at startup by GraphicsApiSelector, so it needs a restart.
        public static SettingsKey<string> GraphicsApi { get; }

        // The Logging keys are read once at Game Studio startup, so they need a restart.

        /// <summary>
        /// Minimum level of the messages Game Studio writes to the attached debugger's output (Debug builds only).
        /// </summary>
        public static SettingsKey<LogMessageType> DebugOutputLevel { get; }

        /// <summary>
        /// Per-module overrides of <see cref="DebugOutputLevel"/>, keyed by log module: a module can be followed in
        /// detail while the rest stays at the general level. Entries merge over the defaults (see <see cref="GetModuleLevels"/>);
        /// edited in the settings file only.
        /// </summary>
        public static SettingsKey<Dictionary<string, LogMessageType>> DebugOutputModuleLevels { get; }

        /// <summary>
        /// Minimum level at which the global loggers themselves emit, keyed by log module; below it a message is never
        /// created, whatever listens. Defaults cover the loggers shown in the debug window (Help > Show debug window), so
        /// its pages get their full history. Entries merge over the defaults (see <see cref="GetModuleLevels"/>); edited in
        /// the settings file only.
        /// </summary>
        public static SettingsKey<Dictionary<string, LogMessageType>> SourceModuleLevels { get; }

        public static bool NeedRestart { get; set; }

        public static void Initialize()
        {
            profile = SettingsContainer.LoadSettingsProfile(EditorPath.EditorConfigPath, true) ?? SettingsContainer.CreateSettingsProfile(true);
            Presentation.Themes.ThemesSettings.Initialize();

            // Settings that requires a restart must register here:
            UseEffectCompilerServer.ChangesValidated += (s, e) => NeedRestart = true;
            Language.ChangesValidated += (s, e) => NeedRestart = true;
            GraphicsApi.ChangesValidated += (s, e) => NeedRestart = true;
            DebugOutputLevel.ChangesValidated += (s, e) => NeedRestart = true;

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
