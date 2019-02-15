// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
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
            return new RenderSpriteStudio { Source = component };
        }

        protected override bool IsAssociatedDataValid(Entity entity, SpriteStudioComponent component, RenderSpriteStudio associatedData)
        {
            return associatedData.Source == component;
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
                var spriteStudioComponent = spriteStateKeyPair.Key;
                var renderSpriteStudio = spriteStateKeyPair.Value;
                renderSpriteStudio.Enabled = spriteStudioComponent.Enabled && spriteStudioComponent.ValidState;

                if (!renderSpriteStudio.Enabled)
                    continue;

                renderSpriteStudio.WorldMatrix = spriteStudioComponent.Entity.Transform.WorldMatrix;
                renderSpriteStudio.Sheet = spriteStudioComponent.Sheet;
                renderSpriteStudio.SortedNodes.Clear();
                renderSpriteStudio.SortedNodes.AddRange(spriteStudioComponent.Nodes);
                renderSpriteStudio.SortedNodes.Sort(PriorityNodeComparer.Default);

                renderSpriteStudio.BoundingBox = new BoundingBoxExt { Center = renderSpriteStudio.WorldMatrix.TranslationVector };
                renderSpriteStudio.RenderGroup = spriteStudioComponent.RenderGroup;
            }
        }

        public VisibilityGroup VisibilityGroup { get; set; }

        private class PriorityNodeComparer : IComparer<SpriteStudioNodeState>
        {
            public static readonly PriorityNodeComparer Default = new PriorityNodeComparer();

            public int Compare(SpriteStudioNodeState x, SpriteStudioNodeState y)
            {
                return x.Priority.CompareTo(y.Priority);
            }
        }
    }
}
