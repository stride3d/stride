// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Annotations;
using Stride.Graphics;

namespace Stride.Engine
{
    /// <summary>
    /// The base interface for all classes providing sprites.
    /// </summary>
    [InlineProperty]
    public interface ISpriteProvider
    {
        /// <summary>
        /// Gets the number of sprites available in the provider.
        /// </summary>
        int SpritesCount { get; }

        /// <summary>
        /// Get a sprite from the provider.
        /// </summary>
        /// <returns></returns>
        Sprite GetSprite();
    }
}
