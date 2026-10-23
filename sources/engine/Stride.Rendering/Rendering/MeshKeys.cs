// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Rendering
{
    /// <summary>
    /// Defines keys associated with mesh used for compiling assets.
    /// </summary>
    public class MeshKeys
    {
        /// <summary>
        /// When compiling effect with an EffectLibraryAsset (sdfxlib), set it to true to allow permutation based on the 
        /// parameters of all meshes.
        /// </summary>
        /// TODO: allow permutation for a specific mesh
        /// <userdoc>
        /// Use the mesh parameters to generate effects
        /// </userdoc>
        public static readonly ValueParameterKey<bool> UseParameters = (ValueParameterKey<bool>)ParameterKeys.Merge(ParameterKeys.NewValue<bool>(), typeof(MeshKeys), "Mesh.UseParameters");

        [ModuleInitializer]
        internal static void __Initialize__()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(MeshKeys).TypeHandle);
        }
    }
}
