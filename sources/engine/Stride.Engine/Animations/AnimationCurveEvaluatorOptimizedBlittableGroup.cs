// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.CompilerServices;
using Stride.Core;

namespace Stride.Animations
{
    public class AnimationCurveEvaluatorOptimizedBlittableGroup<T> : AnimationCurveEvaluatorOptimizedBlittableGroupBase<T>
    {
        protected override unsafe void ProcessChannel(ref Channel channel, CompressedTimeSpan currentTime, IntPtr location, float factor)
        {
            Unsafe.AsRef<T>((void*)(location + channel.Offset)) = channel.ValueStart.Value;
        }
    }
}
