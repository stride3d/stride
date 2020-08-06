// Copyright (c) Stride contributors (https://stride3d.net) and Sean Boettger <sean@whypenguins.com>
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;
using Stride.Graphics;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Collections;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Shaders;
using Stride.Rendering.Images;
using Stride.Rendering.Voxels;
using Stride.Rendering.Lights;
using Stride.Rendering.Skyboxes;
using System.Linq;
using Stride.Rendering.Compositing;

namespace Stride.Rendering.Voxels.Debug
{
    [DataContract("VoxelDebug")]
    public class VoxelDebug : ImageEffect
    {
        [DataMemberIgnore]
        public IVoxelRenderer VoxelRenderer;
        protected override void InitializeCore()
        {
            base.InitializeCore();
        }
        protected override void DrawCore(RenderDrawContext context)
        {
            if (!Initialized)
                Initialize(context.RenderContext);

            if (VoxelRenderer == null)
            {
                return;
            }

            Dictionary<VoxelVolumeComponent, ProcessedVoxelVolume> datas = VoxelRenderer.GetProcessedVolumes();
            if (datas == null)
            {
                return;
            }
            foreach (var datapairs in datas)
            {
                var data = datapairs.Value;

                if (!data.VisualizeVoxels || data.VoxelVisualization == null || data.VisualizationAttribute == null)
                    continue;

                ImageEffectShader shader = data.VoxelVisualization.GetShader(context, data.VisualizationAttribute);

                if (shader == null)
                    continue;

                shader.SetOutput(GetSafeOutput(0));

                shader.Draw(context);
            }
        }
        public void Draw(RenderDrawContext drawContext, Texture output)
        {
            SetOutput(output);
            DrawCore(drawContext);
        }
    }
}
