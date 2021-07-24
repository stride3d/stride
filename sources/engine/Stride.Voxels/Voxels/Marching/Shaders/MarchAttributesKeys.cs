﻿// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Sean Boettger <sean@whypenguins.com>
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Shaders;

namespace Stride.Rendering.Voxels
{
    public partial class MarchAttributesKeys
    {
        public static readonly PermutationParameterKey<ShaderSourceCollection> AttributeSamplers = ParameterKeys.NewPermutation<ShaderSourceCollection>();
    }
}
