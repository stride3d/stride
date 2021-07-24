// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Mathematics;

namespace Stride.Animations
{
    public class AnimationCurveEvaluatorDirectVector3Group : AnimationCurveEvaluatorDirectBlittableGroupBase<Vector3>
    {
        protected unsafe override void ProcessChannel(ref Channel channel, CompressedTimeSpan newTime, IntPtr location)
        {
            SetTime(ref channel, newTime);

            var currentTime = channel.CurrentTime;
            var currentIndex = channel.CurrentIndex;

            var keyFrames = channel.Curve.KeyFrames;
            var keyFramesItems = keyFrames.Items;
            var keyFramesCount = keyFrames.Count;

            // Extract data
            int timeStart = keyFrames[currentIndex + 0].Time.Ticks;
            int timeEnd = keyFrames[currentIndex + 1].Time.Ticks;

            // Compute interpolation factor and avoid NaN operations when timeStart >= timeEnd
            float t = (timeEnd <= timeStart) ? 0 : ((float)currentTime.Ticks - (float)timeStart) / ((float)timeEnd - (float)timeStart);

            if (channel.InterpolationType == AnimationCurveInterpolationType.Cubic)
            {
                Interpolator.Vector3.Cubic(
                    ref keyFramesItems[currentIndex > 0 ? currentIndex - 1 : 0].Value,
                    ref keyFramesItems[currentIndex].Value,
                    ref keyFramesItems[currentIndex + 1].Value,
                    ref keyFramesItems[currentIndex + 2 >= keyFramesCount ? currentIndex + 1 : currentIndex + 2].Value,
                    t,
                    out *(Vector3*)(location + channel.Offset));
            }
            else if (channel.InterpolationType == AnimationCurveInterpolationType.Linear)
            {
                Interpolator.Vector3.Linear(
                    ref keyFramesItems[currentIndex].Value,
                    ref keyFramesItems[currentIndex + 1].Value,
                    t,
                    out *(Vector3*)(location + channel.Offset));
            }
            else if (channel.InterpolationType == AnimationCurveInterpolationType.Constant)
            {
                *(Vector3*)(location + channel.Offset) = keyFrames[currentIndex].Value;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
