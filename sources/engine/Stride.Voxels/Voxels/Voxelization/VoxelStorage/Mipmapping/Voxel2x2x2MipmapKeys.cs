// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Sean Boettger <sean@whypenguins.com>
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Shaders;

namespace Stride.Rendering.Voxels
{
    public static partial class Voxel2x2x2MipmapKeys
    {
        public static readonly PermutationParameterKey<ShaderSource> mipmapper = (PermutationParameterKey<ShaderSource>)ParameterKeys.Merge(ParameterKeys.NewPermutation<ShaderSource>(), typeof(Voxel2x2x2MipmapKeys), "Voxel2x2x2Mipmap.mipmapper");

        // __Initialize__ is defined by the source generator in another partial class.
    }
}
