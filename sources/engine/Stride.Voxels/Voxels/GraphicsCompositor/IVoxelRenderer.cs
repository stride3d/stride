// Copyright (c) Stride contributors (https://stride3d.net) and Sean Boettger <sean@whypenguins.com>
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;

using Stride.Core;
using Stride.Core.Collections;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Lights;
using Stride.Rendering.Shadows;

namespace Stride.Rendering.Voxels
{
    public interface IVoxelRenderer
    {
        void Collect(RenderContext Context, IShadowMapRenderer ShadowMapRenderer);

        void Draw(RenderDrawContext drawContext, IShadowMapRenderer ShadowMapRenderer);
        Dictionary<VoxelVolumeComponent, ProcessedVoxelVolume> GetProcessedVolumes();
    }
}
