// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Core;
using Stride.Graphics;

namespace Stride.Rendering
{
    /// <summary>
    /// Draws the volume thickness stage with both faces, without a depth buffer, and summing into its targets.
    /// </summary>
    public class VolumeThicknessPipelineProcessor : PipelineProcessor
    {
        // A plain sum: the alpha carries optical depth, negative on front faces, not a coverage
        private static readonly BlendStateDescription Sum = new(Blend.One, Blend.One);

        /// <summary>The stage the thickness of see-through volumes is drawn in.</summary>
        [DefaultValue(null)]
        public RenderStage VolumeThicknessRenderStage { get; set; }

        /// <inheritdoc/>
        public override void Process(RenderNodeReference renderNodeReference, ref RenderNode renderNode, RenderObject renderObject, PipelineStateDescription pipelineState)
        {
            if (renderNode.RenderStage != VolumeThicknessRenderStage)
                return;

            // The shader tests against the opaque depth itself, and front and back faces add with opposite signs
            pipelineState.RasterizerState = RasterizerStates.CullNone;
            pipelineState.DepthStencilState = DepthStencilStates.None;
            pipelineState.BlendState = Sum;
        }
    }
}
