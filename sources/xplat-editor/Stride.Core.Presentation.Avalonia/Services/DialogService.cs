// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;

namespace Stride.Core.Presentation.Avalonia.Services;

// Note: this class is shared with the Launcher. Beware before adding new dependencies.
public class DialogService : IDialogService
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

    public async Task<UFile?> OpenFilePickerAsync(UDirectory? initialPath = null, IReadOnlyList<FilePickerFilter>? filters = null)
    {
        if (StorageProvider is null) return null;

        return await Dispatcher.InvokeTask(async () =>
        {
            var initialLocation = await StorageProvider.TryGetFolderFromPathAsync(initialPath);
            var storageFiles = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                SuggestedStartLocation = initialLocation,
            });

            var storageFile = storageFiles?.Count > 0 ? storageFiles[0] : null;
            var path = storageFile?.TryGetLocalPath();
            if (string.IsNullOrEmpty(path)) return null;

            return path;
        });
    }

    public async Task<IReadOnlyList<UFile>> OpenMultipleFilesPickerAsync(UDirectory? initialPath = null, IReadOnlyList<FilePickerFilter>? filters = null)
    {
        if (StorageProvider is null) return Array.Empty<UFile>();

        return await Dispatcher.InvokeTask(async () =>
        {
            var initialLocation = await StorageProvider.TryGetFolderFromPathAsync(initialPath);
            var storageFiles = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                SuggestedStartLocation = initialLocation,
            });

            var files = new List<UFile>(storageFiles.Count);
            foreach (var storageFile in storageFiles)
            {
                var path = storageFile?.TryGetLocalPath();
                if (string.IsNullOrEmpty(path)) continue;

                files.Add(path);
            }

            return files;
        });
    }

    public async Task<UDirectory?> OpenFolderPickerAsync(UDirectory? initialPath = null)
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

    Task<MessageBoxResult> IDialogService.MessageBoxAsync(string message, MessageBoxButton buttons, MessageBoxImage image)
    {
        throw new NotImplementedException();
    }
}
