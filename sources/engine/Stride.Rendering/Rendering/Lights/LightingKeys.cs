// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

using Stride.Core;
using Stride.Shaders;

namespace Stride.Rendering
{
    /// <summary>
    /// Keys used for lighting.
    /// </summary>
    [DataContract]
    public partial class LightingKeys : ShaderMixinParameters
    {
        public static readonly PermutationParameterKey<ShaderSourceCollection> DirectLightGroups = ParameterKeys.NewPermutation((ShaderSourceCollection)null);

        public static readonly PermutationParameterKey<ShaderSourceCollection> EnvironmentLights = ParameterKeys.NewPermutation((ShaderSourceCollection)null);
    }
}
