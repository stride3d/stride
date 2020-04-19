// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.Rendering.Sprites
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
                var spriteComponent = spriteStateKeyPair.Key;
                var renderSprite = spriteStateKeyPair.Value.RenderSprite;
                var currentSprite = spriteComponent.CurrentSprite;

                renderSprite.Enabled = spriteComponent.Enabled;

                if (renderSprite.Enabled)
                {
                    renderSprite.WorldMatrix = spriteComponent.Entity.Transform.WorldMatrix;
                    renderSprite.RotationEulerZ = spriteComponent.Entity.Transform.RotationEulerXYZ.Z;

                    renderSprite.RenderGroup = spriteComponent.RenderGroup;

                    renderSprite.Sprite = currentSprite;
                    renderSprite.SpriteType = spriteComponent.SpriteType;
                    renderSprite.IgnoreDepth = spriteComponent.IgnoreDepth;
                    renderSprite.Sampler = spriteComponent.Sampler;
                    renderSprite.BlendMode = spriteComponent.BlendMode;
                    renderSprite.Swizzle = spriteComponent.Swizzle;
                    renderSprite.IsAlphaCutoff = spriteComponent.IsAlphaCutoff;
                    renderSprite.PremultipliedAlpha = spriteComponent.PremultipliedAlpha;
                    // Use intensity for RGB part
                    renderSprite.Color = spriteComponent.Color * spriteComponent.Intensity;
                    renderSprite.Color.A = spriteComponent.Color.A;

                    renderSprite.CalculateBoundingBox();
                }

                // TODO Should we allow adding RenderSprite without a CurrentSprite instead? (if yes, need some improvement in RenderSystem)
                var isActive = (currentSprite != null) && renderSprite.Enabled;
                if (spriteStateKeyPair.Value.Active != isActive)
                {
                    spriteStateKeyPair.Value.Active = isActive;
                    if (isActive)
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
            return new SpriteInfo { RenderSprite = new RenderSprite { Source = spriteComponent } };
        }

        protected override bool IsAssociatedDataValid(Entity entity, SpriteComponent spriteComponent, SpriteInfo associatedData)
        {
            return associatedData.RenderSprite.Source == spriteComponent;
        }

        public class SpriteInfo
        {
            public bool Active;
            public RenderSprite RenderSprite;
        }
    }
}
