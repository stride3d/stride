// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core;

namespace Stride.Animations
{
    public class AnimationCurveEvaluatorOptimizedBlittableGroup<T> : AnimationCurveEvaluatorOptimizedBlittableGroupBase<T>
    {
        protected override unsafe void ProcessChannel(ref Channel channel, CompressedTimeSpan currentTime, IntPtr location, float factor)
        {
            Interop.CopyInline((void*)(location + channel.Offset), ref channel.ValueStart.Value);
        }
    }
}
