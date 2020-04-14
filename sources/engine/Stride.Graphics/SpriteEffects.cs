// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;

namespace Xenko.Graphics
{
    /// <summary>
    /// Defines sprite mirroring options.
    /// </summary>
    /// <remarks>
    /// Description is taken from original XNA <a href='http://msdn.microsoft.com/en-us/library/microsoft.xna.framework.graphics.spriteeffects.aspx'>SpriteEffects</a> class.
    /// </remarks>
    [DataContract]
    public enum SpriteEffects
    {
        /// <summary>
        /// No rotations specified.
        /// </summary>
        None = 0,

        /// <summary>
        /// Rotate 180 degrees around the Y axis before rendering.
        /// </summary>
        FlipHorizontally = 1,

        /// <summary>
        /// Rotate 180 degrees around the X axis before rendering.
        /// </summary>
        FlipVertically = 3,

        /// <summary>
        /// Rotate 180 degrees around both the X and Y axis before rendering.
        /// </summary>
        FlipBoth = 2,
    }
}
