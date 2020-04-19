// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Assets
{
    /// <summary>
    /// A rendering mode of Preview and Thumbnail for a game.
    /// </summary>
    [DataContract("RenderingMode")]
    public enum RenderingMode
    {
        /// <summary>
        /// The preview and thumbnail will use a low dynamic range settings when displaying assets.
        /// </summary>
        /// <userdoc>The preview and thumbnail will use a low dynamic range settings when displaying assets.</userdoc>
        [Display("Low Dynamic Range")]
        LDR,

        /// <summary>
        /// The preview and thumbnail will use a high dynamic range settings when displaying assets.
        /// </summary>
        /// <userdoc>The preview and thumbnail will use a high dynamic range settings when displaying assets.</userdoc>
        [Display("High Dynamic Range")]
        HDR,
    }
}
