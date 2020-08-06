// Copyright (c) Stride contributors (https://stride3d.net) and Sean Boettger <sean@whypenguins.com>
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core;
using Stride.Rendering;
using Stride.Graphics;
using Stride.Shaders;
using Stride.Core.Mathematics;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.Rendering.Voxels
{
    public static partial class Voxel2x2x2MipmapKeys
    {
        public static readonly PermutationParameterKey<ShaderSource> mipmapper = ParameterKeys.NewPermutation<ShaderSource>();
    }
}
