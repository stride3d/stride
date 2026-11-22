// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Stride.Assets.Models;
using Stride.Core.Assets.Editor.Commands;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.GameStudio.Avalonia.Commands;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The buttons of the solution explorer that expand the folders of the assets, expand everything or collapse everything, and
/// the commands of the palette that do the same.
/// </summary>
public sealed class SolutionExplorerToolbarTests
{
    [AvaloniaFact]
    public async Task ExpandAllAssetFoldersExpandsEveryFolderOfTheAssets()
    {
        var (fixture, window) = await OpenAsync();
        using (fixture)
        {
            try
            {
                // Everything is expanded, then collapsed
                await ClickAsync(window, "ExpandAllFoldersButton");
                await ClickAsync(window, "CollapseAllFoldersButton");
                Assert.False(Item(window, fixture.Package).IsExpanded);

                await ClickAsync(window, "ExpandAssetFoldersButton");

                Assert.True(Item(window, fixture.Package).IsExpanded);
                Assert.True(Item(window, fixture.Package.AssetMountPoint).IsExpanded);
                Assert.True(Folder(window, "Characters").IsExpanded);
                Assert.True(Folder(window, "Heroes").IsExpanded);
                // The deepest folder has no folder to show
                Assert.NotNull(FolderOrNull(window, "Knights"));
            }
            finally
            {
                await window.CloseAsync();
            }
        }
    }

    [AvaloniaFact]
    public async Task ExpandAllExpandsEverythingAndCollapseAllCollapsesThePackages()
    {
        var (fixture, window) = await OpenAsync();
        using (fixture)
        {
            try
            {
                await ClickAsync(window, "ExpandAllFoldersButton");
                // Every item with children is expanded
                var items = Tree(window).GetVisualDescendants().OfType<TreeViewItem>().ToList();
                Assert.Contains(items, x => x.DataContext is DirectoryViewModel { Name: "Knights" });
                Assert.All(items.Where(x => x.ItemCount > 0), x => Assert.True(x.IsExpanded, x.DataContext?.ToString()));

                await ClickAsync(window, "CollapseAllFoldersButton");
                // The categories stay expanded, with their packages collapsed down to the last folder
                var category = Assert.IsType<TreeViewItem>(Tree(window).ContainerFromIndex(0));
                Assert.True(category.IsExpanded);
                Assert.False(Item(window, fixture.Package).IsExpanded);
                Assert.All(items.Where(x => x.DataContext is DirectoryBaseViewModel), x => Assert.False(x.IsExpanded, x.DataContext?.ToString()));
            }
            finally
            {
                await window.CloseAsync();
            }
        }
    }

    [AvaloniaFact]
    public async Task ThePaletteRunsTheSameCommands()
    {
        var (fixture, window) = await OpenAsync();
        using (fixture)
        {
            try
            {
                var host = fixture.Plugins;
                var context = new CommandContext { Session = fixture.Session, Location = CommandLocations.Palette, Source = window };
                CommandDescriptor Command(string id) => host.Commands.Items.Single(x => x.Id == id);
                foreach (var id in new[] { GameStudioCommands.ExpandAssetFoldersId, GameStudioCommands.ExpandAllFoldersId, GameStudioCommands.CollapseAllFoldersId })
                {
                    Assert.True(Command(id).ShowInPalette, id);
                    Assert.True(host.CanExecuteCommand(Command(id), context), id);
                }

                Assert.True(await host.ExecuteCommandAsync(Command(GameStudioCommands.ExpandAllFoldersId), context));
                Assert.True(Folder(window, "Heroes").IsExpanded);

                Assert.True(await host.ExecuteCommandAsync(Command(GameStudioCommands.CollapseAllFoldersId), context));
                Assert.False(Item(window, fixture.Package).IsExpanded);

                Assert.True(await host.ExecuteCommandAsync(Command(GameStudioCommands.ExpandAssetFoldersId), context));
                Assert.True(Folder(window, "Heroes").IsExpanded);
            }
            finally
            {
                await window.CloseAsync();
            }
        }
    }

    private static async Task<(ShellSessionFixture Fixture, MainWindow Window)> OpenAsync()
    {
        var fixture = await ShellSessionFixture.OpenAsync(
            ("Characters/Heroes/Knights/Arthur", new SkeletonAsset()),
            ("Characters/Villains/Morgana", new SkeletonAsset()),
            ("Props/Crate", new SkeletonAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        await WindowTestHelper.WaitUntilAsync(() => window.GetVisualDescendants().OfType<SolutionExplorerView>().Any(), "the solution explorer");
        await WindowTestHelper.PumpAsync();
        return (fixture, window);
    }

    private static TreeView Tree(Window window) => window.GetVisualDescendants().OfType<TreeView>().Single(x => x.Name == "SolutionTree");

    private static TreeViewItem Item(Window window, object viewModel)
    {
        return Tree(window).GetVisualDescendants().OfType<TreeViewItem>().Single(x => ReferenceEquals(x.DataContext, viewModel));
    }

    private static TreeViewItem? FolderOrNull(Window window, string name)
    {
        return Tree(window).GetVisualDescendants().OfType<TreeViewItem>().SingleOrDefault(x => x.DataContext is DirectoryViewModel directory && directory.Name == name && x.IsEffectivelyVisible);
    }

    private static TreeViewItem Folder(Window window, string name) => FolderOrNull(window, name) ?? throw new InvalidOperationException($"No folder {name} on screen");

    private static async Task ClickAsync(Window window, string buttonName)
    {
        var button = window.GetVisualDescendants().OfType<Button>().Single(x => x.Name == buttonName);
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await WindowTestHelper.PumpAsync();
    }
}
