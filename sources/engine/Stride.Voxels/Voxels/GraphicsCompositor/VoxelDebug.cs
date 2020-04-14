using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Graphics;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Collections;
using Xenko.Core.Extensions;
using Xenko.Core.Mathematics;
using Xenko.Shaders;
using Xenko.Rendering.Images;
using Xenko.Rendering.Voxels;
using Xenko.Rendering.Lights;
using Xenko.Rendering.Skyboxes;
using System.Linq;
using Xenko.Rendering.Compositing;

namespace Xenko.Rendering.Voxels.Debug
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
