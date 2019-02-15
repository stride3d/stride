// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

using Xenko.Core;
using Xenko.Shaders;

namespace Xenko.Rendering
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
