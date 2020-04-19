// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.UI
{
    /// <summary>
    /// The different ways of scrolling in a <see cref="Controls.ScrollViewer"/>.
    /// </summary>
    public enum ScrollingMode
    {
        /// <summary>
        /// No scrolling is allowed.
        /// </summary>
        /// <userdoc>No scrolling is allowed.</userdoc>
        None,
        /// <summary>
        /// Only horizontal scrolling is allowed.
        /// </summary>
        /// <userdoc>Only horizontal scrolling is allowed.</userdoc>
        Horizontal,
        /// <summary>
        /// Only vertical scrolling is allowed.
        /// </summary>
        /// <userdoc>Only vertical scrolling is allowed.</userdoc>
        Vertical,
        /// <summary>
        /// Only in depth (back/front) scrolling is allowed.
        /// </summary>
        /// <userdoc>Only in depth (back/front) scrolling is allowed.</userdoc>
        InDepth,
        /// <summary>
        /// Both horizontal and vertical scrolling are allowed.
        /// </summary>
        /// <userdoc>Both horizontal and vertical scrolling are allowed.</userdoc>
        HorizontalVertical,
        /// <summary>
        /// Both vertical and in-depth scrolling are allowed.
        /// </summary>
        /// <userdoc>Both vertical and in-depth scrolling are allowed.</userdoc>
        VerticalInDepth,
        /// <summary>
        /// Both in-depth and horizontal scrolling are allowed.
        /// </summary>
        /// <userdoc>Both in-depth and horizontal scrolling are allowed.</userdoc>
        InDepthHorizontal,
    }
}
