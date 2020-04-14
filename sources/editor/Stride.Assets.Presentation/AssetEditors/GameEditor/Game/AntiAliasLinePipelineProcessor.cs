// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Graphics;
using Stride.Rendering;

namespace Stride.Assets.Presentation.AssetEditors.GameEditor.Game
{
    public class AntiAliasLinePipelineProcessor : PipelineProcessor
    {
        public RenderStage RenderStage { get; set; }

        public override void Process(RenderNodeReference renderNodeReference, ref RenderNode renderNode, RenderObject renderObject, PipelineStateDescription pipelineState)
        {
            if (renderNode.RenderStage == RenderStage)
            {
                pipelineState.BlendState = BlendStates.NonPremultiplied;
                pipelineState.DepthStencilState = DepthStencilStates.DepthRead;
                pipelineState.RasterizerState.MultisampleAntiAliasLine = true;
            }
        }
    }
}
