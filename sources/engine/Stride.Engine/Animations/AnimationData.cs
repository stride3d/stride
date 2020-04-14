// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single type
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Xenko.Core;
using Xenko.Core.Collections;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization;
using Xenko.Updater;

namespace Xenko.Animations
{
    [DataContract(Inherited = true)]
    public abstract class AnimationData
    {
        public const int AnimationSortedValueBlock = 4096;

        public int AnimationSortedValueCount { get; set; }
        public string[] TargetKeys { get; set; }

        public abstract Type ElementType { get; }
        internal abstract AnimationCurveEvaluatorOptimizedGroup CreateEvaluator();
    }

    [DataSerializerGlobal(null, typeof(AnimationData<float>))]
    [DataSerializerGlobal(null, typeof(AnimationData<double>))]
    [DataSerializerGlobal(null, typeof(AnimationData<Vector2>))]
    [DataSerializerGlobal(null, typeof(AnimationData<Vector3>))]
    [DataSerializerGlobal(null, typeof(AnimationData<Vector4>))]
    [DataSerializerGlobal(null, typeof(AnimationData<int>))]
    [DataSerializerGlobal(null, typeof(AnimationData<uint>))]
    [DataSerializerGlobal(null, typeof(AnimationData<long>))]
    [DataSerializerGlobal(null, typeof(AnimationData<ulong>))]
    [DataSerializerGlobal(null, typeof(AnimationData<Int2>))]
    [DataSerializerGlobal(null, typeof(AnimationData<Int3>))]
    [DataSerializerGlobal(null, typeof(AnimationData<Int4>))]
    [DataSerializerGlobal(null, typeof(AnimationData<Quaternion>))]
    [DataSerializerGlobal(null, typeof(AnimationData<object>))]
    public class AnimationData<T> : AnimationData
    {
        public AnimationInitialValues<T>[] AnimationInitialValues { get; set; }
        public AnimationKeyValuePair<T>[][] AnimationSortedValues { get; set; }

        public TimeSpan Duration
        {
            get { return AnimationSortedValueCount == 0 ? TimeSpan.FromSeconds(1) : AnimationSortedValues[(AnimationSortedValueCount - 1) / AnimationSortedValueBlock][(AnimationSortedValueCount - 1) % AnimationSortedValueBlock].Value.Time; }
        }

        public override Type ElementType => typeof(T);

        public static AnimationData<T> FromAnimationChannels(IList<KeyValuePair<string, AnimationCurve<T>>> animationChannelsWithName)
        {
            var result = new AnimationData<T>();

            // Build target object and target properties lists
            var animationChannels = animationChannelsWithName.Select(x => x.Value).ToList();
            result.TargetKeys = animationChannelsWithName.Select(x => x.Key).ToArray();

            // Complexity _might_ be better by inserting directly in order instead of sorting later.
            var animationValues = new List<AnimationKeyValuePair<T>>[animationChannels.Count];
            result.AnimationInitialValues = new AnimationInitialValues<T>[animationChannels.Count];
            for (int channelIndex = 0; channelIndex < animationChannels.Count; ++channelIndex)
            {
                var channel = animationChannels[channelIndex];
                var animationChannelValues = animationValues[channelIndex] = new List<AnimationKeyValuePair<T>>();
                if (channel.KeyFrames.Count > 0)
                {
                    // Copy first two keys for when user start from beginning
                    result.AnimationInitialValues[channelIndex].InterpolationType = channel.InterpolationType;
                    result.AnimationInitialValues[channelIndex].Value1 = channel.KeyFrames[0];
                    result.AnimationInitialValues[channelIndex].Value2 = channel.KeyFrames[channel.KeyFrames.Count > 1 ? 1 : 0];

                    // Copy remaining keys for playback
                    for (int keyIndex = 2; keyIndex < channel.KeyFrames.Count; ++keyIndex)
                    {
                        // We need animation values two keys in advance
                        var requiredTime = channel.KeyFrames[keyIndex - 2].Time;

                        animationChannelValues.Add(new AnimationKeyValuePair<T> { ChannelIndex = channelIndex, RequiredTime = requiredTime, Value = channel.KeyFrames[keyIndex] });
                    }

                    // Add last frame again so that we have ValueNext == ValueEnd at end of curve
                    var lastKeyIndex = channel.KeyFrames.Count - 1;
                    var lastRequiredTime = channel.KeyFrames[channel.KeyFrames.Count > 1 ? lastKeyIndex - 1 : 0].Time; // important should not be "keyIndex - 2" or last frame will be skipped by update (two updates in a row)
                    animationChannelValues.Add(new AnimationKeyValuePair<T> { ChannelIndex = channelIndex, RequiredTime = lastRequiredTime, Value = channel.KeyFrames[lastKeyIndex] });
                }
            }

            // Gather all channel values in a single sorted array.
            // Since each channel values is already sorted, we can just merge them preserving sort order.
            // It is equivalent to:
            //  var animationConcatValues = Concat(animationValues);
            //  animationSortedValues = animationConcatValues.OrderBy(x => x.RequiredTime).ToArray();
            int animationValueCount = 0;

            // Setup and counting
            var animationChannelByNextTime = new MultiValueSortedDictionary<CompressedTimeSpan, KeyValuePair<int, int>>();
            for (int channelIndex = 0; channelIndex < animationChannels.Count; ++channelIndex)
            {
                var animationChannelValues = animationValues[channelIndex];
                animationValueCount += animationChannelValues.Count;
                if (animationChannelValues.Count > 0)
                    animationChannelByNextTime.Add(animationChannelValues[0].RequiredTime, new KeyValuePair<int, int>(channelIndex, 0));
            }

            // Initialize arrays
            result.AnimationSortedValueCount = animationValueCount;
            var animationSortedValues = new AnimationKeyValuePair<T>[(animationValueCount + AnimationSortedValueBlock - 1) / AnimationSortedValueBlock][];
            result.AnimationSortedValues = animationSortedValues;
            for (int i = 0; i < animationSortedValues.Length; ++i)
            {
                var remainingValueCount = animationValueCount - i * AnimationSortedValueBlock;
                animationSortedValues[i] = new AnimationKeyValuePair<T>[Math.Min(AnimationSortedValueBlock, remainingValueCount)];
            }

            // Fill with sorted values
            animationValueCount = 0;
            while (animationChannelByNextTime.Count > 0)
            {
                var firstItem = animationChannelByNextTime.First();
                animationSortedValues[animationValueCount / AnimationSortedValueBlock][animationValueCount % AnimationSortedValueBlock] = animationValues[firstItem.Value.Key][firstItem.Value.Value];
                animationValueCount++;
                animationChannelByNextTime.Remove(firstItem);

                // Add next item for further processing (if any)
                if (firstItem.Value.Value + 1 < animationValues[firstItem.Value.Key].Count)
                    animationChannelByNextTime.Add(animationValues[firstItem.Value.Key][firstItem.Value.Value + 1].RequiredTime, new KeyValuePair<int, int>(firstItem.Value.Key, firstItem.Value.Value + 1));
            }

            return result;
        }

        internal override AnimationCurveEvaluatorOptimizedGroup CreateEvaluator()
        {
            return AnimationCurveEvaluatorOptimizedGroup.Create<T>();
        }
    }

    [DataContract]
    [StructLayout(LayoutKind.Sequential)]
    public struct AnimationTargetProperty
    {
        public string Name { get; set; }
    }

    [DataContract]
    [StructLayout(LayoutKind.Sequential)]
    public struct AnimationKeyValuePair<T>
    {
        public int ChannelIndex;
        public CompressedTimeSpan RequiredTime;
        public KeyFrameData<T> Value;
        
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "Channel: {0} Required: {1} Value:{2}", ChannelIndex, RequiredTime.Ticks, Value);
        }
    }

    [DataContract]
    [StructLayout(LayoutKind.Sequential)]
    public struct AnimationInitialValues<T>
    {
        public AnimationCurveInterpolationType InterpolationType;
        public KeyFrameData<T> Value1;
        public KeyFrameData<T> Value2;
    }
}
