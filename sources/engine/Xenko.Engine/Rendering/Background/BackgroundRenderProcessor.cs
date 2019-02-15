// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Mathematics;
using Xenko.Engine;

namespace Xenko.Rendering.Background
{
    /// <summary>
    /// A default entity processor for <see cref="BackgroundComponent"/>.
    /// </summary>
    public class BackgroundRenderProcessor : EntityProcessor<BackgroundComponent, RenderBackground>, IEntityComponentRenderProcessor
    {
        public VisibilityGroup VisibilityGroup { get; set; }

        /// <summary>
        /// Gets the active background.
        /// </summary>
        /// <value>The active background.</value>
        public RenderBackground ActiveBackground { get; private set; }        

        /// <inheritdoc />
        protected internal override void OnSystemRemove()
        {
            if (ActiveBackground != null)
            {
                VisibilityGroup.RenderObjects.Remove(ActiveBackground);
                ActiveBackground = null;
            }

            base.OnSystemRemove();
        }

        /// <inheritdoc />
        protected override RenderBackground GenerateComponentData(Entity entity, BackgroundComponent component)
        {
            return new RenderBackground { Source = component, RenderGroup = component.RenderGroup };
        }

        /// <inheritdoc />
        protected override bool IsAssociatedDataValid(Entity entity, BackgroundComponent component, RenderBackground associatedData)
        {
            return component == associatedData.Source && component.RenderGroup == associatedData.RenderGroup;
        }

        /// <inheritdoc />
        public override void Draw(RenderContext context)
        {
            var previousBackground = ActiveBackground;
            ActiveBackground = null;

            // Start by making it not visible
            foreach (var entityKeyPair in ComponentDatas)
            {
                var backgroundComponent = entityKeyPair.Key;
                var renderBackground = entityKeyPair.Value;
                if (backgroundComponent.Enabled && backgroundComponent.Texture != null)
                {
                    // Select the first active background
                    renderBackground.Is2D = backgroundComponent.Is2D;
                    renderBackground.Texture = backgroundComponent.Texture;
                    renderBackground.Intensity = backgroundComponent.Intensity;
                    renderBackground.RenderGroup = backgroundComponent.RenderGroup;
                    renderBackground.Rotation = Quaternion.RotationMatrix(backgroundComponent.Entity.Transform.WorldMatrix);

                    ActiveBackground = renderBackground;
                    break;
                }
            }

            if (ActiveBackground != previousBackground)
            {
                if (previousBackground != null)
                    VisibilityGroup.RenderObjects.Remove(previousBackground);
                if (ActiveBackground != null)
                    VisibilityGroup.RenderObjects.Add(ActiveBackground);
            }
        }
    }
}
