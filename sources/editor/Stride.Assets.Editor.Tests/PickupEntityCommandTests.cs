// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Assets.Editor.Services;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core;
using Stride.Core.Extensions;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Quantum;
using Stride.Engine;
using static Stride.Assets.Editor.Tests.EntityHierarchyEditorTests;
using static Stride.Assets.Editor.Tests.EntityHierarchyLoadingTests;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The Pick buttons of the entity and component properties show the entity picker of the editor on the items of the asset, and
/// set the picked entity or component, as in the WPF editor.
/// </summary>
public sealed class PickupEntityCommandTests
{
    [DataContract]
    public sealed class PickTargetComponent : EntityComponent
    {
        public Entity? Target { get; set; }

        public TransformComponent? Transform { get; set; }
    }

    /// <summary>
    /// The picker of the tests: it builds the view model of the editors' pickers, then selects an item by name.
    /// </summary>
    private sealed class TestEntityPicker(string? pick) : IEntityPickerService
    {
        public EntityPickerViewModel? Picker { get; private set; }

        public Task<IPickedEntity?> PickEntityAsync(EntityHierarchyEditorViewModel editor, Type? componentType, Func<EntityHierarchyItemViewModel, bool> filter)
        {
            Picker = new EntityPickerViewModel(editor, componentType, filter);
            if (pick is null)
                return Task.FromResult<IPickedEntity?>(null);

            Picker.SelectedEntity = Picker.Roots.BreadthFirst(x => x.Children).Single(x => x.Name == pick);
            return Task.FromResult(Picker.PickedEntity);
        }
    }

    [Fact]
    public async Task PickingAnEntitySetsTheProperty()
    {
        await using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var picker = Register(fixture, new TestEntityPicker("Child"));
        var component = AddPickTarget(fixture, FindEntity(editor, "Box"));

        await Pick(fixture, editor, "Box", nameof(PickTargetComponent.Target), PickupEntityCommand.CommandName, fixture.Asset("MainScene"), null);

        Assert.Same(FindEntity(editor, "Child").AssetSideEntity, component.Target);
        // The whole hierarchy of the scene, as in WPF
        Assert.Null(picker.Picker!.ComponentType);
        Assert.Equal(["Box", "Parent"], picker.Picker.Roots.Select(x => x.Name));
        editor.Destroy();
    }

    [Fact]
    public async Task PickingAComponentSetsTheComponentOfThePickedEntity()
    {
        await using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var picker = Register(fixture, new TestEntityPicker("Parent"));
        var component = AddPickTarget(fixture, FindEntity(editor, "Box"));

        await Pick(fixture, editor, "Box", nameof(PickTargetComponent.Transform), PickupEntityComponentCommand.CommandName, fixture.Asset("MainScene"), typeof(TransformComponent));

        Assert.Same(FindEntity(editor, "Parent").AssetSideEntity.Transform, component.Transform);
        Assert.Equal(typeof(TransformComponent), picker.Picker!.ComponentType);
        var parent = picker.Picker.Roots.Single(x => x.Name == "Parent");
        Assert.Equal([0], parent.Components.Select(x => x.Index));
        editor.Destroy();
    }

    [Fact]
    public async Task PickingFromAChildSceneShowsTheEntitiesOfTheChildSceneOnly()
    {
        await using var fixture = await OpenScenesAsync();
        var editor = await OpenEditorAsync(fixture);
        var child = ((SceneRootViewModel)editor.HierarchyRoot).ChildScenes.Single();
        await child.RequestLoading(true);
        var lamp = child.InnerSubEntities.Single();
        var picker = Register(fixture, new TestEntityPicker("Lamp"));
        var component = AddPickTarget(fixture, lamp);

        await Pick(fixture, lamp, nameof(PickTargetComponent.Target), PickupEntityCommand.CommandName, fixture.Asset("ChildScene"), null);

        Assert.Same(lamp.AssetSideEntity, component.Target);
        // A reference can't cross assets: the entities of the root scene are not shown
        var root = Assert.Single(picker.Picker!.Roots);
        Assert.Equal("ChildScene", root.Name);
        Assert.Equal(["Lamp"], root.Children.Select(x => x.Name));
        editor.Destroy();
    }

    [Fact]
    public async Task CancellingThePickerKeepsTheValue()
    {
        await using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        Register(fixture, new TestEntityPicker(null));
        var component = AddPickTarget(fixture, FindEntity(editor, "Box"));
        component.Target = FindEntity(editor, "Parent").AssetSideEntity;

        await Pick(fixture, editor, "Box", nameof(PickTargetComponent.Target), PickupEntityCommand.CommandName, fixture.Asset("MainScene"), null);

        Assert.Same(FindEntity(editor, "Parent").AssetSideEntity, component.Target);
        editor.Destroy();
    }

    [Fact]
    public async Task TheSearchTextNarrowsThePickerAndAFolderCannotBePicked()
    {
        await using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var picker = new EntityPickerViewModel(editor, null);

        picker.SearchText = "chi";

        var parent = Assert.Single(picker.Roots);
        Assert.Equal("Parent", parent.Name);
        Assert.Equal(["Child"], parent.Children.Select(x => x.Name));
        picker.SelectedEntity = parent.Children[0];
        Assert.True(picker.SelectionValid);
        // A component is picked among the components of the type only: an entity without one can't be picked
        var components = new EntityPickerViewModel(editor, typeof(LightComponent));
        Assert.Empty(components.Roots);
        editor.Destroy();
    }

    private static TestEntityPicker Register(AssetsSessionFixture fixture, TestEntityPicker picker)
    {
        fixture.Session.ServiceProvider.RegisterService(picker);
        return picker;
    }

    private static PickTargetComponent AddPickTarget(AssetsSessionFixture fixture, EntityViewModel entity)
    {
        var component = new PickTargetComponent();
        var undoRedo = fixture.Session.UndoRedoService;
        using var transaction = undoRedo.CreateTransaction();
        var components = fixture.Session.AssetNodeContainer.GetNode(entity.AssetSideEntity)[nameof(Entity.Components)].Target!;
        components.Add(component, new NodeIndex(entity.AssetSideEntity.Components.Count));
        undoRedo.SetName(transaction, "Add component");
        return component;
    }

    private static Task Pick(AssetsSessionFixture fixture, EntityHierarchyEditorViewModel editor, string entity, string property, string command, AssetViewModel owner, Type? type)
    {
        return Pick(fixture, FindEntity(editor, entity), property, command, owner, type);
    }

    private static async Task Pick(AssetsSessionFixture fixture, EntityViewModel entity, string property, string command, AssetViewModel owner, Type? type)
    {
        var graph = await fixture.Session.AssetViewProperties.GenerateSelectionPropertiesAsync([entity]);
        var components = graph!.RootNode.GetChild(nameof(Entity.Components))!;
        var node = components.Children.Single(x => x.NodeValue is PickTargetComponent).GetChild(property)!;
        var wrapper = Assert.IsAssignableFrom<NodePresenterCommandWrapper>(node.GetCommand(command));
        await wrapper.Invoke(Tuple.Create<object?, object?>(owner, type));
    }
}
