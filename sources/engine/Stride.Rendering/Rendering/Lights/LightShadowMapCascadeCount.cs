// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;

namespace Xenko.Rendering.Lights
{
    /// <summary>
    /// Number of cascades used for a shadow map.
    /// </summary>
    [DataContract("LightShadowMapCascadeCount")]
    public enum LightShadowMapCascadeCount
    {
        /// <summary>
        /// A shadow map with one cascade.
        /// </summary>
        [Display("One Cascade")]
        OneCascade = 1,

        /// <summary>
        /// A shadow map with two cascades.
        /// </summary>
        [Display("Two Cascades")]
        TwoCascades = 2,

        /// <summary>
        /// A shadow map with four cascades.
        /// </summary>
        [Display("Four Cascades")]
        FourCascades = 4,
    }
}
