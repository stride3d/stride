// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.IO;

namespace Stride.Assets.Models
{
    [DataContract("AnimationAssetType")]
    public abstract class AnimationAssetType
    {
        [DataMemberIgnore]
        public abstract AnimationAssetTypeEnum Type { get; }
    }

    [Display("Animation Clip")]
    [DataContract("StandardAnimationAssetType")]
    public class StandardAnimationAssetType : AnimationAssetType
    {
        [DataMemberIgnore]
        public override AnimationAssetTypeEnum Type => AnimationAssetTypeEnum.AnimationClip;
    }

    [Display("Difference Clip")]
    [DataContract("DifferenceAnimationAssetType")]
    public class DifferenceAnimationAssetType : AnimationAssetType
    {
        [DataMemberIgnore]
        public override AnimationAssetTypeEnum Type => AnimationAssetTypeEnum.DifferenceClip;

        public DifferenceAnimationAssetType()
        {
            ClipDuration = new AnimationAssetDurationUnchecked()
            {
                Enabled = false,
                StartAnimationTimeBox = TimeSpan.Zero,
                EndAnimationTimeBox = AnimationAsset.LongestTimeSpan,
            };
        }

        /// <summary>
        /// Gets or sets the path to the base source animation model when using additive animation.
        /// </summary>
        /// <value>The path to the reference clip.</value>
        /// <userdoc>
        /// The reference clip (R) is what the difference clip (D) will be calculated against, effectively resulting in D = S - R ((S) being the source clip)
        /// </userdoc>
        [DataMember(30)]
        [SourceFileMember(false)]
        [Display("Reference")]
        public UFile BaseSource { get; set; } = new UFile("");

        /// <userdoc>Specifies how to use the base animation.</userdoc>
        [DataMember(40)]
        public AdditiveAnimationBaseMode Mode { get; set; } = AdditiveAnimationBaseMode.Animation;

        /// <summary>
        /// Enable clipping of the animation duration
        /// </summary>
        /// <userdoc>
        /// Enable clipping of the animation duration, constraining start and end frames.
        /// </userdoc>
        [DataMember(50)]
        [Display("Clip duration")]
        public AnimationAssetDurationUnchecked ClipDuration { get; set; }
    }

    /// <summary>
    /// Type which describes the nature of the animation clip we want to use.
    /// The terms are borrowed from the book Game Engine Architecture, Chapter 11.6.5 Additive Blending
    /// </summary>
    [DataContract]
    public enum AnimationAssetTypeEnum
    {
        /// <summary>
        /// Single source animation clip which animates the character.
        /// </summary>
        /// <userdoc>
        /// Single source animation clip which animates the character.
        /// </userdoc>
        [Display("Animation Clip")]
        AnimationClip = 1,

        /// <summary>
        /// Difference animation clip is computed as the difference against another animation. It is usually used for additive blending.
        /// </summary>
        /// <userdoc>
        /// Difference animation clip is computed as the difference against another animation. It is usually used for additive blending.
        /// </userdoc>
        [Display("Difference Clip")]
        DifferenceClip = 2,
    }
}
