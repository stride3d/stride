// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core;

namespace Stride.Assets.Models
{
    /// <summary>
    /// Enable clipping of the animation duration
    /// </summary>
    /// <userdoc>
    /// Enable clipping of the animation duration, constraining start and end frames.
    /// </userdoc>
    [DataContract("AnimationAssetDuration")]
    [Display("Clip duration")]
    public struct AnimationAssetDuration
    {
        [DataMember(-5)]
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the start frame of the animation.
        /// </summary>
        /// <userdoc>
        /// The animation will start from this frame.
        /// </userdoc>
        [DataMember(2)]
        [Display("Start frame")]
        public TimeSpan StartAnimationTime { get; set; }

        /// <summary>
        /// Gets or sets the end frame of the animation.
        /// </summary>
        /// <userdoc>
        /// The animation will end on this frame.
        /// </userdoc>
        [DataMember(4)]
        [Display("End frame")]
        public TimeSpan EndAnimationTime { get; set; }
    }

    /// <summary>
    /// Enable clipping of the animation duration
    /// </summary>
    /// <userdoc>
    /// Enable clipping of the animation duration, constraining start and end frames.
    /// </userdoc>
    [DataContract("AnimationAssetDurationUnchecked")]
    [Display("Clip duration")]
    public struct AnimationAssetDurationUnchecked
    {
        [DataMember(-5)]
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the start frame of the animation.
        /// </summary>
        /// <userdoc>
        /// The animation will start from this frame.
        /// </userdoc>
        [DataMember(2)]
        [Display("Start frame")]
        public TimeSpan StartAnimationTimeBox { get; set; }

        /// <summary>
        /// Gets or sets the end frame of the animation.
        /// </summary>
        /// <userdoc>
        /// The animation will end on this frame.
        /// </userdoc>
        [DataMember(4)]
        [Display("End frame")]
        public TimeSpan EndAnimationTimeBox { get; set; }
    }
}
