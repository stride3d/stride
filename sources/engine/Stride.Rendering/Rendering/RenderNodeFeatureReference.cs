// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Rendering
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
}
