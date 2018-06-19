// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;

namespace Xenko.Rendering
{
    public struct RenderNodeFeatureReference
    {
        public readonly RootRenderFeature RootRenderFeature;
        public readonly RenderNodeReference RenderNode;
        public readonly RenderObject RenderObject;

        public RenderNodeFeatureReference(RootRenderFeature rootRenderFeature, RenderNodeReference renderNode, RenderObject renderObject)
        {
            RootRenderFeature = rootRenderFeature;
            RenderNode = renderNode;
            RenderObject = renderObject;
        }
    }

    /// <summary>
    /// Describes a specific <see cref="RenderView"/>, <see cref="RootRenderFeature"/> and <see cref="RenderStage"/> combination.
    /// </summary>
    public struct RenderViewFeatureStage
    {
        public RenderStage RenderStage;

        public int RenderNodeStart;
        public int RenderNodeEnd;

        public RenderViewFeatureStage(RenderStage renderStage, int renderNodeStart, int renderNodeEnd)
        {
            RenderStage = renderStage;
            RenderNodeStart = renderNodeStart;
            RenderNodeEnd = renderNodeEnd;
        }
    }
}
