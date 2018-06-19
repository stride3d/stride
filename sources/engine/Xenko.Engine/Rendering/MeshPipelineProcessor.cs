// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Graphics;

namespace Xenko.Rendering
{
    /// <summary>
    /// Pipeline processor for <see cref="RenderMesh"/> with materials. It will set blend and depth-stencil state for transparent objects, and properly set culling according to material and negative scaling.
    /// </summary>
    public class MeshPipelineProcessor : PipelineProcessor
    {
        public RenderStage TransparentRenderStage { get; set; }

        public override void Process(RenderNodeReference renderNodeReference, ref RenderNode renderNode, RenderObject renderObject, PipelineStateDescription pipelineState)
        {
            var output = renderNode.RenderStage.Output;
            var isMultisample = output.MultisampleCount != MultisampleCount.None;
            var renderMesh = (RenderMesh)renderObject;

            // Make object in transparent stage use AlphaBlend and DepthRead
            if (renderNode.RenderStage == TransparentRenderStage)
            {
                pipelineState.BlendState = renderMesh.MaterialPass.BlendState ?? BlendStates.AlphaBlend;
                pipelineState.DepthStencilState = DepthStencilStates.DepthRead;
                if (isMultisample)
                    pipelineState.BlendState.AlphaToCoverageEnable = true;
            }

            var cullMode = pipelineState.RasterizerState.CullMode;

            // Apply material cull mode
            var cullModeOverride = renderMesh.MaterialInfo.CullMode;
            // No override, or already two-sided?
            if (cullModeOverride.HasValue && cullMode != CullMode.None)
            {
                if (cullModeOverride.Value == CullMode.None)
                {
                    // Override to two-sided
                    cullMode = CullMode.None;
                }
                else if (cullModeOverride.Value == cullMode)
                {
                    // No or double flipping
                    cullMode = CullMode.Back;
                }
                else
                {
                    // Single flipping
                    cullMode = CullMode.Front;
                }
            }

            // Flip faces when geometry is inverted
            if (renderMesh.IsScalingNegative)
            {
                if (cullMode == CullMode.Front)
                {
                    cullMode = CullMode.Back;
                }
                else if (cullMode == CullMode.Back)
                {
                    cullMode = CullMode.Front;
                }
            }

            pipelineState.RasterizerState.CullMode = cullMode;

            if (isMultisample)
            {
                pipelineState.RasterizerState.MultisampleCount = output.MultisampleCount;
                pipelineState.RasterizerState.MultisampleAntiAliasLine = true;
            }

            pipelineState.RasterizerState.ScissorTestEnable = output.ScissorTestEnable;
        }
    }
}
