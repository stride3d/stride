// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Settings;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.Settings;

public static class EditorSettings
{
    // One container with the keys declared in the shared projects (ExternalToolsSettings...), which the WPF editor reads too
    public static SettingsContainer SettingsContainer { get; } = SharedEditorSettings.SettingsContainer;

    public static readonly string AlwaysDeleteWithoutAsking = Tr._p("Settings", "Always delete {0} without asking");
    public static readonly string AlwaysSaveNewScriptsWithoutAsking = Tr._p("Settings", "Always save new scripts without asking");

    // Categories
    public static readonly string Environment = Tr._p("Settings", "Environment");
    public static readonly string Interface = Tr._p("Settings", "Interface");
    public static readonly string Logging = Tr._p("Settings", "Logging");
    public static readonly string Tools = Tr._p("Settings", "Tools");

    public const string GraphicsApiDefault = "Default";

    static EditorSettings()
    {
        // The External tools keys are declared in the shared project
        RuntimeHelpers.RunClassConstructor(typeof(ExternalToolsSettings).TypeHandle);
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
        ResetEditorLayout = new SettingsCommand("Interface/ResetEditorLayout")
        {
            ActionName = Tr._p("Settings", "Reset"),
            DisplayName = $"{Interface}/{Tr._p("Settings", "Reset Game Studio layout")}"
        };
        ReloadLastSession = new SettingsKey<bool>("Interface/ReloadLastSession", SettingsContainer, false)
        {
            DisplayName = $"{Interface}/{Tr._p("Settings", "Automatically reload last session at startup")}",
        };
        FallbackBuildCacheDirectory = new UDirectory(Path.Combine(EditorPath.DefaultTempPath, "BuildCache"));
        UseEffectCompilerServer = new SettingsKey<bool>("Tools/UseEffectCompilerServer", SettingsContainer, false)
        {
            DisplayName = $"{Tools}/{Tr._p("Settings", "Use effect compiler server for mobile platforms")}",
        };
        GraphicsApi = new SettingsKey<string>("Environment/GraphicsApi", SettingsContainer, GraphicsApiDefault)
        {
            DisplayName = $"{Environment}/{Tr._p("Settings", "Graphics API (Game Studio only)")}",
            // "Default" follows the platform default; Windows also offers all three explicitly, other platforms only Vulkan.
            GetAcceptableValues = () => OperatingSystem.IsWindows()
                ? [GraphicsApiDefault, "Direct3D11", "Direct3D12", "Vulkan"]
                : [GraphicsApiDefault, "Vulkan"],
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
            ["EffectCompilerCache"] = LogMessageType.Debug,
            ["Preview"] = LogMessageType.Debug,
            [GraphViewModel.DefaultLoggerName] = LogMessageType.Debug,
        })
        {
            DisplayName = "SourceModuleLevels",
        };
    }

    public static Dictionary<string, LogMessageType> GetModuleLevels(SettingsKey<Dictionary<string, LogMessageType>> key)
    {
        var levels = key.DefaultValue;
        foreach (var (module, level) in key.GetValue() ?? [])
            levels[module] = level;
        return levels;
    }

    public static SettingsKey<bool> AskBeforeDeletingAssets { get; }

    public static SettingsKey<bool> AskBeforeReloadingAssemblies { get; }

    public static SettingsKey<bool> AutoReloadAssemblies { get; }

    public static SettingsKey<bool> AskBeforeSavingNewScripts { get; }

    public static SettingsKey<string> Language { get; }

    public static SettingsKey<Color> ThemeAccent { get; }

    public static SettingsKey<string> ThemeVariant { get; }

    public static SettingsCommand ResetEditorLayout { get; }

    public static SettingsKey<bool> ReloadLastSession { get; }

    public static UDirectory FallbackBuildCacheDirectory { get; }

    public static SettingsKey<bool> UseEffectCompilerServer { get; }

    /// <summary>
    /// The graphics API Game Studio itself loads; applied at startup, so it needs a restart.
    /// </summary>
    public static SettingsKey<string> GraphicsApi { get; }

    // The Logging keys are read once at Game Studio startup, so they need a restart.
    public static SettingsKey<LogMessageType> DebugOutputLevel { get; }

    public static SettingsKey<Dictionary<string, LogMessageType>> DebugOutputModuleLevels { get; }

    public static SettingsKey<Dictionary<string, LogMessageType>> SourceModuleLevels { get; }

    public static bool NeedRestart { get; set; }

    public static void Initialize()
    {
        SharedEditorSettings.Load(EditorPath.EditorConfigPath);

        // Settings that requires a restart must register here
        UseEffectCompilerServer.ChangesValidated += (_, _) => NeedRestart = true;
        Language.ChangesValidated += (_, _) => NeedRestart = true;
        GraphicsApi.ChangesValidated += (_, _) => NeedRestart = true;
        DebugOutputLevel.ChangesValidated += (_, _) => NeedRestart = true;
    }

    public static void Save()
    {
        SharedEditorSettings.Save();
    }

    public static IEnumerable<SettingsCommand> GetAllCommands()
    {
        yield return ResetEditorLayout;
    }
}
