// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Assets.Textures
{
    /// <summary>
    /// Gives a hint to the texture compressor on the kind of textures to select the appropriate compression format depending
    /// on the HW Level and platform.
    /// </summary>
    [DataContract("TextureHint")]
    public enum TextureHint
    {
        /// <summary>
        /// The texture is using the full color.
        /// </summary>
        Color,

        /// <summary>
        /// The texture is a grayscale.
        /// </summary>
        Grayscale,

        /// <summary>
        /// The texture is a normal map.
        /// </summary>
        NormalMap
    }
}
