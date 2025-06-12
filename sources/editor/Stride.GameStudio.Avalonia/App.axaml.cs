// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.IO;
using Stride.Core.Presentation.Avalonia.Services;
using Stride.Core.Presentation.ViewModels;
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
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
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

    private static MainViewModel InitializeMainViewModel(UFile? initialPath)
    {
        var viewmodel = new MainViewModel(InitializeServiceProvider());
        if (initialPath is not null)
            viewmodel.OpenCommand.Execute(initialPath);
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
