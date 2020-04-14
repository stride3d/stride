// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;

namespace Xenko.Assets.Sprite
{
    /// <summary>
    /// The different types of the sprite sheets.
    /// </summary>
    public enum SpriteSheetType
    {
        /// <summary>
        /// A sprite sprite sheet designed for 2D sprites.
        /// </summary>
        /// <userdoc>A sprite sprite sheet designed for 2D sprites.</userdoc>
        [Display("Sprite sheet for 2D sprites")]
        Sprite2D,

        /// <summary>
        /// A sprite sheet designed for UI.
        /// </summary>
        /// <userdoc>A sprite sheet designed for UI.</userdoc>
        [Display("Sprite sheet for UI")]
        UI,
    }
}
