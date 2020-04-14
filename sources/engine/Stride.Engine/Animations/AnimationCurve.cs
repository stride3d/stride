// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single class
using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core;
using Xenko.Core.Collections;
using Xenko.Internals;

namespace Xenko.Animations
{
    /// <summary>
    /// Untyped base class for animation curves.
    /// </summary>
    [DataContract(Inherited = true)]
    public abstract class AnimationCurve
    {
        /// <summary>
        /// Gets or sets the interpolation type.
        /// </summary>
        /// <value>
        /// The interpolation type.
        /// </value>
        public AnimationCurveInterpolationType InterpolationType { get; set; }

        /// <summary>
        /// Gets the type of keyframe values.
        /// </summary>
        /// <value>
        /// The type of keyframe values.
        /// </value>
        public abstract Type ElementType { get; }

        /// <summary>
        /// Gets the size of keyframe values.
        /// </summary>
        /// <value>
        /// The size of keyframe values.
        /// </value>
        public abstract int ElementSize { get; }

        [DataMemberIgnore]
        public abstract IReadOnlyList<CompressedTimeSpan> Keys { get; }

        /// <summary>
        /// Writes a new value at the end of the curve (used for building curves).
        /// It should be done in increasing order as it will simply add a new key at the end of <see cref="AnimationCurve{T}.KeyFrames"/>.
        /// </summary>
        /// <param name="newTime">The new time.</param>
        /// <param name="location">The location.</param>
        public abstract void AddValue(CompressedTimeSpan newTime, IntPtr location);

        /// <summary>
        /// Meant for internal use, to call AnimationData{T}.FromAnimationChannels() without knowing the generic type.
        /// </summary>
        /// <param name="animationChannelsByName"></param>
        internal abstract AnimationData CreateOptimizedData(IEnumerable<KeyValuePair<string, AnimationCurve>> animationChannelsByName);

        internal abstract AnimationCurveEvaluatorDirectGroup CreateEvaluator();

        protected AnimationCurve()
        {
            InterpolationType = AnimationCurveInterpolationType.Linear;
        }

        /// <summary>
        /// Shifts all animation keys by the specified time, adding it to all <see cref="KeyFrameData.Time"/>
        /// </summary>
        /// <param name="shiftTimeSpan">The time span by which the keys should be shifted</param>
        public virtual void ShiftKeys(CompressedTimeSpan shiftTimeSpan) { }
    }

    /// <summary>
    /// Typed class for animation curves.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DataContract]
    public class AnimationCurve<T> : AnimationCurve
    {
        /// <summary>
        /// Gets or sets the key frames.
        /// </summary>
        /// <value>
        /// The key frames.
        /// </value>
        public FastList<KeyFrameData<T>> KeyFrames { get; set; }

        /// <inheritdoc/>
        [DataMemberIgnore]
        public override Type ElementType
        {
            get { return typeof(T); }
        }

        /// <inheritdoc/>
        [DataMemberIgnore]
        public override int ElementSize
        {
            get { return Utilities.UnsafeSizeOf<T>(); }
        }

        /// <inheritdoc/>
        [DataMemberIgnore]
        public override IReadOnlyList<CompressedTimeSpan> Keys
        {
            get { return new LambdaReadOnlyCollection<KeyFrameData<T>, CompressedTimeSpan>(KeyFrames, x => x.Time); }
        }

        public AnimationCurve()
        {
            KeyFrames = new FastList<KeyFrameData<T>>();
        }

        /// <summary>
        /// Find key index.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public int FindKeyIndex(CompressedTimeSpan time)
        {
            // Simple binary search
            int start = 0;
            int end = KeyFrames.Count - 1;
            while (start <= end)
            {
                int middle = start + ((end - start) >> 1);
                var middleTime = KeyFrames[middle].Time;

                if (middleTime == time)
                {
                    return middle;
                }
                if (middleTime < time)
                {
                    start = middle + 1;
                }
                else
                {
                    end = middle - 1;
                }
            }
            return start;
        }

        /// <inheritdoc/>
        public override void AddValue(CompressedTimeSpan newTime, IntPtr location)
        {
            T value;
            Utilities.UnsafeReadOut(location, out value);
            KeyFrames.Add(new KeyFrameData<T> { Time = (CompressedTimeSpan)newTime, Value = value });
        }

        /// <inheritdoc/>
        internal override AnimationData CreateOptimizedData(IEnumerable<KeyValuePair<string, AnimationCurve>> animationChannelsByName)
        {
            return AnimationData<T>.FromAnimationChannels(animationChannelsByName.Select(x => new KeyValuePair<string, AnimationCurve<T>>(x.Key, (AnimationCurve<T>)x.Value)).ToList());
        }

        /// <inheritdoc/>
        public override void ShiftKeys(CompressedTimeSpan shiftTimeSpan)
        {
            var shiftedKeyFrames = new FastList<KeyFrameData<T>>();

            foreach (var keyFrameData in KeyFrames)
            {
                shiftedKeyFrames.Add(new KeyFrameData<T> { Time = keyFrameData.Time + shiftTimeSpan, Value = keyFrameData.Value });
            }

            KeyFrames.Clear();

            KeyFrames = shiftedKeyFrames;
        }

        internal override AnimationCurveEvaluatorDirectGroup CreateEvaluator()
        {
            return AnimationCurveEvaluatorDirectGroup.Create<T>();
        }
    }
}
