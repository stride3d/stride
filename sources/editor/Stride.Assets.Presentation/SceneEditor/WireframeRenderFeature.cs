// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using Xenko.Core.Mathematics;
using Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Xenko.Graphics;
using Xenko.Rendering;

namespace Xenko.Assets.Presentation.SceneEditor
{
    /// <summary>
    /// Performs wireframe rendering.
    /// </summary>
    public class WireframeRenderFeature : SubRenderFeature
    {
        private StaticObjectPropertyKey<RenderEffect> renderEffectKey;

        private ConstantBufferOffsetReference perDrawData;

        private IEditorGameEntitySelectionService selectionService;

        private readonly Stopwatch clockSelection = new Stopwatch();

        // TODO: Make configurable (per object/view/...?)
        private struct PerDraw
        {
            public Color3 FrontColor;
            public float ColorBlend;
            public Color3 BackColor;
            public float AlphaBlend;
        }

        public void RegisterSelectionService(IEditorGameEntitySelectionService selectionService)
        {
            if (selectionService == null) throw new ArgumentNullException(nameof(selectionService));

            this.selectionService = selectionService;
            selectionService.SelectionUpdated += SelectionUpdated;
        }

        /// <inheritdoc/>
        protected override void InitializeCore()
        {
            perDrawData = ((RootEffectRenderFeature)RootRenderFeature).CreateDrawCBufferOffsetSlot(MaterialFrontBackBlendShaderKeys.ColorFront.Name);

            renderEffectKey = ((RootEffectRenderFeature)RootRenderFeature).RenderEffectKey;
        }

        /// <inheritdoc/>
        public override unsafe void Prepare(RenderDrawContext context)
        {
            var blendValue = (selectionService?.DisplaySelectionMask ?? false) ? 1.0f : MathUtil.Clamp((1.0f - (float)clockSelection.Elapsed.TotalSeconds) / 1.0f, 0, 1);
            var perDrawValue = new PerDraw
            {
                FrontColor = ((Color3)Color.FromBgra(0xFFFFDC51)).ToColorSpace(Context.GraphicsDevice.ColorSpace),
                BackColor = ((Color3)Color.FromBgra(0xFFFF8300)).ToColorSpace(Context.GraphicsDevice.ColorSpace),
                ColorBlend = 0.3f * blendValue,
                AlphaBlend = 0.1f * blendValue
            };

            foreach (var renderNode in ((RootEffectRenderFeature)RootRenderFeature).RenderNodes)
            {
                var perDrawLayout = renderNode.RenderEffect.Reflection?.PerDrawLayout;
                if (perDrawLayout == null)
                    continue;

                var perDrawDataOffset = perDrawLayout.GetConstantBufferOffset(this.perDrawData);
                if (perDrawDataOffset == -1)
                    continue;
                    
                var mappedCB = renderNode.Resources.ConstantBuffer.Data;
                var perDraw = (PerDraw*)((byte*)mappedCB + perDrawDataOffset);
                *perDraw = perDrawValue;
            }
        }

        public override void ProcessPipelineState(RenderContext context, RenderNodeReference renderNodeReference, ref RenderNode renderNode, RenderObject renderObject, PipelineStateDescription pipelineState)
        {
            base.ProcessPipelineState(context, renderNodeReference, ref renderNode, renderObject, pipelineState);

            // Check if this is a wireframe rendering
            var perDrawLayout = renderNode.RenderEffect.Reflection?.PerDrawLayout;
            if (perDrawLayout == null)
                return;

            var perDrawDataOffset = perDrawLayout.GetConstantBufferOffset(this.perDrawData);
            if (perDrawDataOffset == -1)
                return;

            // Display using wireframe and without depth-buffer
            pipelineState.BlendState = BlendStates.AlphaBlend;
            pipelineState.RasterizerState = RasterizerStates.Wireframe;
            pipelineState.DepthStencilState.DepthBufferEnable = false;
        }

        /// <inheritdoc />
        protected override void Destroy()
        {
            if (selectionService != null)
                selectionService.SelectionUpdated -= SelectionUpdated;
            base.Destroy();
        }

        private void SelectionUpdated(object sender, EntitySelectionEventArgs e)
        {
            clockSelection.Restart();
        }
    }
}
