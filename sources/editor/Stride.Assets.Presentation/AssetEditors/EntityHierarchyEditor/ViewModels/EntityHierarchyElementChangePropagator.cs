// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Stride.Core.Assets.Quantum;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Quantum;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.AssetEditors.GameEditor.ViewModels;
using Stride.Assets.Presentation.Quantum;
using Stride.Engine;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels
{
    public class EntityHierarchyElementChangePropagator : GameEditorChangePropagator<EntityDesign, Entity, EntityViewModel>
    {
        public EntityHierarchyElementChangePropagator([NotNull] EntityHierarchyEditorViewModel editor, [NotNull] EntityViewModel owner, [NotNull] Entity assetSidePart)
            : base(editor, owner, assetSidePart)
        {
        }

        protected override async Task<bool> PropagatePartReference(IGraphNode gameSideNode, object value, INodeChangeEventArgs e)
        {
            var component = value as EntityComponent;
            // Don't propagate if we're updating the EntityComponentCollection (not a reference),
            // or if we're updating TransformComponent.Children (handled as a part addition/removal)
            var nodeIndex = (e as ItemChangeEventArgs)?.Index ?? NodeIndex.Empty;
            if (component != null && e.Node.Type != typeof(EntityComponentCollection) && !((EntityHierarchyPropertyGraph)Owner.Asset.PropertyGraph).IsChildPartReference(e.Node, nodeIndex))
            {
                var index = component.Entity.Components.IndexOf(component);
                var partId = new AbsoluteId(Owner.Id.AssetId, component.Entity.Id); // FIXME: what about cross-asset references?
                await Editor.Controller.InvokeAsync(() =>
                {
                    var gameSideEntity = (Entity)Editor.Controller.FindGameSidePart(partId);
                    var gameSideComponent = gameSideEntity?.Components[index];
                    UpdateGameSideContent(gameSideNode, gameSideComponent, e.ChangeType, nodeIndex);
                });
                return true;
            }
            return await base.PropagatePartReference(gameSideNode, value, e);
        }

        protected override object CloneObjectForGameSide(object assetSideObject, IAssetNode assetNode, IGraphNode gameSideNode)
        {
            // TODO: this is very similar to what AssetPropertyGraph.CloneValueFromBase is doing, except that the base cloner is different. Try to factorize!
            if (gameSideNode.Type == typeof(EntityComponentCollection) && assetSideObject is TransformComponent)
            {
                // We never clone TransformComponent, we cannot replace them. Instead, return the existing one.
                var transformComponent = (TransformComponent)gameSideNode.Retrieve(new NodeIndex(0));
                // We still reset the Entity to null to make sure it works nicely with reconcilation, etc.
                transformComponent.Entity = null;
                return transformComponent;
            }

            var gameSideValue = base.CloneObjectForGameSide(assetSideObject, assetNode, gameSideNode);

            // Important: In case of component, because of the Entitycomponent serializer is serializing the entity (with all its components)
            // We are clearing the entity of the component, so that this object can be added to the game side part.
            var entityComponent = gameSideValue as EntityComponent;
            if (entityComponent != null)
            {
                entityComponent.Entity = null;
            }

            return gameSideValue;
        }
    }
}
