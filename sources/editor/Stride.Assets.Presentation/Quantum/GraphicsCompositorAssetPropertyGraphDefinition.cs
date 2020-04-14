// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Assets.Quantum;
using Stride.Core.Quantum;
using Stride.Assets.Rendering;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Presentation.Quantum
{
    [AssetPropertyGraphDefinition(typeof(GraphicsCompositorAsset))]
    public class GraphicsCompositorAssetPropertyGraphDefinition : AssetPropertyGraphDefinition
    {
        public override bool IsMemberTargetObjectReference(IMemberNode member, object value)
        {
            if (value is SceneCameraSlot)
            {
                return true;
            }
            if (value is RenderStage)
            {
                return true;
            }
            if (value is ISharedRenderer)
            {
                return true;
            }
            return base.IsMemberTargetObjectReference(member, value);
        }

        public override bool IsTargetItemObjectReference(IObjectNode collection, NodeIndex itemIndex, object value)
        {
            if (value is SceneCameraSlot)
            {
                return collection.Type != typeof(SceneCameraSlotCollection);
            }
            if (value is RenderStage)
            {
                return collection.Type != typeof(RenderStageCollection);
            }
            if (value is ISharedRenderer)
            {
                return collection.Type != typeof(SharedRendererCollection);
            }
            return base.IsTargetItemObjectReference(collection, itemIndex, value);
        }
    }
}
