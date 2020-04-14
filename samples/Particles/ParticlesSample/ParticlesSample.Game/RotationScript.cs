// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Mathematics;
using Stride.Animations;
using Stride.Engine;

namespace ParticlesSample
{
    public class RotationScript : StartupScript
    {
        public float TimeFactor = 1f;

        public override void Start()
        {
            // Create an AnimationClip. Make sure to properly set it's duration.
            var animationClip = new AnimationClip { Duration = TimeSpan.FromSeconds(1) };


            animationClip.AddCurve("[TransformComponent.Key].Rotation", CreateLightRotationCurve());

            // Optional: Pack all animation channels into an optimized interleaved format
            animationClip.Optimize();

            // Add an AnimationComponent to the current entity and register our custom clip
            const string animationName = "MyCustomAnimation";
            var animationComponent = Entity.GetOrCreate<AnimationComponent>();
            animationComponent.Animations.Clear();
            animationComponent.Animations.Add(animationName, animationClip);

            // Start playing the animation right away and keep repeating it
            var playingAnimation = animationComponent.Play(animationName);
            playingAnimation.RepeatMode = AnimationRepeatMode.LoopInfinite;
            playingAnimation.TimeFactor = TimeFactor;
            playingAnimation.CurrentTime = TimeSpan.FromSeconds(0.6f); // start at different time
        }

        private AnimationCurve CreateLightRotationCurve()
        {
            return new AnimationCurve<Quaternion>
            {
                InterpolationType = AnimationCurveInterpolationType.Linear,
                KeyFrames =
                {
                    CreateKeyFrame(0.00f, Quaternion.RotationX(0)),
                    CreateKeyFrame(0.25f, Quaternion.RotationX(MathUtil.PiOverTwo)),
                    CreateKeyFrame(0.50f, Quaternion.RotationX(MathUtil.Pi)),
                    CreateKeyFrame(0.75f, Quaternion.RotationX(-MathUtil.PiOverTwo)),
                    CreateKeyFrame(1.00f, Quaternion.RotationX(MathUtil.TwoPi))
                }
            };
        }

        private static KeyFrameData<T> CreateKeyFrame<T>(float keyTime, T value)
        {
            return new KeyFrameData<T>((CompressedTimeSpan)TimeSpan.FromSeconds(keyTime), value);
        }
    }
}
