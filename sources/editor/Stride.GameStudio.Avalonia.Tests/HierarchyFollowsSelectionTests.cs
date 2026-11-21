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
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets;
using Stride.Core.Assets.Quantum;
using Stride.Engine;
using Stride.GameStudio.Avalonia.Tests.Helpers;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The tree of the scene editor follows the selection made outside it (the scene view, the selection history, the commands):
/// the parents of the selected entities expand and the row of the last one scrolls into view; a selection made in the
/// tree expands nothing.
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
