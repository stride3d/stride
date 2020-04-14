// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Collections;
using Stride.Core.Extensions;

namespace Stride.Animations
{
    /// <summary>
    /// List of float key frame data applying to a specific property in a node.
    /// </summary>
    public class AnimationChannel
    {
        public AnimationChannel()
        {
            KeyFrames = new List<KeyFrameData<float>>();
        }

        public float EvaluateCubic(CompressedTimeSpan time)
        {
            int keyIndex;
            for (keyIndex = 0; keyIndex < KeyFrames.Count - 1; ++keyIndex)
            {
                if (time < KeyFrames[keyIndex + 1].Time)
                    break;
            }

            // TODO: Check if before or after curve (and on those limits)

            // Exact value, just returns it.
            if (time == KeyFrames[keyIndex].Time)
                return KeyFrames[keyIndex].Value;

            // http://en.wikipedia.org/wiki/Cubic_Hermite_spline#Interpolation_on_the_unit_interval_without_exact_derivatives
            long timeStart = KeyFrames[keyIndex + 0].Time.Ticks;
            long timeEnd = KeyFrames[keyIndex + 1].Time.Ticks;

            // Compute interpolation factor and avoid NaN operations when timeStart >= timeEnd
            float t = (timeEnd <= timeStart) ? 0 : ((float)time.Ticks - (float)timeStart) / ((float)timeEnd - (float)timeStart);

            var evaluator = new EvaluatorData();
            evaluator.ValuePrev = KeyFrames[keyIndex > 0 ? keyIndex - 1 : 0];
            evaluator.ValueStart = KeyFrames[keyIndex + 0];
            evaluator.ValueEnd = KeyFrames[keyIndex + 1];
            evaluator.ValueNext = KeyFrames[keyIndex + 2 < KeyFrames.Count ? keyIndex + 2 : KeyFrames.Count - 1];

            return evaluator.Evaluate(t);
        }

        /// <summary>
        /// Evaluates the error within specified segment.
        /// </summary>
        /// <param name="originalCurve">The original curve.</param>
        /// <param name="evaluator">The evaluator.</param>
        /// <param name="stepSize">Size of the step.</param>
        /// <param name="keyFrame">The key frame.</param>
        /// <param name="nextKeyFrame">The next key frame.</param>
        /// <returns></returns>
        private KeyValuePair<CompressedTimeSpan, float> EvaluateError(Func<CompressedTimeSpan, float> originalCurve, Evaluator evaluator, CompressedTimeSpan stepSize, KeyFrameData<float> keyFrame, KeyFrameData<float> nextKeyFrame)
        {
            var startTime = keyFrame.Time;
            var endTime = nextKeyFrame.Time;

            var biggestDifference = 0.0f;
            var biggestDifferenceTime = startTime;

            // Rounds up start time (i.e. startTime is multiple of stepSize)
            startTime = new CompressedTimeSpan((startTime.Ticks / stepSize.Ticks + 1) * stepSize.Ticks);

            for (var time = startTime; time < endTime; time += stepSize)
            {
                var difference = Math.Abs(originalCurve(time) - evaluator.Evaluate(time));

                if (difference > biggestDifference)
                {
                    biggestDifference = difference;
                    biggestDifferenceTime = time;
                }
            }
            return new KeyValuePair<CompressedTimeSpan, float>(biggestDifferenceTime, biggestDifference);
        }

        private class ErrorComparer : IComparer<LinkedListNode<ErrorNode>>
        {
            public int Compare(LinkedListNode<ErrorNode> x, LinkedListNode<ErrorNode> y)
            {
                if (x.Value.Error != y.Value.Error)
                    return Math.Sign(x.Value.Error - y.Value.Error);

                return x.Value.GetHashCode() - y.Value.GetHashCode();
            }
        }

        public class ErrorNode
        {
            public LinkedListNode<KeyFrameData<float>> KeyFrame;
            public CompressedTimeSpan BiggestDeltaTime;
            public float Error;

            public ErrorNode(LinkedListNode<KeyFrameData<float>> keyFrame, CompressedTimeSpan biggestDeltaTime, float error)
            {
                KeyFrame = keyFrame;
                BiggestDeltaTime = biggestDeltaTime;
                Error = error;
            }
        }

        public void Fitting(Func<CompressedTimeSpan, float> originalCurve, CompressedTimeSpan stepSize, float maxErrorThreshold)
        {
            // Some info: http://wscg.zcu.cz/wscg2008/Papers_2008/full/A23-full.pdf
            // Compression of Temporal Video Data by Catmull-Rom Spline and Quadratic B�zier Curve Fitting
            // And: http://bitsquid.blogspot.jp/2009/11/bitsquid-low-level-animation-system.html

            // Only one or zero keys: no need to do anything.
            if (KeyFrames.Count <= 1)
                return;

            var keyFrames = new LinkedList<KeyFrameData<float>>(this.KeyFrames);
            var evaluator = new Evaluator(keyFrames);

            // Compute initial errors (using Least Square Equation)
            var errors = new LinkedList<ErrorNode>();
            //var errors = new List<float>();
            var errorQueue = new PriorityQueue<LinkedListNode<ErrorNode>>(new ErrorComparer());
            foreach (var keyFrame in keyFrames.EnumerateNodes())
            {
                if (keyFrame.Next == null)
                    break;
                var error = EvaluateError(originalCurve, evaluator, stepSize, keyFrame.Value, keyFrame.Next.Value);
                var errorNode = errors.AddLast(new ErrorNode(keyFrame, error.Key, error.Value));
                errorQueue.Enqueue(errorNode);
            }
            //for (int keyFrame = 0; keyFrame < KeyFrames.Count - 1; ++keyFrame)
            //{
            //    //errors.Add(EvaluateError(originalCurve, evaluator, stepSize, keyFrame));
            //    var errorNode = errors.AddLast(new ErrorNode(EvaluateError(originalCurve, evaluator, stepSize, keyFrame)));
            //    errorQueue.Enqueue(errorNode);
            //}

            while (true)
            {
                // Already reached enough subdivisions
                var highestError = errorQueue.Dequeue();
                if (highestError.Value.Error <= maxErrorThreshold)
                    break;

                //// Find segment to optimize
                //var biggestErrorIndex = 0;
                //for (int keyFrame = 1; keyFrame < KeyFrames.Count - 1; ++keyFrame)
                //{
                //    if (errors[keyFrame] > errors[biggestErrorIndex])
                //        biggestErrorIndex = keyFrame;
                //}

                //// Already reached enough subdivisions
                //if (errors[biggestErrorIndex] <= maxErrorThreshold)
                //    break;

                // Create new key (use middle point, but better heuristic might improve situation -- like point with biggest error)
                //var middleTime = (start.Value.Time + end.Value.Time) / 2;
                var middleTime = highestError.Value.BiggestDeltaTime;
                //KeyFrames.Insert(biggestErrorIndex + 1, new KeyFrameData<float> { Time = middleTime, Value = originalCurve(middleTime) });
                var newKeyFrame = keyFrames.AddAfter(highestError.Value.KeyFrame, new KeyFrameData<float> { Time = middleTime, Value = originalCurve(middleTime) });
                //errors.Insert(biggestErrorIndex + 1, 0.0f);
                var newError = errors.AddAfter(highestError, new ErrorNode(newKeyFrame, CompressedTimeSpan.Zero, 0.0f));

                var highestErrorLastUpdate = newError;
                if (highestErrorLastUpdate.Next != null)
                    highestErrorLastUpdate = highestErrorLastUpdate.Next;

                // Invalidate evaluator (data changed)
                evaluator.InvalidateTime();

                // Update errors
                for (var error = highestError.Previous ?? highestError; error != null; error = error.Next)
                {
                    if (error != highestError && error != newError)
                        errorQueue.Remove(error);

                    var errorInfo = EvaluateError(originalCurve, evaluator, stepSize, error.Value.KeyFrame.Value, error.Value.KeyFrame.Next.Value);
                    error.Value.BiggestDeltaTime = errorInfo.Key;
                    error.Value.Error = errorInfo.Value;

                    errorQueue.Enqueue(error);

                    if (error == highestErrorLastUpdate)
                        break;
                }
            }

            KeyFrames = new List<KeyFrameData<float>>(keyFrames);
        }

        public List<KeyFrameData<float>> KeyFrames { get; set; }

        /// <summary>
        /// Gets or sets the target object name.
        /// </summary>
        /// <value>
        /// The target object name.
        /// </value>
        public string TargetObject { get; set; }

        /// <summary>
        /// Gets or sets the target property name.
        /// </summary>
        /// <value>
        /// The target property name.
        /// </value>
        public string TargetProperty { get; set; }

        internal static void InitializeAnimation(ref EvaluatorData animationChannel, ref AnimationInitialValues<float> animationValue)
        {
            animationChannel.ValuePrev = animationValue.Value1;
            animationChannel.ValueStart = animationValue.Value1;
            animationChannel.ValueEnd = animationValue.Value1;
            animationChannel.ValueNext = animationValue.Value2;
        }

        internal static void UpdateAnimation(ref EvaluatorData animationChannel, ref KeyFrameData<float> animationValue)
        {
            animationChannel.ValuePrev = animationChannel.ValueStart;
            animationChannel.ValueStart = animationChannel.ValueEnd;
            animationChannel.ValueEnd = animationChannel.ValueNext;
            animationChannel.ValueNext = animationValue;
        }
        
        public class Evaluator
        {
            private EvaluatorData data;
            private CompressedTimeSpan currentTime;
            private bool reachedEnd;
            private IEnumerable<KeyFrameData<float>> keyFrames;
            private IEnumerator<KeyFrameData<float>> currentKeyFrame;
            //private KeyFrameData<float> ValueStart;
            //private KeyFrameData<float> ValueEnd;
            
            public Evaluator(IEnumerable<KeyFrameData<float>> keyFrames)
            {
                this.keyFrames = keyFrames;
                //this.ValueStart = keyFrames.First();
                //this.ValueEnd = keyFrames.Last();
                InvalidateTime();
            }

            public float Evaluate(CompressedTimeSpan time)
            {
                SetTime(time);

                var startTime = data.ValueStart.Time;
                var endTime = data.ValueEnd.Time;

                if (currentTime < startTime || currentTime > endTime)
                {
                }

                // Sampling before start (should not really happen because we add a keyframe at TimeSpan.Zero, but let's keep it in case it changes later.
                if (currentTime <= startTime)
                    return data.ValueStart.Value;

                // Sampling after end
                if (currentTime >= endTime)
                    return data.ValueEnd.Value;

                // Sampling with catmull rom implicit approximation
                float factor = (float)(currentTime - startTime).Ticks / (float)(endTime - startTime).Ticks;
                return data.Evaluate(factor);
            }

            public void InvalidateTime()
            {
                reachedEnd = false;

                currentKeyFrame = keyFrames.GetEnumerator();

                var animationInitialValues = new AnimationInitialValues<float>();

                // Skip two elements (right before third)
                currentKeyFrame.MoveNext();
                animationInitialValues.Value1 = currentKeyFrame.Current;
                currentKeyFrame.MoveNext();
                animationInitialValues.Value2 = currentKeyFrame.Current;

                currentTime = animationInitialValues.Value1.Time;

                InitializeAnimation(ref data, ref animationInitialValues);
            }

            private void SetTime(CompressedTimeSpan timeSpan)
            {
                // TODO: Add jump frames to do faster seeking.
                // If user seek back, need to start from beginning
                if (timeSpan < currentTime)
                {
                    InvalidateTime();
                }

                currentTime = timeSpan;

                // Advance until requested time is reached
                while (!(currentTime >= data.ValueStart.Time && currentTime < data.ValueEnd.Time) && !reachedEnd)
                {
                    var moveNextFrame = currentKeyFrame.MoveNext();
                    if (!moveNextFrame)
                    {
                        reachedEnd = true;
                        UpdateAnimation(ref data, ref data.ValueNext);
                        UpdateAnimation(ref data, ref data.ValueNext);
                        break;
                    }
                    var keyFrame = currentKeyFrame.Current;
                    UpdateAnimation(ref data, ref keyFrame);
                }

                currentTime = timeSpan;
            }
        }

        public struct EvaluatorData
        {
            public float Evaluate(float t)
            {
                // http://en.wikipedia.org/wiki/Cubic_Hermite_spline#Interpolation_on_the_unit_interval_without_exact_derivatives
                float t2 = t * t;
                float t3 = t2 * t;

                float factor0 = -t3 + 2.0f * t2 - t;
                float factor1 = 3.0f * t3 - 5.0f * t2 + 2.0f;
                float factor2 = -3.0f * t3 + 4.0f * t2 + t;
                float factor3 = t3 - t2;

                return 0.5f * (ValuePrev.Value * factor0
                             + ValueStart.Value * factor1
                             + ValueEnd.Value * factor2
                             + ValueNext.Value * factor3);
            }

            public KeyFrameData<float> ValuePrev;
            public KeyFrameData<float> ValueStart;
            public KeyFrameData<float> ValueEnd;
            public KeyFrameData<float> ValueNext;
        }
    }
}
