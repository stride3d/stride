// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Rendering;

namespace Stride.Engine.Processors
{
    public class UIElementLinkProcessor : EntityProcessor<UIElementLinkComponent>
    {
        public UIElementLinkProcessor()
            : base(typeof(TransformComponent))
        {
        }

        protected override void OnEntityComponentRemoved(Entity entity, UIElementLinkComponent component, UIElementLinkComponent data)
        {
            // Reset TransformLink
            if (entity.Transform.TransformLink is UIElementTransformLink)
                entity.Transform.TransformLink = null;
        }

        public override void Draw(RenderContext context)
        {
            foreach (var item in ComponentDatas)
            {
                var entity = item.Key.Entity;
                var modelNodeLink = item.Value;
                var transformComponent = entity.Transform;
                var transformLink = transformComponent.TransformLink as UIElementTransformLink;

                // Try to use Target, otherwise Parent
                var uiComponent = modelNodeLink.Target;
                var uiEntity = uiComponent?.Entity ?? transformComponent.Parent?.Entity;

                // Check against Entity instead of ModelComponent to avoid having to get ModelComponent when nothing changed)
                if (transformLink == null || transformLink.NeedsRecreate(uiEntity, modelNodeLink.NodeName))
                {
                    // In case we use parent, modelComponent still needs to be resolved
                    if (uiComponent == null)
                        uiComponent = uiEntity?.Get<UIComponent>();

                    // If model component is not parent, we want to use forceRecursive because we might want to update this link before the modelComponent.Entity is updated (depending on order of transformation update)
                    transformComponent.TransformLink = uiComponent != null ? new UIElementTransformLink(uiComponent, modelNodeLink.Camera, modelNodeLink.NodeName, uiEntity != transformComponent.Parent?.Entity) : null;
                }
            }
        }
    }
}
