// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Mathematics;

namespace Stride.Animations
{
    public class AnimationCurveEvaluatorOptimizedVector3Group : AnimationCurveEvaluatorOptimizedBlittableGroupBase<Vector3>
    {
        protected unsafe override void ProcessChannel(ref Channel channel, CompressedTimeSpan currentTime, IntPtr location, float factor)
        {
            if (channel.InterpolationType == AnimationCurveInterpolationType.Cubic)
            {
                Interpolator.Vector3.Cubic(
                    ref channel.ValuePrev.Value,
                    ref channel.ValueStart.Value,
                    ref channel.ValueEnd.Value,
                    ref channel.ValueNext.Value,
                    factor,
                    out *(Vector3*)(location + channel.Offset));
            }
            else if (channel.InterpolationType == AnimationCurveInterpolationType.Linear)
            {
                Interpolator.Vector3.Linear(
                    ref channel.ValueStart.Value,
                    ref channel.ValueEnd.Value,
                    factor,
                    out *(Vector3*)(location + channel.Offset));
            }
            else if (channel.InterpolationType == AnimationCurveInterpolationType.Constant)
            {
                *(Vector3*)(location + channel.Offset) = channel.ValueStart.Value;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
