// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using MarkView.Avalonia;
using MarkView.Avalonia.Rendering;
using Stride.Assets;
using Stride.Assets.Editor;
using Stride.Assets.Templates;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Presentation.Avalonia.Extensions;
using Stride.Core.Presentation.Avalonia.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Settings;
using Stride.Core.Translation;
using Stride.Editor.Avalonia.Preview;
using Stride.Editor.Thumbnails;
using Stride.GameStudio.Avalonia.Services;
using Stride.GameStudio.Avalonia.Settings;
using Stride.GameStudio.Avalonia.ViewModels;
using Stride.GameStudio.Avalonia.Views;
using EditorSettings = Stride.Core.Assets.Editor.Settings.EditorSettings;

namespace Stride.GameStudio.Avalonia;

public partial class App : Application
{
    private static readonly Logger Log = GlobalLogger.GetLogger("GameStudio");

    /// <summary>
    /// What the command line asked: set by the desktop host (<c>Program</c>) before the application starts.
    /// </summary>
    public static StartupOptions Options { get; set; } = new();

    /// <summary>
    /// When non-null, called once after the main window is first loaded to notify the launcher
    /// that Game Studio has started. Set by <c>Program.cs</c> in the Desktop project.
    /// </summary>
    public static Action? LauncherNotifier;

    /// <summary>
    /// Starts a new editor process, to open another session once this one has loaded one (see <see cref="EditorProcessService"/>).
    /// Set by the desktop host; without one, the editor restarts in its own process.
    /// </summary>
    public static IEditorProcessLauncher? ProcessLauncher { get; set; }

    private static EditorProcessService? processService;
    private IViewModelServiceProvider? serviceProvider;
    private MainViewModel? mainViewModel;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        InitializePlugins();

        EditorSettings.Initialize();
        InitializeLogging();
        InitializeLanguage();
        SetAccent(EditorSettings.ThemeAccent.GetValue().ToAvaloniaColor());
        SetVariant(EditorSettings.ThemeVariant.GetValue());
        EditorSettings.ThemeAccent.ChangesValidated += ThemeAccent_ChangesValidated;
        EditorSettings.ThemeVariant.ChangesValidated += ThemeVariant_ChangesValidated;
        return;

        void ThemeAccent_ChangesValidated(object? sender, ChangesValidatedEventArgs _)
        {
            if (sender is SettingsKey<Core.Mathematics.Color> setting)
            {
                SetAccent(setting.GetValue().ToAvaloniaColor());
            }
        }

        void ThemeVariant_ChangesValidated(object? sender, ChangesValidatedEventArgs _)
        {
            if (sender is SettingsKey<string> setting)
            {
                var accent = GetPalette()?.Accent ?? default;
                switch (setting.GetValue())
                {
                    case nameof(ThemeVariant.Dark):
                        SetAccent(accent, ThemeVariant.Dark);
                        SetVariant(nameof(ThemeVariant.Dark));
                        break;
                    case nameof(ThemeVariant.Light):
                        SetAccent(accent, ThemeVariant.Light);
                        SetVariant(nameof(ThemeVariant.Light));
                        break;
                    default:
                        // we don't know which variant is the system one
                        SetAccent(accent, ThemeVariant.Dark);
                        SetAccent(accent, ThemeVariant.Light);
                        SetVariant(nameof(ThemeVariant.Default));
                        break;
                }
            }
        }

        ColorPaletteResources? GetPalette(ThemeVariant? variant = null)
        {
            var theme = Styles.OfType<FluentTheme>().FirstOrDefault();
            return theme?.Palettes[variant ?? ActualThemeVariant];
        }

        void SetAccent(Color accent, ThemeVariant? variant = null)
        {
            if (GetPalette(variant) is { } palette)
            {
                palette.Accent = accent;
            }
        }

        void SetVariant(string variant)
        {
            RequestedThemeVariant = variant switch
            {
                nameof(ThemeVariant.Dark) => ThemeVariant.Dark,
                nameof(ThemeVariant.Light) => ThemeVariant.Light,
                _ => ThemeVariant.Default
            };
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        InitializeMarkdownViewer();

        // The initial session opens once the main window (or view) is up: the progress window and the message boxes of the
        // loading are dialogs of the main window, which must be visible by then
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            RegisterProjectTemplates();
            var mainWindow = new MainWindow();
            desktop.MainWindow = mainWindow;
            Restart();
            mainWindow.Opened += (_, _) => OpenInitialSession();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            RegisterProjectTemplates();
            var mainView = new MainView();
            singleViewPlatform.MainView = mainView;
            Restart();
            mainView.Loaded += (_, _) => OpenInitialSession();
        }
    }

    /// <summary>
    /// Opens the session the editor starts with, if any (see <see cref="GetInitialSessionPath"/>), guarded against a crash loop.
    /// </summary>
    private void OpenInitialSession()
    {
        if (mainViewModel is not { } viewModel)
            return;
        if (GetInitialSessionPath() is { } initialPath)
            viewModel.OpenInitialSessionCommand.Execute(initialPath);
        else
            // No session to open: the project selection window, as the WPF editor shows at startup (its Cancel exits the editor)
            viewModel.SelectInitialProjectCommand.Execute();
    }

    /// <summary>
    /// Replaces the main view model (and the services that live with it) by a fresh one in this process, optionally opening a session.
    /// </summary>
    /// <remarks>
    /// Once this process has loaded a session, the next one opens in a new process instead (<see cref="EditorProcessService"/>),
    /// when the host can start one.
    /// </remarks>
    /// <param name="initialPath">The session to open, or null for an empty editor.</param>
    /// <param name="initialSession">Whether this is the session the editor starts with (it is then guarded against a crash loop).</param>
    /// <param name="selectProject">Whether to show the project selection window (File > New), when no path is given.</param>
    public void Restart(UFile? initialPath = null, bool initialSession = false, bool selectProject = false)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow!.DataContext = InitializeMainViewModel(initialPath, initialSession, selectProject);
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView!.DataContext = InitializeMainViewModel(initialPath, initialSession, selectProject);
        }
    }

    /// <summary>
    /// The session to open at startup: the command line, else the startup session of the settings, else the last session when
    /// the settings ask to reload it. None with <c>/NewProject</c>.
    /// </summary>
    internal static UFile? GetInitialSessionPath()
    {
        if (Options.NewProject)
            return null;
        if (!UPath.IsNullOrEmpty(Options.InitialSessionPath))
            return Options.InitialSessionPath;

        var startupSessionPath = GameStudioEditorSettings.StartupSession.GetValue();
        if (!UPath.IsNullOrEmpty(startupSessionPath))
            return startupSessionPath;

        if (EditorSettings.ReloadLastSession.GetValue())
        {
            var mru = GameStudioInternalSettings.CreateMostRecentlyUsedFiles();
            return mru.MostRecentlyUsedFiles.FirstOrDefault()?.FilePath;
        }
        return null;
    }

    private MainViewModel InitializeMainViewModel(UFile? initialPath, bool initialSession, bool selectProject = false)
    {
        // The services of the previous editor go with it (the plugin service listens to the plugin registry)
        (serviceProvider?.TryGet<PluginService>())?.Dispose();
        serviceProvider = InitializeServiceProvider();

        var viewmodel = new MainViewModel(serviceProvider);
        mainViewModel = viewmodel;
        if (initialPath is not null)
        {
            if (initialSession)
                viewmodel.OpenInitialSessionCommand.Execute(initialPath);
            else
                viewmodel.OpenCommand.Execute(initialPath);
        }
        else if (selectProject)
        {
            viewmodel.SelectProjectCommand.Execute();
        }
        return viewmodel;
    }

    private static void InitializeMarkdownViewer()
    {
        // Global pipeline — applies to every MarkdownViewer in the app
        MarkdownViewerDefaults.Pipeline = new Markdig.MarkdownPipelineBuilder()
            .UseSupportedExtensions()
            .Build();

        // Global link handler — handles external links for every MarkdownViewer in the app
        MarkdownViewer.LinkClickedEvent.AddClassHandler<MarkdownViewer>(OnLinkClicked);

        static void OnLinkClicked(MarkdownViewer sender, LinkClickedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Url) { UseShellExecute = true });
            }
            catch
            {
                // Ignore failures to open browser
            }
        }
    }

    /// <summary>
    /// Loads the editor's own plugin assemblies: their module constructors register their plugins and their asset assemblies.
    /// The plugins of the companion packages of a session are discovered when the session loads them.
    /// </summary>
    /// <remarks>
    /// The editor references its own layers directly (this project's references); plugin packages are never referenced,
    /// they come as companions of the session's packages.
    /// </remarks>
    private static void InitializePlugins()
    {
        // The order of the registrations is the order of the module constructors: the view layers run the ones of the
        // layers below them first
        RuntimeHelpers.RunModuleConstructor(typeof(Core.Assets.Editor.Avalonia.StrideCoreEditorViewPlugin).Module.ModuleHandle);
        RuntimeHelpers.RunModuleConstructor(typeof(Assets.Editor.Avalonia.StrideEditorViewPlugin).Module.ModuleHandle);

        // The command line options of the services
        StrideConfig.GraphicsDebugMode = Options.DebugEditorGraphics;
        var enableThumbnails = Options.EnableThumbnails;
        var disablePreview = Options.DisablePreview;

        // Thumbnails and previews render with a graphics device: without one (no driver on this machine), they are turned off
        // rather than taking the session down when it opens
        if (enableThumbnails || !disablePreview)
        {
            try
            {
                _ = Stride.Graphics.GraphicsAdapterFactory.DefaultAdapter;
            }
            catch (Exception e)
            {
                Log.Warning("No graphics device is available: thumbnails and asset previews are disabled.", e);
                enableThumbnails = false;
                disablePreview = true;
            }
        }

        GameStudioPreviewService.DisablePreview = disablePreview;
        foreach (var plugin in AssetsPlugin.RegisteredPlugins.OfType<StrideEditorPlugin>())
            plugin.EnableThumbnailService = enableThumbnails;

        // Note: it doesn't have to be done here. The only constraint is to do it before AssetsPlugin.RegisteredPlugins is accessed for the first time.
        // So it could be delayed until after a window is opened and even display progress.
    }

    /// <summary>
    /// Registers the project templates (new game, code library, plugin, and the starters and samples once downloaded) shown by the
    /// new project windows, and the asset templates (the Add asset list) of the Stride package, as the WPF plugin does when it
    /// loads. Only the templates installed on this machine are scanned here; the missing content packages download in the
    /// background. An editor without templates still starts.
    /// </summary>
    private static void RegisterProjectTemplates()
    {
        try
        {
            DotNetNewTemplateBridge.RegisterProjectTemplates();
        }
        catch (Exception e)
        {
            Log.Warning("The project templates could not be loaded: the new project windows will be empty.", e);
        }
        try
        {
            // The asset assemblies are loaded by the plugins of the session, not by the template package
            StrideDefaultTemplates.LoadAssetTemplates(loadAssemblyReferences: false);
        }
        catch (Exception e)
        {
            Log.Warning("The asset templates could not be loaded: the Add asset list will be empty.", e);
        }
    }

    /// <summary>
    /// The source levels of the global loggers, from the settings, before anything creates them.
    /// </summary>
    private static void InitializeLogging()
    {
        foreach (var (module, level) in EditorSettings.GetModuleLevels(EditorSettings.SourceModuleLevels))
            GlobalLogger.SetModuleLevel(module, level);
    }

    /// <summary>
    /// The language of the editor, from the settings ("MachineDefault" or a culture name).
    /// </summary>
    private static void InitializeLanguage()
    {
        var language = EditorSettings.Language.GetValue();
        try
        {
            TranslationManager.Instance.CurrentLanguage = string.IsNullOrEmpty(language) || language == "MachineDefault"
                ? CultureInfo.InstalledUICulture
                : CultureInfo.GetCultureInfo(language);
        }
        catch (CultureNotFoundException e)
        {
            Log.Warning($"The language [{language}] of the settings is unknown; the machine's language is used.", e);
            TranslationManager.Instance.CurrentLanguage = CultureInfo.InstalledUICulture;
        }
    }

    /// <summary>
    /// The services of one editor (one main view model): the shell registers them before a session opens, the plugins add theirs when it does.
    /// </summary>
    internal static ViewModelServiceProvider InitializeServiceProvider()
    {
        var dispatcherService = DispatcherService.Create();
        var services = new List<object>
        {
            dispatcherService,
            new PluginService(dispatcherService),
            // The folder and static thumbnails are PNGs embedded in this assembly (Resources/*.png)
            new ThumbnailDataFactory(typeof(App).Assembly),
        };
        // The clipboard belongs to the main window; there is none in a headless host
        if (DialogService.MainWindow?.Clipboard is { } clipboard)
            services.Add(new ClipboardService(clipboard));
        // One for the whole process: it knows whether a session was loaded before the editor restarted in place
        if (ProcessLauncher is { } launcher)
            services.Add(processService ??= new EditorProcessService(launcher, Options));
        var serviceProvider = new ViewModelServiceProvider(services);
        serviceProvider.RegisterService(new EditorDebugService(serviceProvider));
        // The notification bar of the main view: the dialog service shows the background operations there
        serviceProvider.RegisterService(new NotificationsViewModel(serviceProvider));
        serviceProvider.RegisterService(new EditorDialogService(serviceProvider));
        return serviceProvider;
    }
}
