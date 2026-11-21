// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Stride.Assets.Entities;
using Stride.Assets.Models;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.Sprite;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Engine;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;
using Stride.UI.Assets;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The trees follow the selection made outside them: in the scene editor (the scene view, the selection history, the
/// commands) the parents of the selected entities expand and the row of the last one scrolls into view, and a selection made
/// in the tree expands nothing; the UI editor does the same for its elements, the solution explorer for the folders
/// selected in the asset view, and the asset picker for the folder of its initial asset.
/// </summary>
public sealed class HierarchyFollowsSelectionTests
{
    [AvaloniaFact]
    public async Task AnEntitySelectedOutsideTheTreeUnderCollapsedParentsShowsSelected()
    {
        await using var scene = await OpenCollapsedSceneAsync();
        var grandchild = scene.Entity("Grandchild");
        Assert.False(IsShown(scene, "Grandchild"));

        // As the scene view selects the entity under the mouse
        scene.Editor.SelectedContent.Clear();
        scene.Editor.SelectedContent.Add(grandchild);
        await PumpAsync();

        Assert.True(scene.Entity("Parent").IsExpanded);
        Assert.True(scene.Entity("Child").IsExpanded);
        // Not the entity itself
        Assert.False(grandchild.IsExpanded);
        var row = scene.Row("Grandchild");
        Assert.True(row.IsSelected);
        Assert.True(IsShown(scene, "Grandchild"));
        // The view keeps the focus
        Assert.False(scene.Tree.IsKeyboardFocusWithin);
    }

    [AvaloniaFact]
    public async Task SeveralEntitiesSelectedAtOnceAreAllShown()
    {
        await using var scene = await OpenCollapsedSceneAsync();

        // As a rectangle selection or Select all children
        scene.Editor.SelectedContent.Clear();
        scene.Editor.SelectedContent.AddRange([scene.Entity("Leaf"), scene.Entity("Grandchild")]);
        await PumpAsync();

        Assert.True(scene.Entity("Box").IsExpanded);
        Assert.True(scene.Entity("Parent").IsExpanded);
        Assert.True(scene.Entity("Child").IsExpanded);
        Assert.False(scene.Entity("Grandchild").IsExpanded);
        Assert.True(scene.Row("Leaf").IsSelected);
        Assert.True(scene.Row("Grandchild").IsSelected);
        Assert.True(IsShown(scene, "Grandchild"));
    }

    [AvaloniaFact]
    public async Task AnEntitySelectedInTheTreeExpandsNothing()
    {
        await using var scene = await OpenCollapsedSceneAsync();
        scene.Entity("Parent").IsExpanded = true;
        await PumpAsync();

        var point = scene.ToWindow(scene.NameCenter("Child"));
        scene.Window.MouseDown(point, MouseButton.Left);
        scene.Window.MouseUp(point, MouseButton.Left);
        await PumpAsync();

        Assert.Equal([scene.Entity("Child")], scene.Editor.SelectedContent);
        Assert.False(scene.Entity("Child").IsExpanded);
        Assert.False(scene.Entity("Box").IsExpanded);
    }

    [AvaloniaFact]
    public async Task AnElementSelectedOutsideTheTreeOfTheUIEditorShowsSelected()
    {
        await using var page = await UIEditorFixture.OpenAsync<UIPageAsset>("Page");
        var grid = await page.AddRootAsync("Grid");
        await page.DropAsync([page.Factory("Grid")], page.RowCenter(grid));
        var inner = grid.Children[0];
        await page.WaitForRowAsync(inner);
        await page.DropAsync([page.Factory("Button")], page.RowCenter(inner));
        var button = inner.Children[0];
        await page.WaitForRowAsync(button);
        foreach (var row in page.Tree.GetVisualDescendants().OfType<TreeViewItem>().ToList())
            row.IsExpanded = false;
        await PumpAsync();
        Assert.False(page.TryRow(button) is { IsEffectivelyVisible: true });

        // As the design view selects the element under the mouse
        page.Editor.SelectedContent.Clear();
        page.Editor.SelectedContent.Add(button);
        await PumpAsync();

        Assert.True(page.Row(grid).IsExpanded);
        Assert.True(page.Row(inner).IsExpanded);
        var buttonRow = page.Row(button);
        Assert.True(buttonRow.IsSelected);
        Assert.True(buttonRow.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public async Task AFolderSelectedOutsideTheSolutionExplorerShowsSelected()
    {
        using var fixture = ShellSessionFixture.Create(("Characters/Heroes/Sheet", new SpriteSheetAsset()));
        await fixture.OpenAsync();
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            await PumpAsync();
            var explorer = window.GetVisualDescendants().OfType<SolutionExplorerView>().Single();
            explorer.CollapseAllFolders();
            await PumpAsync();
            var folder = fixture.Asset("Characters/Heroes/Sheet").Directory;
            var tree = window.GetVisualDescendants().OfType<TreeView>().Single(x => x.Name == "SolutionTree");
            Assert.False(tree.TreeContainerFromItem(folder) is TreeViewItem { IsEffectivelyVisible: true });

            // As a double click on a folder of the asset view opens it
            fixture.Session.AssetCollection.SelectedLocations.Clear();
            fixture.Session.AssetCollection.SelectedLocations.Add(folder);
            await PumpAsync();

            var row = Assert.IsType<TreeViewItem>(tree.TreeContainerFromItem(folder));
            Assert.True(row.IsSelected);
            Assert.True(row.IsEffectivelyVisible);
            Assert.All(row.GetVisualAncestors().OfType<TreeViewItem>(), x => Assert.True(x.IsExpanded));
        }
        finally
        {
            await WindowTestHelper.CloseAsync(window);
        }
    }

    [AvaloniaFact]
    public async Task TheFolderOfTheInitialAssetOfThePickerShowsSelected()
    {
        using var fixture = ShellSessionFixture.Create(("Characters/Heroes/Skeleton", new SkeletonAsset()));
        await fixture.OpenAsync();
        var owner = new MainWindow { DataContext = fixture.Main };
        owner.Show();
        try
        {
            var skeleton = fixture.Asset("Characters/Heroes/Skeleton");
            var picker = new AssetPickerViewModel(fixture.Session, [typeof(SkeletonAsset)], initialAsset: skeleton);
            var window = new AssetPickerWindow { DataContext = picker };
            _ = window.ShowDialog<bool>(owner);
            await PumpAsync();

            var tree = window.GetVisualDescendants().OfType<TreeView>().Single(x => x.Name == "Locations");
            var row = Assert.IsType<TreeViewItem>(tree.TreeContainerFromItem(skeleton.Directory));
            Assert.True(row.IsSelected);
            Assert.True(row.IsEffectivelyVisible);
            // The folders and the package above it are expanded, not the folder itself
            Assert.All(row.GetVisualAncestors().OfType<TreeViewItem>(), x => Assert.True(x.IsExpanded));
            window.Close(false);
        }
        finally
        {
            await owner.CloseAsync();
        }
    }

    /// <summary>
    /// Opens the scene of the tree tests with a grandchild under Child and a leaf under Box, every entity collapsed.
    /// </summary>
    private static async Task<SceneTreeDropTests.SceneFixture> OpenCollapsedSceneAsync()
    {
        var scene = await SceneTreeDropTests.OpenSceneAsync();
        try
        {
            var graph = (AssetCompositeHierarchyPropertyGraph<EntityDesign, Entity>)scene.Editor.Asset.PropertyGraph!;
            var child = scene.Entity("Child").AssetSideEntity;
            var box = scene.Entity("Box").AssetSideEntity;
            using (scene.Fixture.Session.UndoRedoService.CreateTransaction())
            {
                var grandchild = new EntityDesign(new Entity("Grandchild"));
                var leaf = new EntityDesign(new Entity("Leaf"));
                graph.AddPartToAsset(new AssetPartCollection<EntityDesign, Entity> { grandchild }, grandchild, child, 0);
                graph.AddPartToAsset(new AssetPartCollection<EntityDesign, Entity> { leaf }, leaf, box, 0);
            }
            await WindowTestHelper.WaitUntilAsync(() => scene.Editor.HierarchyRoot.Children.OfType<EntityViewModel>().Any(x => x.Name == "Box" && x.TransformChildren.Any()), "the new entities");
            // The rows follow the state of their entity again (the scene of the tree tests expands the rows themselves)
            foreach (var row in scene.Tree.GetVisualDescendants().OfType<TreeViewItem>().ToList())
                row.ClearValue(TreeViewItem.IsExpandedProperty);
            foreach (var item in scene.Editor.HierarchyRoot.Children.SelectMany(x => new[] { x }.Concat(Descendants(x))))
                item.IsExpanded = false;
            await PumpAsync();
            return scene;
        }
        catch
        {
            await scene.DisposeAsync();
            throw;
        }
    }

    private static IEnumerable<EntityHierarchyItemViewModel> Descendants(EntityHierarchyItemViewModel item)
        => item.Children.SelectMany(x => new[] { x }.Concat(Descendants(x)));

    /// <summary>
    /// Gets whether the row of an entity exists and is inside the visible part of the tree.
    /// </summary>
    private static bool IsShown(SceneTreeDropTests.SceneFixture scene, string name)
    {
        if (scene.TryRow(name) is not { IsEffectivelyVisible: true } row)
            return false;

        var presenter = scene.Tree.GetVisualDescendants().OfType<ScrollContentPresenter>().First();
        var header = row.GetVisualDescendants().OfType<Control>().First(x => x.Name == "PART_Header" && ReferenceEquals(x.TemplatedParent, row));
        if (header.TranslatePoint(default, presenter) is not { } origin)
            return false;
        return origin.Y >= 0 && origin.Y + header.Bounds.Height <= presenter.Bounds.Height && header.Bounds.Height > 0;
    }

    private static async Task PumpAsync()
    {
        for (var i = 0; i < 3; ++i)
        {
            await WindowTestHelper.PumpAsync();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
    }
}
