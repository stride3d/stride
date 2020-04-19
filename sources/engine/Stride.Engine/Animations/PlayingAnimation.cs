// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace Stride.Animations
{
    public class PlayingAnimation
    {
        // Used internally by animation system
        // TODO: Stored in AnimationProcessor?
        internal AnimationClipEvaluator Evaluator;
        internal TaskCompletionSource<bool> EndedTCS;

        internal PlayingAnimation(string name, AnimationClip clip) : this()
        {
            Name = name;
            Clip = clip;
            RepeatMode = Clip.RepeatMode;
        }

        internal PlayingAnimation()
        {
            Enabled = true;
            TimeFactor = 1.0f;
            Weight = 1.0f;
            BlendOperation = AnimationBlendOperation.LinearBlend;
            RepeatMode = AnimationRepeatMode.LoopInfinite;
        }

        /// <summary>
        /// Gets or sets a value indicating whether animation is playing.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the name of this playing animation (optional).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the animation clip to run
        /// </summary>
        public AnimationClip Clip { get; }

        /// <summary>
        /// Gets or sets the repeat mode.
        /// </summary>
        public AnimationRepeatMode RepeatMode { get; set; }

        /// <summary>
        /// Gets or sets the blend operation.
        /// </summary>
        public AnimationBlendOperation BlendOperation { get; set; }

        /// <summary>
        /// Gets or sets the current time.
        /// </summary>
        public TimeSpan CurrentTime { get; set; }

        /// <summary>
        /// Gets or sets the playback speed factor.
        /// </summary>
        public float TimeFactor { get; set; }

        /// <summary>
        /// Gets or sets the animation weight.
        /// </summary>
        public float Weight { get; set; }

        public float WeightTarget { get; set; }

        /// <summary>
        /// If the <see cref="CrossfadeRemainingTime"/> is positive the blend weight will shift towards the target weight, reaching it at CrossfadeRemainingTime == 0
        /// At that point if the blend weight reaches 0, the animation will be deleted from the list
        /// </summary>
        public TimeSpan CrossfadeRemainingTime { get; set; }
    }
}
