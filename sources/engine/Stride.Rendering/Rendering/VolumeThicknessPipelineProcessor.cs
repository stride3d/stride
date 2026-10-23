// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Core;
using Stride.Graphics;
using Stride.Rendering.Materials;

namespace Stride.Rendering
{
    /// <summary>
    /// Sets the pipeline states of see-through volumes: both faces and a sum in the thickness stage, no depth test for their back faces in the transparent stage.
    /// </summary>
    /// <remarks>Those passes test the opaque depth in their shader, so a face resting on the opaque scene is counted and drawn alike.</remarks>
    public class VolumeThicknessPipelineProcessor : PipelineProcessor
    {
        // Faces add their signed optical depth and their count; the depth targets keep the nearest and the farthest
        private static readonly BlendStateDescription Sum = CreateSum();

        private static BlendStateDescription CreateSum()
        {
            var blend = new BlendStateDescription(Blend.One, Blend.One) { IndependentBlendEnable = true };
            blend.RenderTargets[1] = blend.RenderTargets[0];
            blend.RenderTargets[2] = blend.RenderTargets[0];
            blend.RenderTargets[2].ColorBlendFunction = blend.RenderTargets[2].AlphaBlendFunction = BlendFunction.Min;
            blend.RenderTargets[3] = blend.RenderTargets[0];
            blend.RenderTargets[3].ColorBlendFunction = blend.RenderTargets[3].AlphaBlendFunction = BlendFunction.Max;
            return blend;
        }

        /// <summary>The stage the thickness of see-through volumes is drawn in.</summary>
        [DefaultValue(null)]
        public RenderStage VolumeThicknessRenderStage { get; set; }

        /// <summary>The stage see-through materials are drawn in.</summary>
        [DefaultValue(null)]
        public RenderStage TransparentRenderStage { get; set; }

        /// <inheritdoc/>
        public override void Process(RenderNodeReference renderNodeReference, ref RenderNode renderNode, RenderObject renderObject, PipelineStateDescription pipelineState)
        {
            if (renderNode.RenderStage == TransparentRenderStage)
            {
                // Back faces may stand in for the camera being inside even when hidden: the shader tests the depth itself
                var materialPass = ((RenderMesh)renderObject).MaterialPass;
                if (materialPass != null && materialPass.PassIndex == MaterialTransparencyBlendFeature.VolumeBackFacePass && materialPass.Parameters.Get(MaterialVolumeKeys.Absorption) > 0)
                    pipelineState.DepthStencilState = DepthStencilStates.None;
                return;
            }

            if (renderNode.RenderStage != VolumeThicknessRenderStage)
                return;

            // The shader tests against the opaque depth itself, and front and back faces add with opposite signs
            pipelineState.RasterizerState = RasterizerStates.CullNone;
            pipelineState.DepthStencilState = DepthStencilStates.None;
            pipelineState.BlendState = Sum;
        }
    }
}
