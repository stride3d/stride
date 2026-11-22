// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.VisualTree;
using Stride.Assets.Models;
using Stride.Core.Assets.Editor.Avalonia.DragDrop;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.IO;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// Importing files dropped from the file manager of the system onto the asset view, or onto a folder of the solution explorer.
/// </summary>
/// <remarks>
/// No asset template imports the extension of the test files, so the import ends with its "not supported" message: the message
/// shows that the drop ran the import, and the selected folder where it would create the assets.
/// </remarks>
public sealed class FileDropTests
{
    private const string NotSupported = "These files aren't supported.";

    [AvaloniaFact]
    public async Task TheDragDataCarriesTheDroppedFiles()
    {
        var directory = Path.Combine(Path.GetTempPath(), "Stride.GameStudio.Avalonia.Tests", Guid.NewGuid().ToString("N"));
        var window = new Window();
        await window.ShowAsync();
        try
        {
            var first = Path.Combine(directory, "first.fbx");
            var second = Path.Combine(directory, "second.png");
            var data = await FileDataAsync(window, first, second);

            Assert.Equal([new UFile(first), new UFile(second)], AssetDragDropHelper.GetFiles(data));
            // The files are not items of the editor
            Assert.Empty(AssetDragDropHelper.GetItems(data));
            Assert.Empty(AssetDragDropHelper.GetFiles(null));
            Assert.Empty(AssetDragDropHelper.GetFiles(AssetDragDropHelper.CreateItemData(["entity"])));
        }
        finally
        {
            await window.CloseAsync();
            Directory.Delete(directory, true);
        }
    }

    [AvaloniaFact]
    public async Task FilesDroppedOnTheAssetViewAreImportedIntoTheSelectedFolder()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Characters/Skeleton", new SkeletonAsset()));
        var dialogs = fixture.ReplaceDialogService();
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var assets = fixture.Session.AssetCollection;
            var characters = Folder(fixture, "Characters");
            assets.SelectedLocations.Clear();
            assets.SelectedLocations.Add(characters);
            await WindowTestHelper.PumpAsync();
            var list = AssetList(window);
            var behavior = Behavior<FileDropBehavior>(list);
            var file = new UFile(Path.Combine(fixture.Directory, "Sources", "hero.stridetestfile"));

            // The asset view takes files, with the message of the import
            Assert.Same(assets, behavior.GetDropTarget([file], out var message));
            Assert.Equal("Drop files", message);

            await DropFilesAsync(window, list, file.ToOSPath());
            await WindowTestHelper.WaitUntilAsync(() => dialogs.Messages.Count > 0, "the import");

            Assert.Equal([NotSupported], dialogs.Messages);
            Assert.Equal([characters], assets.SelectedLocations);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task FilesDroppedOnAFolderAreImportedIntoIt()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Characters/Skeleton", new SkeletonAsset()), ("Props/Crate", new SkeletonAsset()));
        var dialogs = fixture.ReplaceDialogService();
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var assets = fixture.Session.AssetCollection;
            var characters = Folder(fixture, "Characters");
            assets.SelectedLocations.Clear();
            assets.SelectedLocations.Add(Folder(fixture, "Props"));
            var row = await FindFolderRowAsync(window, "Characters");
            var behavior = Behavior<AddChildDropBehavior>(row);
            Assert.True(behavior.ImportFiles);
            var file = new UFile(Path.Combine(fixture.Directory, "Sources", "hero.stridetestfile"));

            // The folder takes the files, and says where they go
            var target = Assert.IsType<FolderFileImport>(behavior.GetDropTarget([file], AddChildModifiers.None, out var message));
            Assert.Same(characters, target.Folder);
            Assert.Equal("Import into Characters/", message);

            await DropFilesAsync(window, row, file.ToOSPath());
            await WindowTestHelper.WaitUntilAsync(() => dialogs.Messages.Count > 0, "the import");

            // The drop selects the folder it was made on, where the import creates the assets
            Assert.Equal([NotSupported], dialogs.Messages);
            Assert.Equal([characters], assets.SelectedLocations);
            // Nothing was moved or created
            Assert.Empty(fixture.Session.ActionHistory.Transactions);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task AFolderStillMovesTheAssetsDraggedOnItAndRefusesFilesWithoutImport()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Characters/Skeleton", new SkeletonAsset()), ("Hero", new SkeletonAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var row = await FindFolderRowAsync(window, "Characters");
            var behavior = Behavior<AddChildDropBehavior>(row);
            var file = new UFile(Path.Combine(fixture.Directory, "hero.stridetestfile"));

            // The editor's own items go to the folder itself
            Assert.IsAssignableFrom<DirectoryViewModel>(behavior.GetDropTarget([fixture.Asset("Hero")], AddChildModifiers.None, out _));
            // Without ImportFiles, a folder takes no file, as before
            behavior.ImportFiles = false;
            Assert.Null(behavior.GetDropTarget([file], AddChildModifiers.None, out var message));
            Assert.Equal("Only folders can be dropped", message);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task AnAssetDraggedOverTheAssetViewIsNotAnImport()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Characters/Skeleton", new SkeletonAsset()));
        var dialogs = fixture.ReplaceDialogService();
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var list = AssetList(window);
            var point = list.TranslatePoint(new Point(list.Bounds.Width / 2, list.Bounds.Height / 2), window)!.Value;
            var data = AssetDragDropHelper.CreateData([fixture.Asset("Characters/Skeleton")]);

            window.DragDrop(point, RawDragEventType.DragEnter, data, DragDropEffects.Move, RawInputModifiers.None);
            window.DragDrop(point, RawDragEventType.DragOver, data, DragDropEffects.Move, RawInputModifiers.None);

            // No import feedback over the asset view, and the drop does nothing
            Assert.Null(DragFeedbackAdorner.Get(list));
            window.DragDrop(point, RawDragEventType.Drop, data, DragDropEffects.Move, RawInputModifiers.None);
            await Task.Delay(100);
            await WindowTestHelper.PumpAsync();
            Assert.Empty(dialogs.Messages);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    private static DirectoryViewModel Folder(ShellSessionFixture fixture, string name)
    {
        return fixture.Package.AssetMountPoint.SubDirectories.OfType<DirectoryViewModel>().Single(x => x.Name == name);
    }

    private static ListBox AssetList(Window window) => window.GetVisualDescendants().OfType<ListBox>().Single(x => x.Name == "AssetList");

    private static TBehavior Behavior<TBehavior>(Control control)
    {
        return global::Avalonia.Xaml.Interactivity.Interaction.GetBehaviors(control).OfType<TBehavior>().Single();
    }

    /// <summary>
    /// Expands the solution explorer down to the folder with the given name and returns the root of its row.
    /// </summary>
    private static async Task<Control> FindFolderRowAsync(Window window, string name)
    {
        Control? row = null;
        await WindowTestHelper.WaitUntilAsync(() =>
        {
            foreach (var item in window.GetVisualDescendants().OfType<TreeViewItem>())
                item.IsExpanded = true;
            row = window.GetVisualDescendants().OfType<TreeViewItem>()
                .Where(x => x.DataContext is DirectoryViewModel directory && directory.Name == name)
                .Select(x => x.GetVisualDescendants().OfType<StackPanel>().FirstOrDefault(p => global::Avalonia.Xaml.Interactivity.Interaction.GetBehaviors(p).OfType<AddChildDropBehavior>().Any()))
                .FirstOrDefault(x => x is not null);
            return row is not null;
        }, $"the folder {name} in the solution explorer");
        var found = row!;
        found.BringIntoView();
        await WindowTestHelper.PumpAsync();
        return found;
    }

    private static async Task DropFilesAsync(Window window, Control target, params string[] paths)
    {
        // Near the start of the target: a row of the solution explorer can be wider than its panel
        var point = target.TranslatePoint(new Point(Math.Min(target.Bounds.Width / 2, 10), target.Bounds.Height / 2), window) ?? throw new InvalidOperationException("The target is not in the window.");
        var data = await FileDataAsync(window, paths);
        window.DragDrop(point, RawDragEventType.DragEnter, data, DragDropEffects.Copy, RawInputModifiers.None);
        window.DragDrop(point, RawDragEventType.DragOver, data, DragDropEffects.Copy, RawInputModifiers.None);
        window.DragDrop(point, RawDragEventType.Drop, data, DragDropEffects.Copy, RawInputModifiers.None);
    }

    /// <summary>
    /// The data of a drag of files from the file manager: the files are created, as the storage provider only gives existing
    /// files.
    /// </summary>
    private static async Task<DataTransfer> FileDataAsync(TopLevel topLevel, params string[] paths)
    {
        var data = new DataTransfer();
        foreach (var path in paths)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, "test");
            var file = await topLevel.StorageProvider.TryGetFileFromPathAsync(new Uri(path)) ?? throw new InvalidOperationException($"No storage file for {path}");
            data.Add(DataTransferItem.CreateFile(file));
        }
        return data;
    }
}
