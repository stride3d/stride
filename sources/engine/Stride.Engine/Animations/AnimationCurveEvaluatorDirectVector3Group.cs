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
            var keyFramesCount = keyFrames.Count;

            // Extract data
            int timeStart = keyFrames[currentIndex + 0].Time.Ticks;
            int timeEnd = keyFrames[currentIndex + 1].Time.Ticks;

            // Compute interpolation factor and avoid NaN operations when timeStart >= timeEnd
            float t = (timeEnd <= timeStart) ? 0 : ((float)currentTime.Ticks - (float)timeStart) / ((float)timeEnd - (float)timeStart);

            if (channel.InterpolationType == AnimationCurveInterpolationType.Cubic)
            {
                var index01 = currentIndex > 0 ? currentIndex - 1 : 0;
                var index02 = currentIndex;
                var index03 = currentIndex + 1;
                var index04 = currentIndex + 2 >= keyFramesCount ? currentIndex + 1 : currentIndex + 2;
                
                var keyFrameData01 = keyFrames[index01].Value;
                var keyFrameData02 = keyFrames[index02].Value;
                var keyFrameData03 = keyFrames[index03].Value;
                var keyFrameData04 = keyFrames[index04].Value;
                
                Interpolator.Vector3.Cubic(ref keyFrameData01,
                                           ref keyFrameData02,
                                           ref keyFrameData03,
                                           ref keyFrameData04,
                                           t,
                                           out *(Vector3*)(location + channel.Offset));
                
                keyFrames[index01] = keyFrames[index01] with { Value = keyFrameData01 };
                keyFrames[index02] = keyFrames[index02] with { Value = keyFrameData02 };
                keyFrames[index03] = keyFrames[index03] with { Value = keyFrameData03 };
                keyFrames[index04] = keyFrames[index04] with { Value = keyFrameData04 };
                
                
            }
            else if (channel.InterpolationType == AnimationCurveInterpolationType.Linear)
            {
                var index01 = currentIndex;
                var index02 = currentIndex + 1;
                
                var keyFrameData01 = keyFrames[index01].Value;
                var keyFrameData02 = keyFrames[index02].Value;
                
                Interpolator.Vector3.Linear(
                    ref keyFrameData01,
                    ref keyFrameData02,
                    t,
                    out *(Vector3*)(location + channel.Offset));
                
                keyFrames[index01] = keyFrames[index01] with { Value = keyFrameData01 };
                keyFrames[index02] = keyFrames[index02] with { Value = keyFrameData02 };
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
