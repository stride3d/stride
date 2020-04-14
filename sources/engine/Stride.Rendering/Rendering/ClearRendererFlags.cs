// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Rendering
{
    /// <summary>
    /// Flags used to clear a render frame.
    /// </summary>
    [DataContract("ClearRenderFrameFlags")]
    public enum ClearRendererFlags
    {
        /// <summary>
        /// Clears both the Color and DepthStencil buffer.
        /// </summary>
        /// <userdoc>Clears both the Color and DepthStencil buffers</userdoc>
        [Display("Color and Depth")]
        [DataAlias("Color")] // The previous name was using `Color` only
        ColorAndDepth,

        /// <summary>
        /// Clears only the Color buffer.
        /// </summary>
        /// <userdoc>Clears only the Color buffer.</userdoc>
        [Display("Color Only")]
        ColorOnly,

        /// <summary>
        /// Clears only the depth.
        /// </summary>
        /// <userdoc>Clears only the DepthStencil buffer</userdoc>
        [Display("Depth Only")]
        DepthOnly,
    }
}
