// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Stride.Core.Presentation.Avalonia.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Launcher.ViewModels;
using Stride.Launcher.Views;

namespace Stride.Launcher;

public partial class App : Application
{
    internal readonly CancellationTokenSource cts = new();

    internal MainWindow? MainWindow { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = MainWindow = new()
            {
                DataContext = InitializeMainViewModel()
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            // don't remove; also used by visual designer.
            singleViewPlatform.MainView = new MainView
            {
                DataContext = InitializeMainViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static MainViewModel InitializeMainViewModel()
    {
        return new(InitializeServiceProvider());
    }

    private static IViewModelServiceProvider InitializeServiceProvider()
    {
        var dispatcherService = DispatcherService.Create();
        var services = new object[]
        {
            dispatcherService,
            new DialogService(dispatcherService) { ApplicationName = Launcher.ApplicationName }
        };
        return new ViewModelServiceProvider(services);
    }
}
