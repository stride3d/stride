// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Presentation.Services;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Services;

internal sealed class EditorDialogService : DialogService, IEditorDialogService
{
    public EditorDialogService(IDispatcherService dispatcher)
        : base(dispatcher)
    {
    }

    public async Task ShowAboutWindowAsync()
    {
        if (MainWindow == null) return;

        await Dispatcher.InvokeTask(async () =>
        {
            await new AboutWindow().ShowDialog(MainWindow);
        });
    }
}
