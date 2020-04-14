// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single class
using System;
using Xenko.Core;
using Xenko.Core.Collections;
using Xenko.Core.Mathematics;
using Xenko.Updater;

namespace Xenko.Animations
{
    public abstract class AnimationCurveEvaluatorOptimizedGroup : AnimationCurveEvaluatorGroup
    {
        public abstract void Initialize(AnimationData animationData);
        public abstract void SetChannelOffset(string name, int offset);

        public static AnimationCurveEvaluatorOptimizedGroup Create<T>()
        {
            // Those types require interpolators
            // TODO: Simple enough for now, but at some point we might want a mechanism to register them externally?
            if (typeof(T) == typeof(float))
                return new AnimationCurveEvaluatorOptimizedFloatGroup();

            // TODO: Reintroduces explicit int path for now, since generic path does not work on iOS
            if (typeof(T) == typeof(int))
                return new AnimationCurveEvaluatorOptimizedIntGroup();

            if (typeof(T) == typeof(Quaternion))
                return new AnimationCurveEvaluatorOptimizedQuaternionGroup();

            if (typeof(T) == typeof(Vector3))
                return new AnimationCurveEvaluatorOptimizedVector3Group();

            if (typeof(T) == typeof(Vector4))
                return new AnimationCurveEvaluatorOptimizedVector4Group();

            // Blittable
            if (BlittableHelper.IsBlittable(typeof(T)))
                return new AnimationCurveEvaluatorOptimizedBlittableGroup<T>();

            // Objects
            return new AnimationCurveEvaluatorOptimizedObjectGroup<T>();
        }
    }

    public abstract class AnimationCurveEvaluatorOptimizedGroup<T> : AnimationCurveEvaluatorOptimizedGroup
    {
        private int animationSortedIndex;
        protected AnimationData<T> animationData;
        private CompressedTimeSpan currentTime;
        protected FastListStruct<Channel> channels = new FastListStruct<Channel>(8);

        public override Type ElementType => typeof(T);

        public override void Initialize(AnimationData animationData)
        {
            Initialize((AnimationData<T>)animationData);
        }

        public void Initialize(AnimationData<T> animationData)
        {
            this.animationData = animationData;

            foreach (var channel in animationData.AnimationInitialValues)
            {
                channels.Add(new Channel { InterpolationType = channel.InterpolationType, Offset = -1 });
            }

            // Setting infinite time means next time a rewind will be performed and initial values will be populated properly
            currentTime = CompressedTimeSpan.MaxValue;
        }

        public override void Cleanup()
        {
            animationData = null;
            channels.Clear();
        }

        public override void SetChannelOffset(string name, int offset)
        {
            var targetKeys = animationData.TargetKeys;
            for (int i = 0; i < targetKeys.Length; ++i)
            {
                if (targetKeys[i] == name)
                {
                    var channel = channels.Items[i];
                    channel.Offset = offset;
                    channels.Items[i] = channel;
                    break;
                }
            }
        }

        protected void SetTime(CompressedTimeSpan timeSpan)
        {
            // TODO: Add jump frames to do faster seeking.
            // If user seek back, need to start from beginning
            if (timeSpan < currentTime)
            {
                // Always start from beginning after a reset
                animationSortedIndex = 0;
                for (int channelIndex = 0; channelIndex < animationData.AnimationInitialValues.Length; ++channelIndex)
                {
                    InitializeAnimation(ref channels.Items[channelIndex], ref animationData.AnimationInitialValues[channelIndex]);
                }
            }

            currentTime = timeSpan;
            var animationSortedValueCount = animationData.AnimationSortedValueCount;
            var animationSortedValues = animationData.AnimationSortedValues;

            if (animationSortedValueCount == 0)
                return;

            // Advance until requested time is reached
            while (animationSortedIndex < animationSortedValueCount
                    && animationSortedValues[animationSortedIndex / AnimationData.AnimationSortedValueBlock][animationSortedIndex % AnimationData.AnimationSortedValueBlock].RequiredTime <= currentTime)
            {
                //int channelIndex = animationSortedValues[animationSortedIndex / animationSortedValueBlock][animationSortedIndex % animationSortedValueBlock].ChannelIndex;
                UpdateAnimation(ref animationSortedValues[animationSortedIndex / AnimationData.AnimationSortedValueBlock][animationSortedIndex % AnimationData.AnimationSortedValueBlock]);
                animationSortedIndex++;
            }

            currentTime = timeSpan;
        }

        private static void InitializeAnimation(ref Channel animationChannel, ref AnimationInitialValues<T> animationValue)
        {
            animationChannel.ValuePrev = animationValue.Value1;
            animationChannel.ValueStart = animationValue.Value1;
            animationChannel.ValueEnd = animationValue.Value1;
            animationChannel.ValueNext = animationValue.Value2;
        }

        private void UpdateAnimation(ref AnimationKeyValuePair<T> animationValue)
        {
            UpdateAnimation(ref channels.Items[animationValue.ChannelIndex], ref animationValue.Value);
        }

        private static void UpdateAnimation(ref Channel animationChannel, ref KeyFrameData<T> animationValue)
        {
            animationChannel.ValuePrev = animationChannel.ValueStart;
            animationChannel.ValueStart = animationChannel.ValueEnd;
            animationChannel.ValueEnd = animationChannel.ValueNext;
            animationChannel.ValueNext = animationValue;
        }

        protected struct Channel
        {
            public int Offset;
            public AnimationCurveInterpolationType InterpolationType;
            public KeyFrameData<T> ValuePrev;
            public KeyFrameData<T> ValueStart;
            public KeyFrameData<T> ValueEnd;
            public KeyFrameData<T> ValueNext;
        }
    }

    public abstract class AnimationCurveEvaluatorOptimizedBlittableGroupBase<T> : AnimationCurveEvaluatorOptimizedGroup<T>
    {
        public override void Evaluate(CompressedTimeSpan newTime, IntPtr data, UpdateObjectData[] objects)
        {
            if (animationData == null)
                return;

            SetTime(newTime);

            var channelCount = channels.Count;
            var channelItems = channels.Items;

            for (int i = 0; i < channelCount; ++i)
            {
                ProcessChannel(ref channelItems[i], newTime, data);
            }
        }

        protected void ProcessChannel(ref Channel channel, CompressedTimeSpan currentTime, IntPtr data)
        {
            if (channel.Offset == -1)
                return;

            var startTime = channel.ValueStart.Time;

            // TODO: Should we really do that?
            // Sampling before start (should not really happen because we add a keyframe at TimeSpan.Zero, but let's keep it in case it changes later.
            if (currentTime <= startTime)
            {
                Utilities.UnsafeWrite(data + channel.Offset, ref channel.ValueStart.Value);
                return;
            }

            var endTime = channel.ValueEnd.Time;

            // TODO: Should we really do that?
            // Sampling after end
            if (currentTime >= endTime)
            {
                Utilities.UnsafeWrite(data + channel.Offset, ref channel.ValueEnd.Value);
                return;
            }

            float factor = (float)(currentTime.Ticks - startTime.Ticks) / (float)(endTime.Ticks - startTime.Ticks);

            ProcessChannel(ref channel, currentTime, data, factor);
        }

        protected abstract void ProcessChannel(ref Channel channel, CompressedTimeSpan currentTime, IntPtr data, float factor);
    }

    public class AnimationCurveEvaluatorOptimizedObjectGroup<T> : AnimationCurveEvaluatorOptimizedGroup<T>
    {
        public override void Evaluate(CompressedTimeSpan newTime, IntPtr data, UpdateObjectData[] objects)
        {
            if (animationData == null)
                return;

            SetTime(newTime);

            var channelCount = channels.Count;
            var channelItems = channels.Items;

            for (int i = 0; i < channelCount; ++i)
            {
                ProcessChannel(ref channelItems[i], newTime, objects);
            }
        }

        protected void ProcessChannel(ref Channel channel, CompressedTimeSpan currentTime, UpdateObjectData[] objects)
        {
            if (channel.Offset == -1)
                return;

            var startTime = channel.ValueStart.Time;

            // TODO: Should we really do that?
            // Sampling before start (should not really happen because we add a keyframe at TimeSpan.Zero, but let's keep it in case it changes later.
            if (currentTime <= startTime)
            {
                objects[channel.Offset].Value = channel.ValueStart.Value;
                return;
            }

            // (This including sampling after end)
            objects[channel.Offset].Value = channel.ValueStart.Value;
        }
    }
}
