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

    public bool HasMainWindow => MainWindow != null;

    public static Window? MainWindow => (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    protected IDispatcherService Dispatcher { get; }

    protected IStorageProvider? StorageProvider => MainWindow?.StorageProvider;

    public void Exit(int exitCode)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.Shutdown(exitCode);
        }
        else
        {
            Environment.Exit(exitCode);
        }
    }

    public async Task<UFile?> OpenFilePickerAsync(UPath? initialPath = null)
    {
        if (StorageProvider is null) return null;

        return await Dispatcher.InvokeTask(async () =>
        {
            var initialLocation = await StorageProvider.TryGetFolderFromPathAsync(initialPath);
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                SuggestedStartLocation = initialLocation,
            });

            var file = files?.Count > 0 ? files[0] : null;
            var path = file?.TryGetLocalPath();
            if (string.IsNullOrEmpty(path)) return null;

            return path;
        });
    }

    public async Task<UDirectory?> OpenFolderPickerAsync(UPath? initialPath = null)
    {
        if (StorageProvider is null) return null;

        return await Dispatcher.InvokeTask(async () =>
        {
            var initialLocation = await StorageProvider.TryGetFolderFromPathAsync(initialPath);
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                AllowMultiple = false,
                SuggestedStartLocation = initialLocation,
            });

            var folder = folders?.Count > 0 ? folders[0] : null;
            var path = folder?.TryGetLocalPath();
            if (string.IsNullOrEmpty(path)) return null;

            return path;
        });
    }
}
