// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Stride.Core.Assets.Editor.Avalonia;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.IO;
using Stride.Core.Presentation.Avalonia.Extensions;
using Stride.Core.Presentation.Avalonia.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Settings;
using Stride.GameStudio.Avalonia.Services;
using Stride.GameStudio.Avalonia.ViewModels;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        InitializePlugins();

        EditorSettings.Initialize();
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
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new ProjectSelectionWindow();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView();
        }

        base.OnFrameworkInitializationCompleted();
        Restart();
    }

    public void Restart(UFile? initialPath = null)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow!.DataContext = InitializeMainViewModel(initialPath);
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView!.DataContext = InitializeMainViewModel(initialPath);
        }
    }

    private static object InitializeMainViewModel(UFile? initialPath)
    {
        //Switch viewmodel depending the program arguments
        var viewmodel = new NewOrOpenSessionTemplateCollectionViewModel(InitializeServiceProvider());
        // if (initialPath is not null)
        //     vm.OpenCommand.Execute(initialPath);
        return viewmodel;
    }

    private static void InitializePlugins()
    {
        // TODO xplat-editor load plugins from path, and ideally remove direct dependencies to these assemblies in this project.
        // Until then, use a hack to force loading the assemblies.
        string _;
        _ = typeof(Assets.Presentation.StrideDefaultAssetsPlugin).Name;
        _ = typeof(Core.Assets.Editor.Avalonia.StrideCoreEditorViewPlugin).Name;
        _ = typeof(Assets.Editor.StrideEditorPlugin).Name;
        _ = typeof(Assets.Editor.Avalonia.StrideEditorViewPlugin).Name;
        // Note: it doesn't have to be done here. The only constraint is to do it before AssetsPlugin.RegisteredPlugins is accessed for the first time.
        // So it could be delayed until after a windows is opened and even display progress.
    }

    private static IViewModelServiceProvider InitializeServiceProvider()
    {
        var dispatcherService = DispatcherService.Create();
        var services = new object[]
        {
            dispatcherService,
            new PluginService(dispatcherService)
        };
        var serviceProvider = new ViewModelServiceProvider(services);
        serviceProvider.RegisterService(new EditorDebugService(serviceProvider));
        serviceProvider.RegisterService(new EditorDialogService(serviceProvider));
        return serviceProvider;
    }
}
