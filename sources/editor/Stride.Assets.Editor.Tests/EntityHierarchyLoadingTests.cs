// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Quantum;
using Stride.Core.Mathematics;
using Stride.Engine;
using static Stride.Assets.Editor.Tests.EntityHierarchyEditorTests;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The loading of the hierarchy of the scene editor and its child scenes, as in the WPF scene editor: the edited scene is
/// always loaded, a child scene loads as it was when the editor closed, and only a loaded element can be locked.
/// </summary>
public sealed class EntityHierarchyLoadingTests
{
    [Fact]
    public async Task TheEditedSceneIsLoadedWithItsEntitiesAndUnloadingItUnloadsThem()
    {
        await using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var root = editor.HierarchyRoot;
        var entities = root.InnerSubEntities.BreadthFirstEntities().ToList();

        Assert.True(root.IsLoaded);
        Assert.All(entities, x => Assert.True(x.IsLoaded && x.IsSelectable));
        Assert.True(root.LockCommand.IsEnabled);

        await root.RequestLoading(false);

        Assert.False(root.IsLoaded);
        Assert.All(entities, x => Assert.False(x.IsLoaded || x.IsSelectable));
        // Only a loaded element can be locked
        Assert.False(root.LockCommand.IsEnabled);
        await root.RequestLocking(true);
        Assert.False(root.IsLocked);
        editor.Destroy();
    }

    [Fact]
    public async Task AnEntityAddedToALoadedSceneIsLoadedAndNotOtherwise()
    {
        await using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var parent = FindEntity(editor, "Parent");

        AddEntity(fixture, editor, "Lamp", parent.AssetSideEntity, 1);

        Assert.True(FindEntity(editor, "Lamp").IsLoaded);

        await editor.HierarchyRoot.RequestLoading(false);
        AddEntity(fixture, editor, "Sun", null, 0);

        Assert.False(FindEntity(editor, "Sun").IsLoaded);
        editor.Destroy();
    }

    [Fact]
    public async Task AChildSceneComesAfterTheEntitiesAndIsNotLoadedByDefault()
    {
        await using var fixture = await OpenScenesAsync();
        var editor = await OpenEditorAsync(fixture);
        var root = (SceneRootViewModel)editor.HierarchyRoot;

        var child = Assert.Single(root.ChildScenes);
        Assert.Equal(["Box", "Parent", "ChildScene"], root.Children.Select(x => x.Name));
        Assert.Same(root, child.ParentScene);
        Assert.Equal(new Vector3(5, 0, 0), child.Offset);
        // The entities of the child scene are not those of the edited scene
        Assert.Equal(["Box", "Parent"], root.InnerSubEntities.Select(x => x.Name));
        Assert.Equal(["Lamp"], child.InnerSubEntities.Select(x => x.Name));
        Assert.False(child.IsLoaded);
        Assert.False(child.InnerSubEntities.Single().IsLoaded);

        await child.RequestLoading(true);

        Assert.True(child.IsLoaded);
        Assert.True(child.InnerSubEntities.Single().IsLoaded);
        editor.Destroy();
    }

    [Fact]
    public async Task AChildSceneLoadedWhenTheEditorClosedIsLoadedWhenItOpens()
    {
        await using var fixture = await OpenScenesAsync();
        var editor = await OpenEditorAsync(fixture);
        var child = ((SceneRootViewModel)editor.HierarchyRoot).ChildScenes.Single();
        await child.RequestLoading(true);
        child.SaveSettings();
        editor.Destroy();

        var settings = fixture.Asset<SceneViewModel>("MainScene").Directory.Package.UserSettings.GetValue(PackageSceneSettings.SceneSettings);
        Assert.True(settings[fixture.Asset<SceneViewModel>("ChildScene").Id].SceneLoaded);

        editor = await OpenEditorAsync(fixture);
        child = ((SceneRootViewModel)editor.HierarchyRoot).ChildScenes.Single();
        await ExclusiveDispatcherService.WaitUntilAsync(() => child.IsLoaded, "the child scene to load");

        Assert.True(child.InnerSubEntities.Single().IsLoaded);
        editor.Destroy();
    }

    [Fact]
    public async Task RemovingAChildSceneRemovesItsRootAndTheActiveRootComesBack()
    {
        await using var fixture = await OpenScenesAsync();
        var editor = await OpenEditorAsync(fixture);
        var root = (SceneRootViewModel)editor.HierarchyRoot;
        var child = root.ChildScenes.Single();
        editor.ActiveRoot = child;
        var undoRedo = fixture.Session.UndoRedoService;

        using (var transaction = undoRedo.CreateTransaction())
        {
            child.Delete();
            undoRedo.SetName(transaction, "Delete scene");
        }

        Assert.Empty(root.ChildScenes);
        Assert.Equal(["Box", "Parent"], root.Children.Select(x => x.Name));
        Assert.Same(root, editor.ActiveRoot);
        // The root of the child scene is destroyed once it is unloaded
        await ExclusiveDispatcherService.WaitUntilAsync(() => fixture.Asset<SceneViewModel>("ChildScene").HierarchyRoot is null, "the child scene to unload");

        undoRedo.Undo();

        Assert.Equal(["Box", "Parent", "ChildScene"], root.Children.Select(x => x.Name));
        Assert.NotSame(child, root.ChildScenes.Single());
        editor.Destroy();
    }

    [Fact]
    public async Task LockingASceneLocksItsEntitiesAndItsChildScenesOnlyWithCtrl()
    {
        await using var fixture = await OpenScenesAsync();
        var editor = await OpenEditorAsync(fixture);
        var root = (SceneRootViewModel)editor.HierarchyRoot;
        var child = root.ChildScenes.Single();
        await child.RequestLoading(true);

        root.LockCommand.Execute(false);

        Assert.True(FindEntity(editor, "Child").IsLocked);
        Assert.False(child.IsLocked);
        Assert.False(child.InnerSubEntities.Single().IsLocked);

        root.LockCommand.Execute(false);
        root.LockCommand.Execute(true);

        Assert.True(child.IsLocked);
        Assert.True(child.InnerSubEntities.Single().IsLocked);
        editor.Destroy();
    }

    /// <summary>
    /// The scene of <see cref="OpenSceneAsync"/>, with a child scene "ChildScene" at (5, 0, 0) that has a "Lamp" entity at
    /// (1, 0, 0).
    /// </summary>
    internal static async DispatcherTask<AssetsSessionFixture> OpenScenesAsync()
    {
        var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: true, ("MainScene", new SceneAsset()), ("ChildScene", new SceneAsset { Offset = new Vector3(5, 0, 0) }));
        Assert.False(fixture.Result.HasErrors, fixture.Log);

        var main = fixture.Asset<SceneViewModel>("MainScene");
        var childScene = fixture.Asset<SceneViewModel>("ChildScene");
        var mainGraph = (AssetCompositeHierarchyPropertyGraph<EntityDesign, Entity>)main.PropertyGraph!;
        var childGraph = (AssetCompositeHierarchyPropertyGraph<EntityDesign, Entity>)childScene.PropertyGraph!;
        using (fixture.Session.UndoRedoService.CreateTransaction())
        {
            var box = new EntityDesign(new Entity("Box"));
            var parent = new EntityDesign(new Entity("Parent"));
            var child = new EntityDesign(new Entity("Child"));
            mainGraph.AddPartToAsset(new AssetPartCollection<EntityDesign, Entity> { box }, box, null, 0);
            mainGraph.AddPartToAsset(new AssetPartCollection<EntityDesign, Entity> { parent }, parent, null, 1);
            mainGraph.AddPartToAsset(new AssetPartCollection<EntityDesign, Entity> { child }, child, parent.Entity, 0);
            var lamp = new EntityDesign(new Entity("Lamp") { Transform = { Position = new Vector3(1, 0, 0) } });
            childGraph.AddPartToAsset(new AssetPartCollection<EntityDesign, Entity> { lamp }, lamp, null, 0);
            main.Children.Add(childScene);
        }
        return fixture;
    }
}
