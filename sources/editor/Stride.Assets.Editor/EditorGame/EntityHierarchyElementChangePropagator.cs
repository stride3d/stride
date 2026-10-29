// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Entities;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Core;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Core.Quantum;
using Stride.Engine;

namespace Stride.Assets.Editor.EditorGame;

/// <summary>
/// Replays the changes of an entity of a scene or a prefab on its game-side clone.
/// </summary>
/// <remarks>
/// The cross-platform counterpart of the WPF <c>EntityHierarchyElementChangePropagator</c>.
/// </remarks>
public sealed class EntityHierarchyElementChangePropagator : GameEditorChangePropagator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EntityHierarchyElementChangePropagator"/> class (UI thread).
    /// </summary>
    /// <param name="editor">The editor of the asset.</param>
    /// <param name="controller">The controller of the game of the editor.</param>
    /// <param name="asset">The asset the entity belongs to.</param>
    /// <param name="ownerId">The identifier of the entity.</param>
    /// <param name="assetSideEntity">The asset-side entity.</param>
    public EntityHierarchyElementChangePropagator(AssetEditorViewModel editor, IEditorGameController controller, AssetViewModel asset, AbsoluteId ownerId, Entity assetSideEntity)
        : base(editor, controller, asset, ownerId, assetSideEntity)
    {
    }

    /// <inheritdoc />
    protected override async Task<bool> PropagatePartReference(IGraphNode gameSideNode, object? value, INodeChangeEventArgs e)
    {
        var propertyGraph = (AssetCompositeHierarchyPropertyGraph<EntityDesign, Entity>)Asset.PropertyGraph;
        var index = (e as ItemChangeEventArgs)?.Index ?? NodeIndex.Empty;

        // The addition or removal of a child entity is not a change of this entity: the editor adds and removes the clones
        if (propertyGraph.IsChildPartReference(e.Node, index))
            return true;

        switch (value)
        {
            // A reference to a component of another entity (outside of a component collection) targets the game-side component
            case EntityComponent component when e.Node.Type != typeof(EntityComponentCollection):
            {
                var componentIndex = component.Entity.Components.IndexOf(component);
                var partId = new AbsoluteId(OwnerId.AssetId, component.Entity.Id);
                await Controller.InvokeAsync(() =>
                {
                    var gameSideEntity = (Entity?)Controller.FindGameSidePart(partId);
                    var gameSideComponent = gameSideEntity?.Components[componentIndex];
                    UpdateGameSideContent(gameSideNode, gameSideComponent, e.ChangeType, index);
                });
                return true;
            }
            // A reference to another entity targets the game-side entity
            case Entity entity:
            {
                var partId = new AbsoluteId(OwnerId.AssetId, entity.Id);
                await Controller.InvokeAsync(() =>
                {
                    var gameSideEntity = Controller.FindGameSidePart(partId);
                    UpdateGameSideContent(gameSideNode, gameSideEntity, e.ChangeType, index);
                });
                return true;
            }
            default:
                return false;
        }
    }

    /// <inheritdoc />
    protected override object? CloneObjectForGameSide(object? assetSideObject, IAssetNode assetNode, IGraphNode gameSideNode)
    {
        // The transform component cannot be cloned alone: the game-side entity keeps its own
        if (gameSideNode.Type == typeof(EntityComponentCollection) && assetSideObject is TransformComponent)
        {
            var transformComponent = (TransformComponent)gameSideNode.Retrieve(new NodeIndex(0));
            transformComponent.Entity = null;
            return transformComponent;
        }

        var gameSideValue = base.CloneObjectForGameSide(assetSideObject, assetNode, gameSideNode);
        // A cloned component is not attached yet: the component collection of the game-side entity attaches it
        if (gameSideValue is EntityComponent entityComponent)
            entityComponent.Entity = null;
        return gameSideValue;
    }
}
