// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Collections;
using Xenko.Core.Diagnostics;
using Xenko.Core.Mathematics;
using Xenko.Core.Storage;
using Xenko.Graphics;
using Xenko.Rendering.Images;
using Xenko.Rendering.Lights;
using Xenko.Rendering.Shadows;
using Xenko.Rendering.SubsurfaceScattering;
using Xenko.VirtualReality;
using Xenko.Rendering.Compositing;
using Xenko.Rendering.Voxels.Debug;

namespace Xenko.Rendering.Voxels
{
    /// <summary>
    /// Renders your game. It should use current <see cref="RenderContext.RenderView"/> and <see cref="CameraComponentRendererExtensions.GetCurrentCamera"/>.
    /// </summary>
    [Display("Forward & Voxel renderer")]
    public class ForwardRendererVoxels : ForwardRenderer
    {
        public IVoxelRenderer VoxelRenderer { get; set; }

        protected IShadowMapRenderer ShadowMapRenderer_notPrivate;

        public VoxelDebug VoxelVisualization { get; set; }

        protected override void InitializeCore()
        {
            ShadowMapRenderer_notPrivate = Context.RenderSystem.RenderFeatures.OfType<MeshRenderFeature>().FirstOrDefault()?.RenderFeatures.OfType<ForwardLightingRenderFeature>().FirstOrDefault()?.ShadowMapRenderer;
            base.InitializeCore();
        }
        protected override void CollectCore(RenderContext context)
        {
            VoxelRenderer?.Collect(Context, ShadowMapRenderer_notPrivate);
            base.CollectCore(context);
        }
        protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
        {
            using (drawContext.PushRenderTargetsAndRestore())
            {
                VoxelRenderer?.Draw(drawContext, ShadowMapRenderer_notPrivate);
            }

            base.DrawCore(context, drawContext);
        }

        protected override void DrawView(RenderContext context, RenderDrawContext drawContext, int eyeIndex, int eyeCount)
        {
            base.DrawView(context, drawContext, eyeIndex, eyeCount);

            // Voxel Debug if enabled
            if (VoxelVisualization != null)
            {
                VoxelVisualization.VoxelRenderer = VoxelRenderer;
                VoxelVisualization.Draw(drawContext, viewOutputTarget);
            }
        }
    }
}


