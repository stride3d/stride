// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Mathematics;
using Xenko.Graphics;

namespace Xenko.Rendering.Shadows
{
    /// <summary>
    /// Keys used for shadow mapping.
    /// </summary>
    public static partial class ShadowMapKeys
    {
        /// <summary>
        /// Final shadow map texture.
        /// </summary>
        public static readonly ObjectParameterKey<Texture> ShadowMapTexture = ParameterKeys.NewObject<Texture>();
        
        /// <summary>
        /// Final shadow map texture size
        /// </summary>
        public static readonly ValueParameterKey<Vector2> TextureSize = ParameterKeys.NewValue<Vector2>();

        /// <summary>
        /// Final shadow map texture texel size.
        /// </summary>
        public static readonly ValueParameterKey<Vector2> TextureTexelSize = ParameterKeys.NewValue<Vector2>();
    }
}
