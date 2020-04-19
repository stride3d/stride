// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Rendering.Images
{
    /// <summary>
    /// How samples are fetched from the source texture when scaling
    /// </summary>
    public enum SamplingPattern
    {
        /// <summary>
        /// tilted pyramid gather, 9 taps, weights inverse to distance to center. (inspired by "Next Generation Post Processing in Call of Duty Advanced Warfare")
        /// </summary>
        Expanded,

        /// <summary>
        /// simple unique sampling at the center (the hardware sampler creates the 4 neighbor gathering).
        /// </summary>
        Linear,
    }
}
