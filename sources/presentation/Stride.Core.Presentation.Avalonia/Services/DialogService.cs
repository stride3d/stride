// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Avalonia.Windows;
using Stride.Core.Presentation.Windows;

namespace Stride.Core.Presentation.Avalonia.Services;

// Note: this class is shared with the Launcher. Beware before adding new dependencies.
public class DialogService : IDialogService
{
    public DialogService(IDispatcherService dispatcher)
    {
        Dispatcher = dispatcher;
    }

    public string ApplicationName { get; init; } = string.Empty;

    public bool HasMainWindow => MainWindow != null;

    public static Window? MainWindow => (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    protected IDispatcherService Dispatcher { get; }

    protected IStorageProvider? StorageProvider => MainWindow?.StorageProvider;

    public void Exit(int exitCode = 0)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            if (desktopLifetime.ShutdownMode == ShutdownMode.OnMainWindowClose && desktopLifetime.MainWindow is { } mainWindow)
            {
                mainWindow.Close();
            }
            else
            {
                desktopLifetime.TryShutdown(exitCode);
            }
        }
        else if (Application.Current?.ApplicationLifetime is IControlledApplicationLifetime controlledLifetime)
        {
            controlledLifetime.Shutdown();
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
                FileTypeFilter = filters?.Select(x => new FilePickerFileType(x.Name) { Patterns = x.Patterns }).ToList(),
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
                FileTypeFilter = filters?.Select(x => new FilePickerFileType(x.Name) { Patterns = x.Patterns }).ToList(),
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

    public async Task<UFile?> SaveFilePickerAsync(UDirectory? initialPath = null, IReadOnlyList<FilePickerFilter>? filters = null, string? defaultExtension = null, string? defaultFileName = null)
    {
        if (StorageProvider is null) return null;

        return await Dispatcher.InvokeTask(async () =>
        {
            var initialLocation = await StorageProvider.TryGetFolderFromPathAsync(initialPath);
            var storageFile = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                DefaultExtension = defaultExtension,
                FileTypeChoices = filters?.Select(x => new FilePickerFileType(x.Name) { Patterns = x.Patterns }).ToList(),
                ShowOverwritePrompt = true,
                SuggestedFileName = defaultFileName,
                SuggestedStartLocation = initialLocation
            });
            var path = storageFile?.TryGetLocalPath();
            if (string.IsNullOrEmpty(path)) return null;

            return path;
        });
    }

    public async Task<CheckedMessageBoxResult> CheckedMessageBoxAsync(string message, bool? isChecked, string checkboxMessage, MessageBoxButton buttons, MessageBoxImage image)
    {
        return await Dispatcher.InvokeTask(() => CheckedMessageBox.ShowAsync(ApplicationName, message, isChecked, checkboxMessage, IDialogService.GetButtons(buttons), image, MainWindow));
    }

    public async Task<CheckedMessageBoxResult> CheckedMessageBoxAsync(string message, bool? isChecked, string checkboxMessage, IReadOnlyCollection<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None)
    {
        return await Dispatcher.InvokeTask(() => CheckedMessageBox.ShowAsync(ApplicationName, message, isChecked, checkboxMessage, buttons, image, MainWindow));
    }

    public async Task<MessageBoxResult> MessageBoxAsync(string message, MessageBoxButton buttons, MessageBoxImage image)
    {
        return (MessageBoxResult)await Dispatcher.InvokeTask(() => MessageBox.ShowAsync(ApplicationName, message, IDialogService.GetButtons(buttons), image, MainWindow));
    }

    public async Task<int> MessageBoxAsync(string message, IReadOnlyCollection<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None)
    {
        return await Dispatcher.InvokeTask(() => MessageBox.ShowAsync(ApplicationName, message, buttons, image, MainWindow));
    }
}
