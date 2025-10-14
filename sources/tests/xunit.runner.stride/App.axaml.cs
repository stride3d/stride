// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using xunit.runner.stride.ViewModels;
using xunit.runner.stride.Views;

namespace xunit.runner.stride;

public partial class App : Application
{
    internal readonly CancellationTokenSource cts = new();
    internal Action<bool>? setInteractiveMode;

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
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel
                {
                    Tests =
                    {
                        SetInteractiveMode = setInteractiveMode,
                        IsInteractiveMode = true,
                    }
                }
            };
            desktop.MainWindow.Closed += (_, __) => cts.Cancel();
            desktop.MainWindow.Show();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            // don't remove; also used by visual designer.
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel
                {
                    Tests =
                    {
                        SetInteractiveMode = setInteractiveMode,
                        IsInteractiveMode = true,
                    }
                }
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
