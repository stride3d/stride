// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Graphics;
using Xenko.Rendering;

namespace Xenko.Assets.Presentation.SceneEditor
{
    /// <summary>
    /// A render feature that can highlight meshes.
    /// </summary>
    public class HighlightRenderFeature : SubRenderFeature
    {
        public static readonly Dictionary<Material, Color4> MaterialHighlightColors = new Dictionary<Material, Color4>();

        public static readonly Dictionary<Mesh, Color4> MeshHighlightColors = new Dictionary<Mesh, Color4>();

        public static readonly Dictionary<ModelComponent, Color4> ModelHighlightColors = new Dictionary<ModelComponent, Color4>();

        public static readonly HashSet<Material> MaterialsHighlightedForModel = new HashSet<Material>();

        private ConstantBufferOffsetReference color;

        private ObjectPropertyKey<Color4> renderModelObjectInfoKey;

        /// <inheritdoc/>
        protected override void InitializeCore()
        {
            renderModelObjectInfoKey = RootRenderFeature.RenderData.CreateObjectKey<Color4>();

            color = ((RootEffectRenderFeature)RootRenderFeature).CreateDrawCBufferOffsetSlot(HighlightShaderKeys.HighlightColor.Name);
        }

        public override void Extract()
        {
            var renderModelObjectInfo = RootRenderFeature.RenderData.GetData(renderModelObjectInfoKey);

            foreach (var objectNodeReference in RootRenderFeature.ObjectNodeReferences)
            {
                var objectNode = RootRenderFeature.GetObjectNode(objectNodeReference);
                var renderMesh = (RenderMesh)objectNode.RenderObject;

                Color4 highlightColor;

                var isHighlighted =
                    MaterialHighlightColors.TryGetValue(renderMesh.MaterialPass.Material, out highlightColor) ||
                    MeshHighlightColors.TryGetValue(renderMesh.Mesh, out highlightColor) ||
                    (MaterialsHighlightedForModel.Contains(renderMesh.MaterialPass.Material)
                     && renderMesh.Source is ModelComponent component
                     && ModelHighlightColors.TryGetValue(component, out highlightColor));

                renderModelObjectInfo[objectNodeReference] = highlightColor;
            }
        }

        /// <inheritdoc/>
        public override unsafe void Prepare(RenderDrawContext context)
        {
            var renderModelObjectInfoData = RootRenderFeature.RenderData.GetData(renderModelObjectInfoKey);

            foreach (var renderNode in ((RootEffectRenderFeature)RootRenderFeature).RenderNodes)
            {
                var perDrawLayout = renderNode.RenderEffect.Reflection?.PerDrawLayout;
                if (perDrawLayout == null)
                    continue;

                var colorOffset = perDrawLayout.GetConstantBufferOffset(this.color);
                if (colorOffset == -1)
                    continue;

                var renderModelObjectInfo = renderModelObjectInfoData[renderNode.RenderObject.ObjectNode];

                var mappedCB = renderNode.Resources.ConstantBuffer.Data;
                var perDraw = (Color4*)((byte*)mappedCB + colorOffset);
                *perDraw = renderModelObjectInfo;
            }
        }

        public override void ProcessPipelineState(RenderContext context, RenderNodeReference renderNodeReference, ref RenderNode renderNode, RenderObject renderObject, PipelineStateDescription pipelineState)
        {
            base.ProcessPipelineState(context, renderNodeReference, ref renderNode, renderObject, pipelineState);

            // Check if this is a highlight rendering
            var perDrawLayout = renderNode.RenderEffect.Reflection?.PerDrawLayout;
            if (perDrawLayout == null)
                return;

            var colorOffset = perDrawLayout.GetConstantBufferOffset(this.color);
            if (colorOffset == -1)
                return;

            // Display using alpha blending and without depth-buffer writing
            pipelineState.BlendState = BlendStates.AlphaBlend;
            pipelineState.DepthStencilState = DepthStencilStates.DepthRead;
        }
    }
}
