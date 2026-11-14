// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;

using Stride.Engine;

namespace Stride.Rendering.UI
{
    /// <summary>
    /// The processor in charge of updating and drawing the entities having UI components.
    /// </summary>
    public class UIRenderProcessor : EntityProcessor<UIComponent, RenderUIElement>, IEntityComponentRenderProcessor
    {
        public List<RenderUIElement> UIRoots { get; private set; }

        public VisibilityGroup VisibilityGroup { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UIRenderProcessor"/> class.
        /// </summary>
        public UIRenderProcessor()
            : base(typeof(TransformComponent))
        {
            UIRoots = new List<RenderUIElement>();
        }

        public override void Draw(RenderContext gameTime)
        {
            UIRoots.Clear();
            foreach (var spriteStateKeyPair in ComponentDatas)
            {
                var uiComponent = spriteStateKeyPair.Key;
                var renderUIElement = spriteStateKeyPair.Value;
                renderUIElement.Enabled = uiComponent.Enabled;

                if (!renderUIElement.Enabled)
                    continue;

                var isFullScreenChanged = renderUIElement.IsFullScreen != uiComponent.IsFullScreen;

                // TODO GRAPHICS REFACTOR: Proper bounding box.
                //renderSprite.BoundingBox = new BoundingBoxExt(new Vector3(float.NegativeInfinity), new Vector3(float.PositiveInfinity));

                // Copy values from ECS to render object
                renderUIElement.WorldMatrix = uiComponent.Entity.Transform.WorldMatrix;

                renderUIElement.Page = uiComponent.Page;
                renderUIElement.Sampler = uiComponent.Sampler;
                renderUIElement.IsFullScreen = uiComponent.IsFullScreen;
                renderUIElement.Resolution = uiComponent.Resolution;
                renderUIElement.Size = uiComponent.Size;
                renderUIElement.ResolutionStretch = uiComponent.ResolutionStretch;
                renderUIElement.IsBillboard = uiComponent.IsBillboard;
                renderUIElement.SnapText = uiComponent.SnapText;
                renderUIElement.IsFixedSize = uiComponent.IsFixedSize;

                if (isFullScreenChanged ||
                    renderUIElement.RenderGroup != uiComponent.RenderGroup)
                {
                    renderUIElement.IsFullScreen = uiComponent.IsFullScreen;
                    renderUIElement.RenderGroup = uiComponent.RenderGroup;

                    // Forces a re-evaluation of active render stages in the VisibilityGroup for this render object
                    // when a selector-dependent UI property changes (e.g., IsFullScreen or RenderGroup)
                    VisibilityGroup.RenderObjects.Remove(renderUIElement);
                    VisibilityGroup.RenderObjects.Add(renderUIElement);
                }

                UIRoots.Add(renderUIElement);
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
            return new RenderUIElement
            {
                Source = component,

                // RenderGroup must be up to date from the start as VisibilityGroup.ReevaluateActiveRenderStages()
                // reads from it for this objects' initial render stage
                RenderGroup = component.RenderGroup,
                IsFullScreen = component.IsFullScreen
            };
        }

        protected override bool IsAssociatedDataValid(Entity entity, UIComponent component, RenderUIElement associatedData)
        {
            return associatedData.Source == component && component.RenderGroup == associatedData.RenderGroup;
        }
    }
}
