// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.EntityFactories;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Engine;
using static Stride.Assets.Editor.Tests.EntityHierarchyEditorTests;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The creation of entities and folders in the scene editor, as the "Create" menus of the WPF scene editor: an empty
/// entity or an entity of a factory, at the root or in the selection, each one a single entry of the undo history.
/// </summary>
public sealed class EntityCreationTests
{
    [Fact]
    public async Task AnEmptyEntityGoesLastAtTheRootSelectedAndBeingRenamed()
    {
        using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        editor.SelectedContent.Add(FindEntity(editor, "Parent"));
        var names = RecordUndoNames(fixture);

        editor.CreateEntityInRootCommand.Execute(null);

        Assert.Equal(["Box", "Parent", "Entity"], editor.HierarchyRoot.Children.Select(x => x.Name));
        var entity = (EntityViewModel)Assert.Single(editor.SelectedContent);
        Assert.Equal("Entity", entity.Name);
        Assert.True(entity.IsEditing);
        Assert.Equal(["Create entity 'Entity'"], names);

        fixture.Session.UndoRedoService.Undo();

        Assert.Equal(["Box", "Parent"], editor.HierarchyRoot.Children.Select(x => x.Name));
        editor.Destroy();
    }

    [Fact]
    public async Task AnEntityCreatedInTheSelectionGoesUnderTheSelectedEntityOrFolder()
    {
        using var fixture = await OpenSceneAsync();
        EntityHierarchyFolderTests.AddEntity(fixture, "Sun", "Lights", parent: null, index: 2);
        var editor = await OpenEditorAsync(fixture);
        var parent = FindEntity(editor, "Parent");
        editor.SelectedContent.Add(parent);

        editor.CreateEntityInSelectionCommand.Execute(new PointLightEntityFactory());

        var light = Assert.IsType<EntityViewModel>(parent.Children[1]);
        Assert.Equal("Point light", light.Name);
        Assert.IsType<Stride.Rendering.Lights.LightPoint>(light.AssetSideEntity.Get<LightComponent>()!.Type);

        var lights = (EntityFolderViewModel)editor.HierarchyRoot.Children.Single(x => x.Name == "Lights");
        editor.SelectedContent.Clear();
        editor.SelectedContent.Add(lights);

        editor.CreateEntityInSelectionCommand.Execute(null);

        Assert.Equal(["Sun", "Entity"], lights.Children.Select(x => x.Name));
        Assert.Equal("Lights", ((EntityViewModel)lights.Children[1]).EntityDesign.Folder);
        editor.Destroy();
    }

    [Fact]
    public async Task AFactoryThatAsksForAnAssetCreatesNothingWhenThePickerIsCancelled()
    {
        using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var dialogs = (FakeEditorDialogService)fixture.ServiceProvider.Get<IEditorDialogService>();
        var names = RecordUndoNames(fixture);

        editor.CreateEntityInRootCommand.Execute(new ModelEntityFactory());

        var picker = Assert.Single(dialogs.AssetPickers);
        Assert.Equal("Select the asset to use for this entity", picker.Message);
        Assert.Equal(["Box", "Parent"], editor.HierarchyRoot.Children.Select(x => x.Name));
        Assert.Empty(names);
        editor.Destroy();
    }

    [Fact]
    public async Task ANewFolderHasAFreeNameAndTheUndoRemovesIt()
    {
        using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var names = RecordUndoNames(fixture);

        editor.CreateFolderInRootCommand.Execute();
        editor.CreateFolderInRootCommand.Execute();

        var folders = editor.HierarchyRoot.Folders.Select(x => x.Name).ToList();
        Assert.Equal(2, folders.Count);
        Assert.Contains("Folder", folders);
        Assert.Equal(2, folders.Distinct().Count());
        var selected = Assert.IsType<EntityFolderViewModel>(Assert.Single(editor.SelectedContent));
        Assert.True(selected.IsEditing);
        Assert.Equal(["Create folder 'Folder'", $"Create folder '{selected.Name}'"], names);

        fixture.Session.UndoRedoService.Undo();
        fixture.Session.UndoRedoService.Undo();

        Assert.Empty(editor.HierarchyRoot.Folders);
        editor.Destroy();
    }

    [Fact]
    public async Task AFolderCreatedInTheSelectionGoesUnderTheSelectedEntity()
    {
        using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var parent = FindEntity(editor, "Parent");
        editor.SelectedContent.Add(parent);

        editor.CreateFolderInSelectionCommand.Execute();

        var folder = Assert.IsType<EntityFolderViewModel>(parent.Children[0]);
        Assert.Same(parent, folder.Owner);
        Assert.True(parent.IsExpanded);
        editor.Destroy();
    }

    private static List<string?> RecordUndoNames(AssetsSessionFixture fixture)
    {
        var undoRedo = fixture.Session.UndoRedoService;
        var names = new List<string?>();
        undoRedo.Done += (_, e) => names.Add(undoRedo.GetName(e.Transaction));
        return names;
    }
}
