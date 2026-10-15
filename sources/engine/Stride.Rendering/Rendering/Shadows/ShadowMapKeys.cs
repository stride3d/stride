// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Rendering.Shadows
{
    /// <summary>
    /// Keys used for shadow mapping.
    /// </summary>
    public static partial class ShadowMapKeys
    {
        /// <summary>
        /// Final shadow map texture.
        /// </summary>
        public static readonly ObjectParameterKey<Texture> ShadowMapTexture = (ObjectParameterKey<Texture>)ParameterKeys.Merge(ParameterKeys.NewObject<Texture>(), typeof(ShadowMapKeys), "ShadowMap.ShadowMapTexture");
        
        /// <summary>
        /// Final shadow map texture size
        /// </summary>
        public static readonly ValueParameterKey<Vector2> TextureSize = (ValueParameterKey<Vector2>)ParameterKeys.Merge(ParameterKeys.NewValue<Vector2>(), typeof(ShadowMapKeys), "ShadowMap.TextureSize");

        /// <summary>
        /// Final shadow map texture texel size.
        /// </summary>
        public static readonly ValueParameterKey<Vector2> TextureTexelSize = (ValueParameterKey<Vector2>)ParameterKeys.Merge(ParameterKeys.NewValue<Vector2>(), typeof(ShadowMapKeys), "ShadowMap.TextureTexelSize");

        [ModuleInitializer]
        internal static void __Initialize__()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(ShadowMapKeys).TypeHandle);
        }
    }
}
