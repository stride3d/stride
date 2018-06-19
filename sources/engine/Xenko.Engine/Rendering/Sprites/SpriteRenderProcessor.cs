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
                    var transform = renderSprite.TransformComponent;

                    // TODO GRAPHICS REFACTOR: Proper bounding box. Reuse calculations in sprite batch.
                    // For now we only set a center for sorting, but no extent (which disable culling)
                    renderSprite.BoundingBox = new BoundingBoxExt { Center = transform.WorldMatrix.TranslationVector };
                    renderSprite.RenderGroup = renderSprite.SpriteComponent.RenderGroup;

                    // update the sprite bounding box
                    Vector3 halfBoxSize;
                    var halfSpriteSize = currentSprite?.Size / 2 ?? Vector2.Zero;
                    var worldMatrix = renderSprite.TransformComponent.WorldMatrix;
                    var boxOffset = worldMatrix.TranslationVector;
                    if (renderSprite.SpriteComponent.SpriteType == SpriteType.Billboard)
                    {
                        // Make a gross estimation here as we don't have access to the camera view matrix
                        // TODO: move this code or grant camera view matrix access to this processor
                        var maxScale = Math.Max(worldMatrix.Row1.Length(), Math.Max(worldMatrix.Row2.Length(), worldMatrix.Row3.Length()));
                        halfBoxSize = maxScale * halfSpriteSize.Length() * Vector3.One;
                    }
                    else
                    {
                        halfBoxSize = new Vector3(
                            Math.Abs(worldMatrix.M11 * halfSpriteSize.X + worldMatrix.M21 * halfSpriteSize.Y),
                            Math.Abs(worldMatrix.M12 * halfSpriteSize.X + worldMatrix.M22 * halfSpriteSize.Y),
                            Math.Abs(worldMatrix.M13 * halfSpriteSize.X + worldMatrix.M23 * halfSpriteSize.Y));

                    }
                    renderSprite.BoundingBox = new BoundingBoxExt(boxOffset - halfBoxSize, boxOffset + halfBoxSize);
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
                }
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
