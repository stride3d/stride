// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets;
using Stride.Core.Assets.Quantum;
using Stride.Engine;
using Stride.GameStudio.Avalonia.Tests.Helpers;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// Child scenes in the tree of the scene editor: the load button loads and unloads a child scene,
/// its row says when it is unloaded, its entities are grey until it loads, and the active scene is in bold.
/// </summary>
public sealed class HierarchicalScenesTests
{
    [AvaloniaFact]
    public async Task TheLoadButtonOfAChildSceneLoadsAndUnloadsItAndTheRowFollows()
    {
        await using var scene = await SceneTreeDropTests.OpenSceneAsync();
        var main = (SceneViewModel)scene.Fixture.Asset("MainScene");
        var childAsset = (SceneViewModel)scene.Fixture.Asset("ChildScene");
        using (scene.Fixture.Session.UndoRedoService.CreateTransaction())
        {
            var graph = (AssetCompositeHierarchyPropertyGraph<EntityDesign, Entity>)childAsset.PropertyGraph!;
            var lamp = new EntityDesign(new Entity("Lamp"));
            graph.AddPartToAsset(new AssetPartCollection<EntityDesign, Entity> { lamp }, lamp, null, 0);
            main.Children.Add(childAsset);
        }
        var root = (SceneRootViewModel)scene.Editor.HierarchyRoot;
        var child = root.ChildScenes.Single();
        await WindowTestHelper.WaitUntilAsync(() =>
        {
            foreach (var item in scene.Tree.GetVisualDescendants().OfType<TreeViewItem>())
                item.IsExpanded = true;
            return scene.TryRow("Lamp") is not null;
        }, "the rows of the child scene");
        await WindowTestHelper.PumpAsync();

        // Unloaded: the row says so, its lock is off, its entities are grey; the edited scene is the active one, in bold
        Assert.False(child.IsLoaded);
        Assert.True(IsShown(scene.Row("ChildScene"), "(Unloaded)"));
        Assert.False(LockButton(scene.Row("ChildScene")).IsEnabled);
        Assert.True(Name(scene.Row("Lamp"), "Lamp").Classes.Contains("unloaded"));
        Assert.True(Name(scene.Row("MainScene"), "MainScene").Classes.Contains("active"));
        Assert.False(Name(scene.Row("ChildScene"), "ChildScene").Classes.Contains("active"));
        Assert.False(IsShown(scene.Row("MainScene"), "(Unloaded)"));

        // The load button loads it (without Ctrl, its own child scenes stay as they are)
        var load = LoadButton(scene.Row("ChildScene"));
        load.Command!.Execute(false);
        await WindowTestHelper.WaitUntilAsync(() => child.IsLoaded, "the child scene to load");
        await WindowTestHelper.PumpAsync();
        Assert.False(IsShown(scene.Row("ChildScene"), "(Unloaded)"));
        Assert.True(LockButton(scene.Row("ChildScene")).IsEnabled);
        Assert.False(Name(scene.Row("Lamp"), "Lamp").Classes.Contains("unloaded"));
        Assert.True(child.InnerSubEntities.Single().IsLoaded);

        // Set as active scene: the bold follows
        scene.Editor.ActiveRoot = child;
        await WindowTestHelper.PumpAsync();
        Assert.True(Name(scene.Row("ChildScene"), "ChildScene").Classes.Contains("active"));
        Assert.False(Name(scene.Row("MainScene"), "MainScene").Classes.Contains("active"));

        // And unloads it again
        load.Command!.Execute(false);
        await WindowTestHelper.WaitUntilAsync(() => !child.IsLoaded && !child.IsLoading, "the child scene to unload");
        await WindowTestHelper.PumpAsync();
        Assert.True(IsShown(scene.Row("ChildScene"), "(Unloaded)"));
        Assert.True(Name(scene.Row("Lamp"), "Lamp").Classes.Contains("unloaded"));
        Assert.False(child.InnerSubEntities.Single().IsLoaded);
    }

    /// <summary>
    /// Gets the header of a row: its own controls, not those of its children.
    /// </summary>
    private static IEnumerable<Control> Header(TreeViewItem row)
    {
        var header = row.GetVisualDescendants().OfType<Control>().First(x => x.Name == "PART_HeaderPresenter" && ReferenceEquals(x.TemplatedParent, row));
        return header.GetVisualDescendants().OfType<Control>();
    }

    private static Button LoadButton(TreeViewItem row)
    {
        return Header(row).OfType<Button>().Single(x => x.Command == ((EntityHierarchyElementViewModel)row.DataContext!).LoadCommand);
    }

    private static Button LockButton(TreeViewItem row)
    {
        return Header(row).OfType<Button>().Single(x => x.Command == ((EntityHierarchyElementViewModel)row.DataContext!).LockCommand);
    }

    private static TextBlock Name(TreeViewItem row, string name)
    {
        return Header(row).OfType<TextBlock>().First(x => x.Text == name);
    }

    private static bool IsShown(TreeViewItem row, string text)
    {
        return Header(row).OfType<TextBlock>().Any(x => x.Text == text && x.IsEffectivelyVisible);
    }
}
