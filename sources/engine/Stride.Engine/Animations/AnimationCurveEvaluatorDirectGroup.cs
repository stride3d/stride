// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single class
using System;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Updater;

namespace Stride.Animations
{
    public abstract class AnimationCurveEvaluatorDirectGroup : AnimationCurveEvaluatorGroup
    {
        public static AnimationCurveEvaluatorDirectGroup Create<T>()
        {
            // Those types require interpolators
            // TODO: Simple enough for now, but at some point we might want a mechanism to register them externally?
            if (typeof(T) == typeof(float))
                return new AnimationCurveEvaluatorDirectFloatGroup();

            if (typeof(T) == typeof(Quaternion))
                return new AnimationCurveEvaluatorDirectQuaternionGroup();

            if (typeof(T) == typeof(Vector3))
                return new AnimationCurveEvaluatorDirectVector3Group();

            if (typeof(T) == typeof(Vector4))
                return new AnimationCurveEvaluatorDirectVector4Group();

            // Blittable
            if (BlittableHelper.IsBlittable(typeof(T)))
                return new AnimationCurveEvaluatorDirectBlittableGroup<T>();

            // Objects
            return new AnimationCurveEvaluatorDirectObjectGroup<T>();
        }

        public abstract void AddChannel(AnimationCurve curve, int offset);
    }

    public abstract class AnimationCurveEvaluatorDirectGroup<T> : AnimationCurveEvaluatorDirectGroup
    {
        protected FastListStruct<Channel> channels = new FastListStruct<Channel>(8);

        public override Type ElementType => typeof(T);

        public void Initialize()
        {
        }

        public override void Cleanup()
        {
            channels.Clear();
        }

        public override void AddChannel(AnimationCurve curve, int offset)
        {
            channels.Add(new Channel { Offset = offset, Curve = (AnimationCurve<T>)curve, InterpolationType = curve.InterpolationType });
        }

        protected static void SetTime(ref Channel channel, CompressedTimeSpan newTime)
        {
            var currentTime = channel.CurrentTime;
            if (newTime == currentTime)
                return;

            var currentIndex = channel.CurrentIndex;
            var keyFrames = channel.Curve.KeyFrames;

            var keyFramesItems = keyFrames.Items;
            var keyFramesCount = keyFrames.Count;

            if (newTime > currentTime)
            {
                while (currentIndex + 1 < keyFramesCount - 1 && newTime >= keyFramesItems[currentIndex + 1].Time)
                {
                    ++currentIndex;
                }
            }
            else if (newTime <= keyFramesItems[0].Time)
            {
                // Special case: fast rewind to beginning of animation
                currentIndex = 0;
            }
            else // newTime < currentTime
            {
                while (currentIndex - 1 >= 0 && newTime < keyFramesItems[currentIndex].Time)
                {
                    --currentIndex;
                }
            }

            channel.CurrentIndex = currentIndex;
            channel.CurrentTime = newTime;
        }

        protected struct Channel
        {
            public int Offset;
            public AnimationCurveInterpolationType InterpolationType;
            public AnimationCurve<T> Curve;
            public int CurrentIndex;
            public CompressedTimeSpan CurrentTime;
        }
    }

    public abstract class AnimationCurveEvaluatorDirectBlittableGroupBase<T> : AnimationCurveEvaluatorDirectGroup<T>
    {
        public override void Evaluate(CompressedTimeSpan newTime, IntPtr data, UpdateObjectData[] objects)
        {
            var channelCount = channels.Count;
            var channelItems = channels.Items;

            for (int i = 0; i < channelCount; ++i)
            {
                ProcessChannel(ref channelItems[i], newTime, data);
            }
        }

        protected abstract void ProcessChannel(ref Channel channel, CompressedTimeSpan newTime, IntPtr location);
    }

    public class AnimationCurveEvaluatorDirectObjectGroup<T> : AnimationCurveEvaluatorDirectGroup<T>
    {
        public override void Evaluate(CompressedTimeSpan newTime, IntPtr data, UpdateObjectData[] objects)
        {
            var channelCount = channels.Count;
            var channelItems = channels.Items;

            for (int i = 0; i < channelCount; ++i)
            {
                ProcessChannel(ref channelItems[i], newTime, objects);
            }
        }

        private void ProcessChannel(ref Channel channel, CompressedTimeSpan newTime, UpdateObjectData[] objects)
        {
            SetTime(ref channel, newTime);

            var keyFrames = channel.Curve.KeyFrames;
            var currentIndex = channel.CurrentIndex;

            objects[channel.Offset].Value = keyFrames.Items[currentIndex].Value;
        }
    }
}
