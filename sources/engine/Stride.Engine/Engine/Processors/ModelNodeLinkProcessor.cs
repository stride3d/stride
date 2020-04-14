// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Rendering;

namespace Stride.Engine.Processors
{
    public class ModelNodeLinkProcessor : EntityProcessor<ModelNodeLinkComponent>
    {
        public Dictionary<ModelNodeLinkComponent, ModelNodeLinkComponent>.KeyCollection ModelNodeLinkComponents => ComponentDatas.Keys;

        public ModelNodeLinkProcessor()
            : base(typeof(TransformComponent))
        {
            Order = -300;
        }

        protected override void OnEntityComponentAdding(Entity entity, ModelNodeLinkComponent component, ModelNodeLinkComponent data)
        {
            //populate the valid property
            component.ValidityCheck();

            entity.EntityManager.HierarchyChanged += component.OnHierarchyChanged;
        }

        protected override void OnEntityComponentRemoved(Entity entity, ModelNodeLinkComponent component, ModelNodeLinkComponent data)
        {
            // Reset TransformLink
            if (entity.Transform.TransformLink is ModelNodeTransformLink)
                entity.Transform.TransformLink = null;

            entity.EntityManager.HierarchyChanged -= component.OnHierarchyChanged;
        }

        public override void Draw(RenderContext context)
        {
            foreach (var item in ComponentDatas)
            {
                var entity = item.Key.Entity;
                var transformComponent = entity.Transform;

                if (item.Value.IsValid)
                {
                    var modelNodeLink = item.Value;
                    var transformLink = transformComponent.TransformLink as ModelNodeTransformLink;

                    // Try to use Target, otherwise Parent
                    var modelComponent = modelNodeLink.Target;
                    var modelEntity = modelComponent?.Entity ?? transformComponent.Parent?.Entity;

                    // Check against Entity instead of ModelComponent to avoid having to get ModelComponent when nothing changed)
                    if (transformLink == null || transformLink.NeedsRecreate(modelEntity, modelNodeLink.NodeName))
                    {
                        // In case we use parent, modelComponent still needs to be resolved
                        if (modelComponent == null)
                            modelComponent = modelEntity?.Get<ModelComponent>();

                        // If model component is not parent, we want to use forceRecursive because we might want to update this link before the modelComponent.Entity is updated (depending on order of transformation update)
                        transformComponent.TransformLink = modelComponent != null
                            ? new ModelNodeTransformLink(modelComponent, modelNodeLink.NodeName)
                            : null;
                    }
                }
                else
                {
                    transformComponent.TransformLink = null;
                }
            }
        }
    }
}
