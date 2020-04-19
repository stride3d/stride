// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Mathematics;
using Stride.Animations;
using Stride.Engine;
using Stride.Rendering.Colors;
using Stride.Rendering.Lights;

namespace AnimatedModel
{
    public class AnimationScript : StartupScript
    {
        public override void Start()
        {
            // Create an AnimationClip. Make sure to properly set it's duration.
            var animationClip = new AnimationClip { Duration = TimeSpan.FromSeconds(1) };

            // Add some curves, specifying the path to the properties to animate.
            // - Components can be index using a special syntax to their key.
            // - Properties can be qualified with a type name in parenthesis
            // - If a type is not serializable, it's fully qualified name must be used
            var colorLightBaseName = typeof(ColorLightBase).AssemblyQualifiedName;
            var colorRgbProviderName = typeof(ColorRgbProvider).AssemblyQualifiedName;

            animationClip.AddCurve("[TransformComponent.Key].Rotation", CreateLightRotationCurve());
            animationClip.AddCurve(string.Format("[LightComponent.Key].Type.({0})Color.({1})Value", colorLightBaseName, colorRgbProviderName), CreateLightColorCurve());

            // Optional: Pack all animation channels into an optimized interleaved format
            animationClip.Optimize();

            // Add an AnimationComponent to the current entity and register our custom clip
            const string animationName = "MyCustomAnimation";
            var animationComponent = Entity.GetOrCreate<AnimationComponent>();
            animationComponent.Animations.Add(animationName, animationClip);

            // Start playing the animation right away and keep repeating it
            var playingAnimation = animationComponent.Play(animationName);
            playingAnimation.RepeatMode = AnimationRepeatMode.LoopInfinite;
            playingAnimation.TimeFactor = 0.1f; // slow down
            playingAnimation.CurrentTime = TimeSpan.FromSeconds(0.6f); // start at different time
        }

        private AnimationCurve CreateLightColorCurve()
        {
            return new AnimationCurve<Vector3>
            {
                InterpolationType = AnimationCurveInterpolationType.Cubic,
                KeyFrames =
                {
                    CreateKeyFrame(0.00f, new Vector3(1, 0.45f, 0 * 0.7f)), // Dawn
                    CreateKeyFrame(0.05f, new Vector3(1, 0.15f, 0) * 0.3f),

                    CreateKeyFrame(0.10f, new Vector3(0)),
                    CreateKeyFrame(0.25f, new Vector3(0)), // Midnight
                    CreateKeyFrame(0.40f, new Vector3(0)),

                    CreateKeyFrame(0.45f, new Vector3(1, 0.15f, 0) * 0.3f),
                    CreateKeyFrame(0.50f, new Vector3(1, 0.45f, 0) * 0.7f), // Dusk
                    CreateKeyFrame(0.55f, new Vector3(1, 0.65f, 0)),

                    CreateKeyFrame(0.60f, new Vector3(1, 1, 1)),
                    CreateKeyFrame(0.75f, new Vector3(1, 1, 1)), // Noon
                    CreateKeyFrame(0.90f, new Vector3(1, 1, 1)),

                    CreateKeyFrame(0.95f, new Vector3(1, 0.65f, 0)),
                    CreateKeyFrame(1.00f, new Vector3(1, 0.35f, 0)), // Dawn
                }
            };
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
