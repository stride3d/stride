// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core.Mathematics;
using Xenko.Engine;

namespace Xenko.Rendering.Sprites
{
    /// <summary>
    /// The processor in charge of updating and drawing the entities having sprite components.
    /// </summary>
    internal class SpriteRenderProcessor : EntityProcessor<SpriteComponent, SpriteRenderProcessor.SpriteInfo>, IEntityComponentRenderProcessor
    {
        public VisibilityGroup VisibilityGroup { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpriteRenderProcessor"/> class.
        /// </summary>
        public SpriteRenderProcessor()
            : base(typeof(TransformComponent))
        {
        }

        public override void Draw(RenderContext gameTime)
        {
            foreach (var spriteStateKeyPair in ComponentDatas)
            {
                var renderSprite = spriteStateKeyPair.Value.RenderSprite;
                var currentSprite = renderSprite.SpriteComponent.CurrentSprite;

                renderSprite.Enabled = renderSprite.SpriteComponent.Enabled;

                if (renderSprite.Enabled)
                {
                    renderSprite.CalculateBoundingBox();
                }

                // TODO Should we allow adding RenderSprite without a CurrentSprite instead? (if yes, need some improvement in RenderSystem)
                if (spriteStateKeyPair.Value.Active != (currentSprite != null))
                {
                    spriteStateKeyPair.Value.Active = (currentSprite != null);
                    if (spriteStateKeyPair.Value.Active)
                        VisibilityGroup.RenderObjects.Add(renderSprite);
                    else
                        VisibilityGroup.RenderObjects.Remove(renderSprite);
                }
            }
        }

        protected override void OnEntityComponentRemoved(Entity entity, SpriteComponent component, SpriteInfo data)
        {
            VisibilityGroup.RenderObjects.Remove(data.RenderSprite);
        }

        protected override SpriteInfo GenerateComponentData(Entity entity, SpriteComponent spriteComponent)
        {
            return new SpriteInfo
            {
                RenderSprite = new RenderSprite
                {
                    SpriteComponent = spriteComponent,
                    TransformComponent = entity.Transform,
                },
            };
        }

        protected override bool IsAssociatedDataValid(Entity entity, SpriteComponent spriteComponent, SpriteInfo associatedData)
        {
            return
                spriteComponent == associatedData.RenderSprite.SpriteComponent &&
                entity.Transform == associatedData.RenderSprite.TransformComponent;
        }

        public class SpriteInfo
        {
            public bool Active;
            public RenderSprite RenderSprite;
        }
    }
}
