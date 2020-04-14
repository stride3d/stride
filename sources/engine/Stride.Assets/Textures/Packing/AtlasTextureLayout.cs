// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;

namespace Stride.Assets.Textures.Packing
{
    /// <summary>
    /// This specifies the full layout of an atlas texture.
    /// </summary>
    public class AtlasTextureLayout
    {
        /// <summary>
        /// Gets or sets a list of packed AtlasTextureElement
        /// </summary>
        public readonly List<AtlasTextureElement> Textures = new List<AtlasTextureElement>();

        /// <summary>
        /// Gets or sets Width of the texture atlas
        /// </summary>
        public int Width;

        /// <summary>
        /// Gets or sets Height of the texture atlas
        /// </summary>
        public int Height;
    }
}
