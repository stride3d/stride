// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.CompilerServices;
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Animations
{
    public class AnimationCurveEvaluatorDirectBlittableGroup<T> : AnimationCurveEvaluatorDirectBlittableGroupBase<T>
    {
        protected override unsafe void ProcessChannel(ref Channel channel, CompressedTimeSpan newTime, IntPtr location)
        {
            SetTime(ref channel, newTime);

            var keyFrames = channel.Curve.KeyFrames;
            var currentIndex = channel.CurrentIndex;

            Unsafe.AsRef<T>((void*)(location + channel.Offset)) = keyFrames.Items[currentIndex].Value;
        }
    }
}
