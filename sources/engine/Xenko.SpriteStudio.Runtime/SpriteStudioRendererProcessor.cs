// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Rendering;

namespace Xenko.SpriteStudio.Runtime
{
    public class SpriteStudioRendererProcessor : EntityProcessor<SpriteStudioComponent, RenderSpriteStudio>, IEntityComponentRenderProcessor
    {
        public SpriteStudioRendererProcessor()
            : base(typeof(TransformComponent))
        {
            Order = 550;
        }

        protected override RenderSpriteStudio GenerateComponentData(Entity entity, SpriteStudioComponent component)
        {
            return new RenderSpriteStudio
            {
                SpriteStudioComponent = component,
                TransformComponent = entity.Transform
            };
        }

        protected override bool IsAssociatedDataValid(Entity entity, SpriteStudioComponent component, RenderSpriteStudio associatedData)
        {
            return
                component == associatedData.SpriteStudioComponent &&
                entity.Transform == associatedData.TransformComponent;
        }

        protected override void OnEntityComponentAdding(Entity entity, SpriteStudioComponent component, RenderSpriteStudio data)
        {
            VisibilityGroup.RenderObjects.Add(data);
        }

        protected override void OnEntityComponentRemoved(Entity entity, SpriteStudioComponent component, RenderSpriteStudio data)
        {
            VisibilityGroup.RenderObjects.Remove(data);
        }

        public override void Draw(RenderContext context)
        {
            foreach (var spriteStateKeyPair in ComponentDatas)
            {
                var renderSpriteStudio = spriteStateKeyPair.Value;
                renderSpriteStudio.Enabled = renderSpriteStudio.SpriteStudioComponent.Enabled;

                if (!renderSpriteStudio.Enabled || !renderSpriteStudio.SpriteStudioComponent.ValidState) continue;

                renderSpriteStudio.BoundingBox = new BoundingBoxExt { Center = renderSpriteStudio.TransformComponent.WorldMatrix.TranslationVector };
                renderSpriteStudio.RenderGroup = renderSpriteStudio.SpriteStudioComponent.RenderGroup;
            }
        }

        public VisibilityGroup VisibilityGroup { get; set; }
    }
}
