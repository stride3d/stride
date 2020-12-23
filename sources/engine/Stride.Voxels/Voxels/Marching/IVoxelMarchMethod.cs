// Copyright (c) Stride contributors (https://stride3d.net) and Sean Boettger <sean@whypenguins.com>
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;
using Stride.Shaders;

namespace Stride.Rendering.Voxels
{
    public interface IVoxelMarchMethod
    {
        ShaderSource GetMarchingShader(int attrID);
        void UpdateMarchingLayout(string compositionName);
        void ApplyMarchingParameters(ParameterCollection parameters);
    }
}
