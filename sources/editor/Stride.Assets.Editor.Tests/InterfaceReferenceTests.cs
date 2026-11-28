// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Quantum;
using Stride.Engine;
using static Stride.Assets.Editor.Tests.EntityHierarchyEditorTests;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// A property whose type is an interface of both a content type and a component (Scene and EntityComponent are
/// both IIdentifiable) takes an asset or an entity.
/// </summary>
public sealed class InterfaceReferenceTests
{
    [DataContract]
    public sealed class IdentifiableTargetComponent : EntityComponent
    {
        public IIdentifiable? Target { get; set; }
    }

    [Fact]
    public async Task ThePropertyAcceptsAnAssetAndAnEntity()
    {
        await using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var box = FindEntity(editor, "Box");
        AddComponent(fixture, box, new IdentifiableTargetComponent());
        AddComponent(fixture, FindEntity(editor, "Parent"), new IdentifiableTargetComponent());

        var graph = await fixture.Session.AssetViewProperties.GenerateSelectionPropertiesAsync([box]);

        Assert.True(graph is not null, fixture.Session.AssetViewProperties.FallbackMessage);
        var components = graph.RootNode.GetChild(nameof(Entity.Components))!;
        var target = components.Children.Single(x => x.NodeValue is IdentifiableTargetComponent).GetChild(nameof(IdentifiableTargetComponent.Target))!;
        var reference = Assert.IsAssignableFrom<IAddReferenceViewModel>(target.AssociatedData[ReferenceData.AddReferenceViewModel]);
        reference.SetTargetNode(target);
        Assert.True(reference.CanAddChildren([FindEntity(editor, "Parent")], AddChildModifiers.None, out var message), message);
        Assert.True(reference.CanAddChildren([fixture.Asset("MainScene")], AddChildModifiers.None, out message), message);
        editor.Destroy();
    }

    private static void AddComponent(AssetsSessionFixture fixture, EntityViewModel entity, EntityComponent component)
    {
        var undoRedo = fixture.Session.UndoRedoService;
        using var transaction = undoRedo.CreateTransaction();
        var components = fixture.Session.AssetNodeContainer.GetNode(entity.AssetSideEntity)[nameof(Entity.Components)].Target!;
        components.Add(component, new NodeIndex(entity.AssetSideEntity.Components.Count));
        undoRedo.SetName(transaction, "Add component");
    }
}
