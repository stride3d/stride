// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using Xunit;
using Stride.Animations;

namespace Stride.Engine.Tests
{
    public class AnimationChannelTest
    {
        [Fact(Skip = "Need check")]
        public void TestFitting()
        {
            // Make a sinus between T = 0s to 10s at 60 FPS
            var animationChannel = new AnimationChannel();
            animationChannel.KeyFrames.Add(new KeyFrameData<float> { Time = CompressedTimeSpan.Zero, Value = 0.0f });
            animationChannel.KeyFrames.Add(new KeyFrameData<float> { Time = CompressedTimeSpan.FromSeconds(10.0), Value = 0.0f });

            var maxErrorThreshold = 0.05f;
            var timeStep = CompressedTimeSpan.FromSeconds(1.0f / 60.0f);
            Func<CompressedTimeSpan, float> curve = x =>
                {
                    if (x.Ticks == 196588)
                    {
                    }
                    return (float)Math.Sin(x.Ticks / (double)CompressedTimeSpan.FromSeconds(10.0).Ticks * Math.PI * 2.0);
                };
            animationChannel.Fitting(
                curve,
                CompressedTimeSpan.FromSeconds(1.0f / 60.0f),
                maxErrorThreshold);

            var evaluator = new AnimationChannel.Evaluator(animationChannel.KeyFrames);
            for (var time = CompressedTimeSpan.Zero; time < CompressedTimeSpan.FromSeconds(10.0); time += timeStep)
            {
                var diff = Math.Abs(curve(time) - evaluator.Evaluate(time));
                Assert.True(diff <= maxErrorThreshold);
            }
        }

        [Fact]
        public void TestDiscontinuity()
        {
            var animationChannel = new AnimationChannel();
            animationChannel.KeyFrames.Add(new KeyFrameData<float> { Time = CompressedTimeSpan.Zero, Value = 0.0f });
            animationChannel.KeyFrames.Add(new KeyFrameData<float> { Time = CompressedTimeSpan.FromSeconds(1.0), Value = 0.0f });
            animationChannel.KeyFrames.Add(new KeyFrameData<float> { Time = CompressedTimeSpan.FromSeconds(1.0), Value = 0.0f });
            animationChannel.KeyFrames.Add(new KeyFrameData<float> { Time = CompressedTimeSpan.FromSeconds(1.0), Value = 1.0f });
            animationChannel.KeyFrames.Add(new KeyFrameData<float> { Time = CompressedTimeSpan.FromSeconds(1.0), Value = 1.0f });
            animationChannel.KeyFrames.Add(new KeyFrameData<float> { Time = CompressedTimeSpan.FromSeconds(2.0), Value = 1.0f });
            animationChannel.KeyFrames.Add(new KeyFrameData<float> { Time = CompressedTimeSpan.FromSeconds(2.0), Value = 1.0f });
            animationChannel.KeyFrames.Add(new KeyFrameData<float> { Time = CompressedTimeSpan.FromSeconds(2.0), Value = 0.0f });
            animationChannel.KeyFrames.Add(new KeyFrameData<float> { Time = CompressedTimeSpan.FromSeconds(2.0), Value = 0.0f });

            var evaluator = new AnimationChannel.Evaluator(animationChannel.KeyFrames);
            Assert.Equal(0.0f, evaluator.Evaluate(CompressedTimeSpan.FromSeconds(0.0)));
            Assert.Equal(0.0f, evaluator.Evaluate(CompressedTimeSpan.FromSeconds(0.999999)));
            Assert.Equal(1.0f, evaluator.Evaluate(CompressedTimeSpan.FromSeconds(1.0)));
            Assert.Equal(1.0f, evaluator.Evaluate(CompressedTimeSpan.FromSeconds(1.000001)));
            Assert.Equal(1.0f, evaluator.Evaluate(CompressedTimeSpan.FromSeconds(1.999999)));
            Assert.Equal(0.0f, evaluator.Evaluate(CompressedTimeSpan.FromSeconds(2.0)));
            Assert.Equal(0.0f, evaluator.Evaluate(CompressedTimeSpan.FromSeconds(2.000001)));
            Assert.Equal(0.0f, evaluator.Evaluate(CompressedTimeSpan.FromSeconds(2.5)));
        }

        [Fact]
        public void TestAnimationClip()
        {
            var clip = new AnimationClip
            {
                Duration = TimeSpan.FromSeconds(2.0f),
                RepeatMode = AnimationRepeatMode.LoopInfinite
            };

            var testCurve = new AnimationCurve<float>();
            clip.AddCurve("posx[TestNode]", testCurve);
            testCurve.InterpolationType = AnimationCurveInterpolationType.Linear;

            var time = CompressedTimeSpan.FromSeconds(0.0f);
            var value = 0.0f;
            var frame0 = new KeyFrameData<float>(time, value);
            testCurve.KeyFrames.Add(frame0);

            time = CompressedTimeSpan.FromSeconds(1.0f);
            value = 1.0f;
            var frame1 = new KeyFrameData<float>(time, value);
            testCurve.KeyFrames.Add(frame1);

            clip.Optimize();

            var optimizedCurvesFloat = (AnimationData<float>)clip.OptimizedAnimationDatas.First();

            //we should have 3 frames at this point. the last one will be added by the optimization process...
            Assert.Equal(1, optimizedCurvesFloat.AnimationSortedValueCount);
            //And 2 initial frames            
            Assert.Equal(frame0, optimizedCurvesFloat.AnimationInitialValues[0].Value1);
            Assert.Equal(frame1, optimizedCurvesFloat.AnimationInitialValues[0].Value2);
            Assert.Single(optimizedCurvesFloat.AnimationSortedValues);
            Assert.Single(optimizedCurvesFloat.AnimationSortedValues[0]);
            Assert.Equal(frame1, optimizedCurvesFloat.AnimationSortedValues[0][0].Value);
        }
    }
}
