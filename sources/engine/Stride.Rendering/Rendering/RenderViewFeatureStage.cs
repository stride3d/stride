// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Rendering
{
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
