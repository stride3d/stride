// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Rendering;

namespace Xenko.Rendering.UI
{
    /// <summary>
    /// The processor in charge of updating and drawing the entities having sprite components.
    /// </summary>
    public class UIRenderProcessor : EntityProcessor<UIComponent, RenderUIElement>, IEntityComponentRenderProcessor
    {
        public VisibilityGroup VisibilityGroup { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UIRenderProcessor"/> class.
        /// </summary>
        public UIRenderProcessor()
            : base(typeof(TransformComponent))
        {
        }
        
        public override void Draw(RenderContext gameTime)
        {
            foreach (var spriteStateKeyPair in ComponentDatas)
            {
                var uiComponent = spriteStateKeyPair.Key;
                var renderUIElement = spriteStateKeyPair.Value;
                renderUIElement.Enabled = uiComponent.Enabled;

                if (renderUIElement.Enabled)
                {
                    if (uiComponent.IsFullScreen == false) {
                        renderUIElement.BoundingBox.Center = uiComponent.Entity.Transform.WorldPosition();
                        renderUIElement.BoundingBox.Extent = uiComponent.Size * uiComponent.Entity.Transform.WorldScale() * 0.5f;
                    } else {
                        renderUIElement.BoundingBox.Extent = Vector3.Zero; // always draw this
                    }

                    // Copy values from ECS to render object
                    renderUIElement.WorldMatrix = uiComponent.Entity.Transform.WorldMatrix;

                    renderUIElement.RenderGroup = uiComponent.RenderGroup;
                    renderUIElement.DistanceSortFudge = uiComponent.DistanceSortFudge;

                    renderUIElement.Page = uiComponent.Page;
                    renderUIElement.IsFullScreen = uiComponent.IsFullScreen;
                    renderUIElement.Resolution = uiComponent.Resolution;
                    renderUIElement.Size = uiComponent.Size;
                    renderUIElement.ResolutionStretch = uiComponent.ResolutionStretch;
                    renderUIElement.IsBillboard = uiComponent.IsBillboard;
                    renderUIElement.SnapText = uiComponent.SnapText;
                    renderUIElement.IsFixedSize = uiComponent.IsFixedSize;
                }
            }
        }

        protected override void OnEntityComponentAdding(Entity entity, UIComponent uiComponent, RenderUIElement renderUIElement)
        {
            VisibilityGroup.RenderObjects.Add(renderUIElement);
        }

        protected override void OnEntityComponentRemoved(Entity entity, UIComponent uiComponent, RenderUIElement renderUIElement)
        {
            VisibilityGroup.RenderObjects.Remove(renderUIElement);
        }

        protected override RenderUIElement GenerateComponentData(Entity entity, UIComponent component)
        {
            return new RenderUIElement { Source = component };
        }

        protected override bool IsAssociatedDataValid(Entity entity, UIComponent component, RenderUIElement associatedData)
        {
            return associatedData.Source == component;
        }
    }
}
