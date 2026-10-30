// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using Stride.Core.Assets.Editor.Internal;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.ViewModels;

/// <summary>
/// The Explore commands of the asset view and the solution explorer: show an item in the file manager, open the file of an asset or
/// its source file with another program.
/// </summary>
partial class SessionViewModel
{
    /// <summary>
    /// Shows the last of the given items (or the last selected location) in the file manager: the file of an asset, a package or
    /// a project, selected; the folder of a directory.
    /// </summary>
    public ICommandBase ExploreCommand { get; private set; } = null!;

    /// <summary>
    /// Shows the source file of an asset in the file manager, or its folder if the file no longer exists.
    /// </summary>
    public ICommandBase ExploreSourceFileCommand { get; private set; } = null!;

    /// <summary>
    /// Opens the file of an asset with the program the system associates with it.
    /// </summary>
    public ICommandBase OpenAssetFileCommand { get; private set; } = null!;

    /// <summary>
    /// Opens the source file of an asset with the program the system associates with it.
    /// </summary>
    public ICommandBase OpenSourceFileCommand { get; private set; } = null!;

    /// <summary>
    /// Opens the file of an asset with the text editor of the settings (<see cref="ExternalToolsSettings.DefaultTextEditor"/>).
    /// </summary>
    public ICommandBase OpenWithTextEditorCommand { get; private set; } = null!;

    private IFileExplorerService FileExplorer => ServiceProvider.TryGet<IFileExplorerService>() ?? new FileExplorerService();

    private void InitializeExploreCommands()
    {
        ExploreCommand = new AnonymousTaskCommand<object?>(ServiceProvider, ExploreAsync);
        ExploreSourceFileCommand = new AnonymousTaskCommand<AssetViewModel?>(ServiceProvider, ExploreSourceFileAsync);
        OpenAssetFileCommand = new AnonymousTaskCommand<AssetViewModel?>(ServiceProvider, OpenAssetFileAsync);
        OpenSourceFileCommand = new AnonymousTaskCommand<AssetViewModel?>(ServiceProvider, OpenSourceFileAsync);
        OpenWithTextEditorCommand = new AnonymousTaskCommand<AssetViewModel?>(ServiceProvider, OpenWithTextEditorAsync);
    }

    private void UpdateExploreCommands()
    {
        var asset = AssetCollection.SingleSelectedAsset;
        ExploreCommand.IsEnabled = AssetCollection.SelectedContent.Count > 0 || AssetCollection.SelectedLocations.Count == 1;
        ExploreSourceFileCommand.IsEnabled = asset?.Asset?.MainSource is not null;
        OpenWithTextEditorCommand.IsEnabled = OpenAssetFileCommand.IsEnabled = OpenSourceFileCommand.IsEnabled = asset is not null;
    }

    private async Task ExploreAsync(object? parameter)
    {
        if (parameter is IEnumerable items and not string)
            parameter = items.Cast<object>().LastOrDefault();
        parameter ??= AssetCollection.SelectedLocations.LastOrDefault();

        var package = GetContainerPackage(parameter);
        UPath path;
        var selectFile = false;
        switch (parameter)
        {
            case ProjectViewModel project:
                path = project.ProjectPath;
                selectFile = true;
                break;
            case PackageViewModel:
                path = package!.PackagePath;
                selectFile = true;
                break;
            case DirectoryViewModel folder when package is not null:
                // The folders of an asset mount point are in the asset folder, the others (code) in the package folder
                var root = folder.Root is AssetMountPointViewModel ? GetAssetFolder(package) : package.Package.RootDirectory;
                path = UPath.Combine(root, new UDirectory(folder.Path));
                break;
            case AssetViewModel asset when package is not null:
                path = asset.AssetItem.FullPath;
                selectFile = true;
                break;
            case PackageCategoryViewModel category when category.Content == LocalPackages && !string.IsNullOrEmpty(SolutionPath):
                path = SolutionPath;
                selectFile = true;
                break;
            case not null when package is not null:
                // A mount point, the dependencies of a package...: the asset folder of the package
                path = GetAssetFolder(package);
                break;
            default:
                return;
        }

        try
        {
            var osPath = path.ToOSPath();
            if (parameter is AssetViewModel && !File.Exists(osPath))
            {
                await Dialogs.MessageBoxAsync(Tr._p("Message", "You need to save the asset before you can explore it."), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (selectFile)
                await FileExplorer.ShowFileAsync(osPath);
            else
                await FileExplorer.OpenFolderAsync(osPath);
        }
        catch (Exception)
        {
            await Dialogs.MessageBoxAsync(Tr._p("Message", "There was a problem starting the file explorer."), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// The full path of the asset folder of a package.
    /// </summary>
    private static UDirectory GetAssetFolder(PackageViewModel package)
    {
        var folder = package.Package.GetDefaultAssetFolder();
        return folder.IsAbsolute || package.Package.RootDirectory is not { } root ? folder : UPath.Combine(root, folder);
    }

    private async Task ExploreSourceFileAsync(AssetViewModel? asset)
    {
        var source = asset?.Asset?.MainSource;
        if (source is null)
        {
            await Dialogs.MessageBoxAsync(Tr._p("Message", "This asset doesn't have a source file."), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var path = source.ToOSPath();
            if (File.Exists(path))
            {
                await FileExplorer.ShowFileAsync(path);
                return;
            }

            // If the file doesn't exist, we want to at least try to go to the directory it belonged to
            var folder = Path.GetDirectoryName(path);
            if (folder is null || !Directory.Exists(folder))
            {
                await Dialogs.MessageBoxAsync(Tr._p("Message", "Source file and path no longer exists."), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            await FileExplorer.OpenFolderAsync(folder);
        }
        catch (Exception)
        {
            await Dialogs.MessageBoxAsync(Tr._p("Message", "There was a problem starting the file explorer."), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task OpenAssetFileAsync(AssetViewModel? asset)
    {
        if (asset?.Directory?.Package is null)
            return;

        if (!await SaveBeforeOpeningAsync(asset, Tr._p("Message", "This asset has unsaved changes. To open it, you need to save it first. Do you want to save the session now?")))
            return;

        await OpenFileAsync(asset.AssetItem.FullPath);
    }

    private async Task OpenSourceFileAsync(AssetViewModel? asset)
    {
        if (asset?.Directory?.Package is null)
            return;

        var source = asset.Asset.MainSource;
        if (source is null)
        {
            await Dialogs.MessageBoxAsync(Tr._p("Message", "This asset doesn't have a source file to open."), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        await OpenFileAsync(source);
    }

    private async Task OpenWithTextEditorAsync(AssetViewModel? asset)
    {
        if (asset?.Directory?.Package is null)
            return;

        if (!await SaveBeforeOpeningAsync(asset, Tr._p("Message", "This asset has unsaved changes. To open it, you need to save the session first. Do you want to save now?")))
            return;

        var path = asset.AssetItem.FullPath.ToOSPath();
        if (!File.Exists(path))
        {
            await Dialogs.MessageBoxAsync(Tr._p("Message", "You need to save the file before you can open it."), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var setting = ExternalToolsSettings.DefaultTextEditor;
        var editor = setting.GetValue();
        var editorPath = editor is not null ? Environment.ExpandEnvironmentVariables(editor.ToOSPath()) : string.Empty;
        try
        {
            // The default text editor of the settings is Windows' Notepad: elsewhere, the system's editor of the file opens it
            if (!OperatingSystem.IsWindows() && editor == setting.DefaultValue && !File.Exists(editorPath))
                await FileExplorer.OpenFileAsync(path, tryEdit: true);
            else
                await FileExplorer.OpenFileWithAsync(editorPath, path);
        }
        catch (Exception ex)
        {
            var message = $"{Tr._p("Message", "There was a problem starting the text editor. Make sure the path to the text editor in Settings is correct.")}{ex.FormatSummary(true)}";
            await Dialogs.MessageBoxAsync(message, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Opens a file with the program that edits it, as the WPF <c>EditorViewModel.OpenFile</c>.
    /// </summary>
    private async Task OpenFileAsync(UFile file)
    {
        try
        {
            var path = file.ToOSPath();
            if (!File.Exists(path))
            {
                await Dialogs.MessageBoxAsync(Tr._p("Message", "You need to save the file before you can open it."), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            await FileExplorer.OpenFileAsync(path, tryEdit: true);
        }
        catch (Exception ex)
        {
            var message = $"{Tr._p("Message", "An error occurred while opening the file.")}{ex.FormatSummary(true)}";
            await Dialogs.MessageBoxAsync(message, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Offers to save the session when the asset has unsaved changes: the file of an asset is up to date once saved.
    /// </summary>
    /// <returns><c>true</c> if the asset has no unsaved changes (anymore).</returns>
    private async Task<bool> SaveBeforeOpeningAsync(AssetViewModel asset, string message)
    {
        if (!asset.IsDirty)
            return true;

        var buttons = DialogButtons.Create([Tr._p("Button", "Save"), Tr._p("Button", "Cancel")], 1, 2);
        var result = await Dialogs.MessageBoxAsync(message, buttons, MessageBoxImage.Information);
        if (result == 1)
            await SaveSessionAsync();

        return !asset.IsDirty;
    }
}
