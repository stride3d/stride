// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Services;

internal sealed class EditorDialogService : DialogService, IEditorDialogService
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

    public void ShowDebugWindow()
    {
        if (debugWindow == null)
        {
            debugWindow = new DebugWindow(new DebugWindowViewModel(serviceProvider));
            debugWindow.Closed += (s, e) => debugWindow = null;
            debugWindow.Show();
        }

        if (debugWindow.WindowState == WindowState.Minimized)
        {
            debugWindow.WindowState = WindowState.Normal;
        }
        debugWindow.Activate();
    }
}
