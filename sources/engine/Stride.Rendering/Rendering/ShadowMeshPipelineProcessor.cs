// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.ComponentModel;
using Stride.Graphics;

namespace Stride.Rendering
{
    /// <summary>
    /// Pipline processor for <see cref="RenderMesh"/> that cast shadows, to properly disable culling and depth clip.
    /// </summary>
    public class ShadowMeshPipelineProcessor : PipelineProcessor
    {
        public RenderStage ShadowMapRenderStage { get; set; }

        [DefaultValue(false)]
        public bool DepthClipping { get; set; } = false;

        public override void Process(RenderNodeReference renderNodeReference, ref RenderNode renderNode, RenderObject renderObject, PipelineStateDescription pipelineState)
        {
            // Objects in the shadow map render stage disable culling and depth clip
            if (renderNode.RenderStage == ShadowMapRenderStage)
            {
                pipelineState.RasterizerState = new RasterizerStateDescription(CullMode.None) { DepthClipEnable = DepthClipping };
            }
        }
    }
}
