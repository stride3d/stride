// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

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
        public static readonly PermutationParameterKey<ShaderSourceCollection> DirectLightGroups = (PermutationParameterKey<ShaderSourceCollection>)ParameterKeys.Merge(ParameterKeys.NewPermutation((ShaderSourceCollection)null), typeof(LightingKeys), "Lighting.DirectLightGroups");

        public static readonly PermutationParameterKey<ShaderSourceCollection> EnvironmentLights = (PermutationParameterKey<ShaderSourceCollection>)ParameterKeys.Merge(ParameterKeys.NewPermutation((ShaderSourceCollection)null), typeof(LightingKeys), "Lighting.EnvironmentLights");

        [ModuleInitializer]
        internal static void __Initialize__()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(LightingKeys).TypeHandle);
        }
    }
}
