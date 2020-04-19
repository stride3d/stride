// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Animations
{
    /// <summary>
    /// Describes the type of animation blend operation.
    /// </summary>
    [DataContract]
    public enum AnimationBlendOperation
    {
        /// <summary>
        /// Linear blend operation.
        /// </summary>
        [Display("Linear blend")]
        LinearBlend = 0,

        /// <summary>
        /// Add operation.
        /// </summary>
        [Display("Additive")]
        Add = 1,
    }

    /// <summary>
    /// Core animation operations support all operations exposed for blending as well as several required for the animation building itself
    /// </summary>
    public enum CoreAnimationOperation
    {
        Blend = 0,

        Add = 1,

        Subtract = 2,
    }
}
