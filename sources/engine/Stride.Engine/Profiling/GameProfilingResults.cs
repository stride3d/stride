// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Profiling
{
    public enum GameProfilingResults
    {
        [Display("FPS")]
        Fps,

        [Display("CPU profiling events")]
        CpuEvents,

        [Display("GPU profiling events")]
        GpuEvents,
    }
}
