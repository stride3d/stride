// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Xenko.Core.Mathematics;
using Xenko.Rendering.Lights;

namespace Xenko.Rendering.Images
{
    public struct RenderLightShaft
    {
        public RenderLight Light;
        public IDirectLight Light2;
        public int SampleCount;
        public float DensityFactor;
        public IReadOnlyList<RenderLightShaftBoundingVolume> BoundingVolumes;
        public bool SeparateBoundingVolumes;
    }

    public struct RenderLightShaftBoundingVolume
    {
        public Matrix World;
        public Model Model;
    }
}
