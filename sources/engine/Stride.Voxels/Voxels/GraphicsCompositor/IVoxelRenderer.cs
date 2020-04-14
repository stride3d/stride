using System;
using System.Collections.Generic;
using System.Text;

using Xenko.Core;
using Xenko.Core.Collections;
using Xenko.Core.Diagnostics;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Graphics;
using Xenko.Rendering;
using Xenko.Rendering.Lights;
using Xenko.Rendering.Shadows;

namespace Xenko.Rendering.Voxels
{
    public interface IVoxelRenderer
    {
        void Collect(RenderContext Context, IShadowMapRenderer ShadowMapRenderer);

        void Draw(RenderDrawContext drawContext, IShadowMapRenderer ShadowMapRenderer);
        Dictionary<VoxelVolumeComponent, ProcessedVoxelVolume> GetProcessedVolumes();
    }
}
