using System;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Animations;
using Stride.Engine;

namespace ##Namespace##
{
    public class ##Scriptname## : SyncScript, IBlendTreeBuilder
    {
        [Display("Animation Component")]
        public AnimationComponent AnimationComponent;

        [Display("Animation 1")]
        public AnimationClip Animation1;

        [Display("Animation 2")]
        public AnimationClip Animation2;

        [DataMemberRange(0, 1, 0.01, 0.1, 3)]
        [Display("Blend Lerp")]
        public float BlendLerp = 0.5f;

        [Display("Time Scale")]
        public double TimeFactor = 1;

        private AnimationClipEvaluator anim1Evaluator;
        private AnimationClipEvaluator anim2Evaluator;
        private double currentTime = 0;

        public override void Start()
        {
            base.Start();

            if (AnimationComponent == null)
                throw new InvalidOperationException("The animation component is not set");

            if (Animation1 == null)
                throw new InvalidOperationException("Animation 1 is not set");

            if (Animation2 == null)
                throw new InvalidOperationException("Animation 2 is not set");

            AnimationComponent.BlendTreeBuilder = this;

            anim1Evaluator = AnimationComponent.Blender.CreateEvaluator(Animation1);
            anim2Evaluator = AnimationComponent.Blender.CreateEvaluator(Animation2);
        }

        public override void Cancel()
        {
            AnimationComponent.Blender.ReleaseEvaluator(anim1Evaluator);
            AnimationComponent.Blender.ReleaseEvaluator(anim2Evaluator);
        }

        public override void Update()
        {
            // Use DrawTime rather than UpdateTime
            var time = Game.DrawTime;

            // This update function will account for animation with different durations, keeping a current time relative to the blended maximum duration
            long blendedMaxDuration = 0;
            blendedMaxDuration = (long)MathUtil.Lerp(Animation1.Duration.Ticks, Animation1.Duration.Ticks, BlendLerp);

            var currentTicks = TimeSpan.FromTicks((long)(currentTime * blendedMaxDuration));

            currentTicks = blendedMaxDuration == 0 ? TimeSpan.Zero : TimeSpan.FromTicks((currentTicks.Ticks + (long)(time.Elapsed.Ticks * TimeFactor)) % blendedMaxDuration);

            currentTime = ((double) currentTicks.Ticks/(double) blendedMaxDuration);
        }

        public void BuildBlendTree(FastList<AnimationOperation> blendStack)
        {
            // Note! The tree has to be flattened and given as a stack!
            blendStack.Add(AnimationOperation.NewPush(anim1Evaluator, TimeSpan.FromTicks((long)(currentTime * Animation1.Duration.Ticks))));
            blendStack.Add(AnimationOperation.NewPush(anim2Evaluator, TimeSpan.FromTicks((long)(currentTime * Animation2.Duration.Ticks))));
            blendStack.Add(AnimationOperation.NewBlend(CoreAnimationOperation.Blend, BlendLerp));
        }
    }
}
