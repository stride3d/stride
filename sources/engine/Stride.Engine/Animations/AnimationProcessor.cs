// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Xenko.Core.Collections;
using Xenko.Core.Threading;
using Xenko.Engine;
using Xenko.Rendering;

namespace Xenko.Animations
{
    public class AnimationProcessor : EntityProcessor<AnimationComponent, AnimationProcessor.AssociatedData>
    {
        private readonly ConcurrentPool<FastList<AnimationOperation>> animationOperationPool = new ConcurrentPool<FastList<AnimationOperation>>(() => new FastList<AnimationOperation>());

        public AnimationProcessor()
        {
            Order = -500;
        }

        protected override AssociatedData GenerateComponentData(Entity entity, AnimationComponent component)
        {
            return new AssociatedData
            {
                AnimationComponent = component,
            };
        }

        protected override bool IsAssociatedDataValid(Entity entity, AnimationComponent component, AssociatedData associatedData)
        {
            return component == associatedData.AnimationComponent;
        }

        protected override void OnEntityComponentAdding(Entity entity, AnimationComponent component, AssociatedData data)
        {
            data.AnimationUpdater = new AnimationUpdater();
        }

        protected override void OnEntityComponentRemoved(Entity entity, AnimationComponent component, AssociatedData data)
        {
            // Return AnimationClipEvaluators to pool
            foreach (var playingAnimation in data.AnimationComponent.PlayingAnimations)
            {
                var evaluator = playingAnimation.Evaluator;
                if (evaluator != null)
                {
                    data.AnimationComponent.Blender.ReleaseEvaluator(evaluator);
                    playingAnimation.Evaluator = null;
                }
            }

            // Return AnimationClipResult to pool
            if (data.AnimationClipResult != null)
                data.AnimationComponent.Blender.FreeIntermediateResult(data.AnimationClipResult);
        }

        public override void Draw(RenderContext context)
        {
            var time = context.Time;

            //foreach (var entity in ComponentDatas.Values)
            Dispatcher.ForEach(ComponentDatas, () => animationOperationPool.Acquire(), (entity, animationOperations) =>
            {
                var associatedData = entity.Value;

                var animationUpdater = associatedData.AnimationUpdater;
                var animationComponent = associatedData.AnimationComponent;

                if (animationComponent.BlendTreeBuilder != null)
                {
                    animationComponent.BlendTreeBuilder.BuildBlendTree(animationOperations);
                }
                else
                {
                    // Advance time for all playing animations with AutoPlay set to on
                    foreach (var playingAnimation in animationComponent.PlayingAnimations)
                    {
                        if (!playingAnimation.Enabled || playingAnimation.Clip == null)
                            continue;

                        switch (playingAnimation.RepeatMode)
                        {
                            case AnimationRepeatMode.PlayOnceHold:
                            case AnimationRepeatMode.PlayOnce:
                                playingAnimation.CurrentTime = TimeSpan.FromTicks(playingAnimation.CurrentTime.Ticks + (long)(time.Elapsed.Ticks * (double)playingAnimation.TimeFactor));
                                if (playingAnimation.CurrentTime > playingAnimation.Clip.Duration)
                                    playingAnimation.CurrentTime = playingAnimation.Clip.Duration;
                                else if (playingAnimation.CurrentTime < TimeSpan.Zero)
                                    playingAnimation.CurrentTime = TimeSpan.Zero;
                                break;
                            case AnimationRepeatMode.LoopInfinite:
                                playingAnimation.CurrentTime = playingAnimation.Clip.Duration == TimeSpan.Zero
                                    ? TimeSpan.Zero
                                    : TimeSpan.FromTicks((playingAnimation.CurrentTime.Ticks + playingAnimation.Clip.Duration.Ticks
                                        + (long)(time.Elapsed.Ticks * (double)playingAnimation.TimeFactor)) % playingAnimation.Clip.Duration.Ticks);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    // Regenerate animation operations
                    float totalWeight = 0.0f;

                    for (int index = 0; index < animationComponent.PlayingAnimations.Count; index++)
                    {
                        var playingAnimation = animationComponent.PlayingAnimations[index];
                        var animationWeight = playingAnimation.Weight;

                        // Skip animation with 0.0f weight
                        if (animationWeight == 0.0f || playingAnimation.Clip == null)
                            continue;

                        // Default behavior for linea blending (it will properly accumulate multiple blending with their cumulative weight)
                        totalWeight += animationWeight;
                        float currentBlend = animationWeight / totalWeight;

                        if (playingAnimation.BlendOperation == AnimationBlendOperation.Add)
                        {
                            // Additive or substractive blending will use the weight as is (and reset total weight with it)
                            currentBlend = animationWeight;
                            totalWeight = animationWeight;
                        }

                        // Create evaluator
                        var evaluator = playingAnimation.Evaluator;
                        if (evaluator == null)
                        {
                            evaluator = animationComponent.Blender.CreateEvaluator(playingAnimation.Clip);
                            playingAnimation.Evaluator = evaluator;
                        }

                        animationOperations.Add(CreatePushOperation(playingAnimation));

                        if (animationOperations.Count >= 2)
                            animationOperations.Add(AnimationOperation.NewBlend((CoreAnimationOperation)playingAnimation.BlendOperation, currentBlend));
                    }
                }

                if (animationOperations.Count > 0)
                {
                    // Animation blending
                    animationComponent.Blender.Compute(animationOperations, ref associatedData.AnimationClipResult);

                    // Update animation data if we have a model component
                    animationUpdater.Update(animationComponent.Entity, associatedData.AnimationClipResult);
                }

                if (animationComponent.BlendTreeBuilder == null)
                {
                    // Update weight animation
                    for (int index = 0; index < animationComponent.PlayingAnimations.Count; index++)
                    {
                        var playingAnimation = animationComponent.PlayingAnimations[index];
                        bool removeAnimation = false;
                        if (playingAnimation.CrossfadeRemainingTime > TimeSpan.Zero)
                        {
                            playingAnimation.Weight += (playingAnimation.WeightTarget - playingAnimation.Weight)
                                                       * ((float)time.Elapsed.Ticks / playingAnimation.CrossfadeRemainingTime.Ticks);
                            playingAnimation.CrossfadeRemainingTime -= time.Elapsed;
                            if (playingAnimation.CrossfadeRemainingTime <= TimeSpan.Zero)
                            {
                                playingAnimation.Weight = playingAnimation.WeightTarget;

                                // If weight target was 0, removes the animation
                                if (playingAnimation.Weight <= 0.0f)
                                    removeAnimation = true;
                            }
                        }

                        if (playingAnimation.RepeatMode == AnimationRepeatMode.PlayOnce)
                        {
                             if ((playingAnimation.TimeFactor > 0 && playingAnimation.CurrentTime == playingAnimation.Clip.Duration) ||
                                 (playingAnimation.TimeFactor < 0 && playingAnimation.CurrentTime == TimeSpan.Zero))
                            removeAnimation = true;
                        }

                        if (removeAnimation)
                        {
                            animationComponent.PlayingAnimations.RemoveAt(index--); // Will also release its evaluator
                        }
                    }
                }

                animationOperations.Clear();
            }, animationOperations => animationOperationPool.Release(animationOperations));
        }

        private AnimationOperation CreatePushOperation(PlayingAnimation playingAnimation)
        {
            return AnimationOperation.NewPush(playingAnimation.Evaluator, playingAnimation.CurrentTime);
        }

        public AnimationClipResult GetAnimationClipResult(AnimationComponent animationComponent)
        {
            if (!ComponentDatas.ContainsKey(animationComponent))
                return null;

            return ComponentDatas[animationComponent].AnimationClipResult;
        }

        public class AssociatedData
        {
            public AnimationUpdater AnimationUpdater;
            public AnimationComponent AnimationComponent;
            public AnimationClipResult AnimationClipResult;
        }
    }
}
