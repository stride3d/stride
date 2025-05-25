// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Mathematics;

namespace Stride.Animations
{
    public class AnimationCurveEvaluatorDirectQuaternionGroup : AnimationCurveEvaluatorDirectBlittableGroupBase<Quaternion>
    {
        protected unsafe override void ProcessChannel(ref Channel channel, CompressedTimeSpan newTime, IntPtr location)
        {
            SetTime(ref channel, newTime);

            var currentTime = channel.CurrentTime;
            var currentIndex = channel.CurrentIndex;

            var keyFrames = channel.Curve.KeyFrames;
            var keyFramesCount = keyFrames.Count;

            // Extract data
            int timeStart = keyFrames[currentIndex + 0].Time.Ticks;
            int timeEnd = keyFrames[currentIndex + 1].Time.Ticks;

            // Compute interpolation factor and avoid NaN operations when timeStart >= timeEnd
            float t = (timeEnd <= timeStart) ? 0 : ((float)currentTime.Ticks - (float)timeStart) / ((float)timeEnd - (float)timeStart);

            if (channel.InterpolationType == AnimationCurveInterpolationType.Cubic)
            {
                //TODO: because the cubic quaternion interpolation is not implemented yet;
                throw new NotImplementedException();
                
                // Interpolator.Quaternion.Cubic(
                //     ref keyFrames[currentIndex > 0 ? currentIndex - 1 : 0].Value,
                //     ref keyFrames[currentIndex].Value,
                //     ref keyFrames[currentIndex + 1].Value,
                //     ref keyFrames[currentIndex + 2 >= keyFramesCount ? currentIndex + 1 : currentIndex + 2].Value,
                //     t,
                //     out *(Quaternion*)(location + channel.Offset));
            }
            else if (channel.InterpolationType == AnimationCurveInterpolationType.Linear)
            {
                // Using spherical linear interpolation for quaternions
                
                var frameData1 = keyFrames[currentIndex].Value;
                var frameData2 = keyFrames[currentIndex + 1].Value;
                
                Interpolator.Quaternion.SphericalLinear(
                    ref frameData1,
                    ref frameData2,
                    t,
                    out *(Quaternion*)(location + channel.Offset));

                keyFrames[currentIndex] = keyFrames[currentIndex] with { Value = frameData1 };
                keyFrames[currentIndex + 1] = keyFrames[currentIndex + 1] with { Value = frameData2 };
            }
            else if (channel.InterpolationType == AnimationCurveInterpolationType.Constant)
            {
                *(Quaternion*)(location + channel.Offset) = keyFrames[currentIndex].Value;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
