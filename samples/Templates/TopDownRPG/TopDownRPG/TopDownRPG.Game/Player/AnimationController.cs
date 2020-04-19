// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Animations;
using Stride.Engine;
using Stride.Engine.Events;

namespace TopDownRPG.Player
{
    public class AnimationController : SyncScript, IBlendTreeBuilder
    {
        [Display("Animation Component")]
        public AnimationComponent AnimationComponent { get; set; }

        [Display("Idle")]
        public AnimationClip AnimationIdle { get; set; }

        [Display("Walk")]
        public AnimationClip AnimationWalk { get; set; }

        [Display("Run")]
        public AnimationClip AnimationRun { get; set; }

        [Display("Punch")]
        public AnimationClip AnimationPunch { get; set; }

        [DataMemberRange(0, 1, 0.01, 0.1, 3)]
        [Display("Walk Threshold")]
        public float WalkThreshold { get; set; } = 0.25f;

        [Display("Time Scale")]
        public double TimeFactor { get; set; } = 1;

        private AnimationClipEvaluator animEvaluatorIdle;
        private AnimationClipEvaluator animEvaluatorWalk;
        private AnimationClipEvaluator animEvaluatorRun;
        private AnimationClipEvaluator animEvaluatorPunch;
        private double currentTime = 0;

        // Idle-Walk-Run lerp
        private AnimationClipEvaluator animEvaluatorWalkLerp1;
        private AnimationClipEvaluator animEvaluatorWalkLerp2;
        private AnimationClip animationClipWalkLerp1;
        private AnimationClip animationClipWalkLerp2;
        private float walkLerpFactor = 0.5f;

        // Internal state
        private readonly EventReceiver<float> runSpeedEvent = new EventReceiver<float>(PlayerController.RunSpeedEventKey);
        private AnimationState state = AnimationState.Walking;

        private readonly EventReceiver<bool> attackEvent = new EventReceiver<bool>(PlayerController.IsAttackingEventKey);

        float runSpeed;

        public override void Start()
        {
            base.Start();

            if (AnimationComponent == null)
                throw new InvalidOperationException("The animation component is not set");

            if (AnimationIdle == null)
                throw new InvalidOperationException("Idle animation is not set");

            if (AnimationWalk == null)
                throw new InvalidOperationException("Walking animation is not set");

            if (AnimationRun == null)
                throw new InvalidOperationException("Running animation is not set");

            if (AnimationPunch == null)
                throw new InvalidOperationException("Punching animation is not set");

            // By setting a custom blend tree builder we can override the default behavior of the animation system
            //  Instead, BuildBlendTree(FastList<AnimationOperation> blendStack) will be called each frame
            AnimationComponent.BlendTreeBuilder = this;

            animEvaluatorIdle = AnimationComponent.Blender.CreateEvaluator(AnimationIdle);
            animEvaluatorWalk = AnimationComponent.Blender.CreateEvaluator(AnimationWalk);
            animEvaluatorRun = AnimationComponent.Blender.CreateEvaluator(AnimationRun);
            animEvaluatorPunch = AnimationComponent.Blender.CreateEvaluator(AnimationPunch);

            // Initial walk lerp
            walkLerpFactor = 0;
            animEvaluatorWalkLerp1 = animEvaluatorIdle;
            animEvaluatorWalkLerp2 = animEvaluatorWalk;
            animationClipWalkLerp1 = AnimationIdle;
            animationClipWalkLerp2 = AnimationWalk;
        }

        public override void Cancel()
        {
            AnimationComponent.Blender.ReleaseEvaluator(animEvaluatorIdle);
            AnimationComponent.Blender.ReleaseEvaluator(animEvaluatorWalk);
            AnimationComponent.Blender.ReleaseEvaluator(animEvaluatorRun);
            AnimationComponent.Blender.ReleaseEvaluator(animEvaluatorPunch);
        }

        private void UpdateWalking()
        {
            if (runSpeed < WalkThreshold)
            {
                walkLerpFactor = runSpeed / WalkThreshold;
                walkLerpFactor = (float)Math.Sqrt(walkLerpFactor);  // Idle-Walk blend looks really werid, so skew the factor towards walking
                animEvaluatorWalkLerp1 = animEvaluatorIdle;
                animEvaluatorWalkLerp2 = animEvaluatorWalk;
                animationClipWalkLerp1 = AnimationIdle;
                animationClipWalkLerp2 = AnimationWalk;
            }
            else
            {
                walkLerpFactor = (runSpeed - WalkThreshold) / (1.0f - WalkThreshold);
                animEvaluatorWalkLerp1 = animEvaluatorWalk;
                animEvaluatorWalkLerp2 = animEvaluatorRun;
                animationClipWalkLerp1 = AnimationWalk;
                animationClipWalkLerp2 = AnimationRun;
            }

            // Use DrawTime rather than UpdateTime
            var time = Game.DrawTime;
            // This update function will account for animation with different durations, keeping a current time relative to the blended maximum duration
            long blendedMaxDuration = 0;
            blendedMaxDuration =
                (long)MathUtil.Lerp(animationClipWalkLerp1.Duration.Ticks, animationClipWalkLerp2.Duration.Ticks, walkLerpFactor);

            var currentTicks = TimeSpan.FromTicks((long)(currentTime * blendedMaxDuration));

            currentTicks = blendedMaxDuration == 0
                ? TimeSpan.Zero
                : TimeSpan.FromTicks((currentTicks.Ticks + (long)(time.Elapsed.Ticks * TimeFactor)) %
                                     blendedMaxDuration);

            currentTime = ((double)currentTicks.Ticks / (double)blendedMaxDuration);
        }

        private void UpdatePunching()
        {
            var speedFactor = 1;
            var currentTicks = TimeSpan.FromTicks((long)(currentTime * AnimationPunch.Duration.Ticks));
            var updatedTicks = currentTicks.Ticks + (long)(Game.DrawTime.Elapsed.Ticks * TimeFactor * speedFactor);

            if (updatedTicks < AnimationPunch.Duration.Ticks)
            {
                currentTicks = TimeSpan.FromTicks(updatedTicks);
                currentTime = ((double)currentTicks.Ticks / (double)AnimationPunch.Duration.Ticks);
            }
            else
            {
                state = AnimationState.Walking;
                currentTime = 0;
                UpdateWalking();
            }
        }

        public override void Update()
        {
            // State control
            runSpeedEvent.TryReceive(out runSpeed);

            bool isAttackingNewValue;
            if (attackEvent.TryReceive(out isAttackingNewValue) && isAttackingNewValue && state != AnimationState.Punching)
            {
                currentTime = 0;
                state = AnimationState.Punching;
            }

            switch (state)
            {
                case AnimationState.Walking:  UpdateWalking();  break;
                case AnimationState.Punching: UpdatePunching(); break;
            }
        }

        /// <summary>
        /// BuildBlendTree is called every frame from the animation system when the <see cref="AnimationComponent"/> needs to be evaluated
        /// It overrides the default behavior of the <see cref="AnimationComponent"/> by setting a custom blend tree
        /// </summary>
        /// <param name="blendStack">The stack of animation operations to be blended</param>
        public void BuildBlendTree(FastList<AnimationOperation> blendStack)
        {
            switch (state)
            {
                case AnimationState.Walking:
                    {
                        // Note! The tree is laid out as a stack and has to be flattened before returning it to the animation system!
                        blendStack.Add(AnimationOperation.NewPush(animEvaluatorWalkLerp1,
                            TimeSpan.FromTicks((long)(currentTime * animationClipWalkLerp1.Duration.Ticks))));
                        blendStack.Add(AnimationOperation.NewPush(animEvaluatorWalkLerp2,
                            TimeSpan.FromTicks((long)(currentTime * animationClipWalkLerp2.Duration.Ticks))));
                        blendStack.Add(AnimationOperation.NewBlend(CoreAnimationOperation.Blend, walkLerpFactor));
                    }
                    break;

                case AnimationState.Punching:
                    {
                        blendStack.Add(AnimationOperation.NewPush(animEvaluatorPunch,
                            TimeSpan.FromTicks((long)(currentTime * AnimationPunch.Duration.Ticks))));
                    }
                    break;
            }
        }

        enum AnimationState
        {
            Walking,
            Punching,
        }
    }
}
