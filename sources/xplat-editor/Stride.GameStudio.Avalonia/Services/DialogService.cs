// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;

namespace Stride.GameStudio.Avalonia.Services;

// TODO: consider moving it to a new assembly (such as Stride.Core.Presentation.Avalonia)
//       to make it reusable outside the context of an editor
internal class DialogService : IDialogService
{
    public DialogService(IDispatcherService dispatcher)
    {
        Dispatcher = dispatcher;
    }

    public static Window? MainWindow => (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    protected IDispatcherService Dispatcher { get; }

    public async Task<UFile?> OpenFilePickerAsync()
    {
        if (MainWindow == null) return null;

        return await Dispatcher.InvokeTask(async () =>
        {
            var files = await MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
            });

            var file = files?.Count > 0 ? files[0] : null;
            var path = file?.TryGetLocalPath();
            if (string.IsNullOrEmpty(path)) return null;

            return path;
        });
    }
}
