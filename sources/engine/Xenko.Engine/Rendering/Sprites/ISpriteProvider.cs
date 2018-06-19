// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Annotations;
using Xenko.Graphics;

namespace Xenko.Engine
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
