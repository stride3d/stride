// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Animations
{
    /// <summary>
    /// Enumeration describing how an animation should be repeated.
    /// </summary>
    [DataContract]
    public enum AnimationRepeatMode
    {
        /// <summary>
        /// The animation plays once, and then stops.
        /// </summary>
        /// <userdoc>The animation plays once, and then stops.</userdoc>
        [Display("Play once")]
        PlayOnce,

        /// <summary>
        /// The animation loop for always.
        /// </summary>
        /// <userdoc>The animation loop for always.</userdoc>
        [Display("Loop")]
        LoopInfinite,

        /// <summary>
        /// The animation plays once, and then holds, being kept in the list.
        /// </summary>
        /// <userdoc>The animation plays once, and then holds, being kept in the list.</userdoc>
        [Display("Play once & hold")]
        PlayOnceHold,
    }
}
