// Copyright (c) Stride contributors (https://stride3d.net) and Sean Boettger <sean@whypenguins.com>
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.ComponentModel;
using Stride.Graphics;

namespace Stride.Rendering.Voxels
{
    /// <summary>
    /// Pipline processor for <see cref="RenderMesh"/> that cast shadows, to properly disable culling and depth clip.
    /// </summary>
    public class VoxelPipelineProcessor : PipelineProcessor
    {
        public List<RenderStage> VoxelRenderStage { get; set; } = new List<RenderStage>();

        [DefaultValue(false)]
        public bool DepthClipping { get; set; } = false;

        public override void Process(RenderNodeReference renderNodeReference, ref RenderNode renderNode, RenderObject renderObject, PipelineStateDescription pipelineState)
        {
            // Disable culling and depth clip
            if (VoxelRenderStage.Contains(renderNode.RenderStage))
            {
                pipelineState.RasterizerState = new RasterizerStateDescription(CullMode.None) { DepthClipEnable = DepthClipping };
                pipelineState.DepthStencilState.DepthBufferEnable = false;
                pipelineState.DepthStencilState.DepthBufferWriteEnable = false;
                pipelineState.DepthStencilState.StencilEnable = false;
                pipelineState.DepthStencilState.StencilWriteMask = 0;
                pipelineState.DepthStencilState.StencilMask = 0;
                pipelineState.BlendState.RenderTarget0.BlendEnable = false;
                pipelineState.BlendState.IndependentBlendEnable = false;
            }
        }
    }
}
