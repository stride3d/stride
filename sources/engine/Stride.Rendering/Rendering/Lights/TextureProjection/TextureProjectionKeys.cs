// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Rendering.Lights
{
    /// <summary>
    /// Keys used for texture projection by lights.
    /// </summary>
    public static class TextureProjectionKeys
    {
        /// <summary>
        /// Key for the texture to project.
        /// </summary>
        public static readonly ObjectParameterKey<Texture> ProjectionTexture = ParameterKeys.NewObject<Texture>();

        /// <summary>
        /// Key for the texture to project.
        /// </summary>
        public static readonly ValueParameterKey<Vector2> UVScale = ParameterKeys.NewValue<Vector2>();

        /// <summary>
        /// Key for the texture to project.
        /// </summary>
        public static readonly ValueParameterKey<Vector2> UVOffset = ParameterKeys.NewValue<Vector2>();
    }
}
