// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Avalonia.Services;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Services;

internal class EditorDialogService : DialogService
{
    private DebugWindow? debugWindow;
    private readonly IViewModelServiceProvider serviceProvider;

    public EditorDialogService(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider.Get<IDispatcherService>())
    {
        this.serviceProvider = serviceProvider;
    }

    public async Task ShowAboutWindowAsync()
    {
        if (MainWindow == null) return;

        await Dispatcher.InvokeTask(async () =>
        {
            await new AboutWindow().ShowDialog(MainWindow);
        });
    }

    public async Task ShowDebugWindowAsync()
    {
        await Dispatcher.InvokeAsync(() =>
        {
            if (debugWindow == null)
            {
                debugWindow = new DebugWindow(new DebugWindowViewModel(serviceProvider));
                debugWindow.Show();
                debugWindow.Closed += (_, _) => debugWindow = null;
            }
            else
            {
                if (debugWindow.WindowState == WindowState.Minimized)
                {
                    debugWindow.WindowState = WindowState.Normal;
                }
                debugWindow.Activate();
            }
        });
    }
}
