// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using Stride.Core.IO;
using Avalonia;
using Avalonia.Platform.Storage;

namespace Stride.Core.Presentation.Services;

public class DialogService : IDialogService
{
    public static Window? MainWindow => (Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow!;

    public async Task<UFile?> OpenFilePickerAsync()
    {
        if (MainWindow == null) return null;

        var files = await MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
        });

        var file = files?.Count > 0 ? files[0] : null;
        var path = file?.TryGetLocalPath();
        if (string.IsNullOrEmpty(path)) return null;

        return path;
    }
}
