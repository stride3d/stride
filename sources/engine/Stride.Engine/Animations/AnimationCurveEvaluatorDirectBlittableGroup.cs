// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core;
using Xenko.Core.Mathematics;

namespace Xenko.Animations
{
    public class AnimationCurveEvaluatorDirectBlittableGroup<T> : AnimationCurveEvaluatorDirectBlittableGroupBase<T>
    {
        protected override unsafe void ProcessChannel(ref Channel channel, CompressedTimeSpan newTime, IntPtr location)
        {
            SetTime(ref channel, newTime);

            var keyFrames = channel.Curve.KeyFrames;
            var currentIndex = channel.CurrentIndex;

            Interop.CopyInline((void*)(location + channel.Offset), ref keyFrames.Items[currentIndex].Value);
        }
    }
}
