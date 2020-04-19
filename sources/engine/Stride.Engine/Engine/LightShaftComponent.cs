// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Engine.Processors;
using Stride.Rendering;
using Stride.Rendering.Shadows;

namespace Stride.Engine
{
    /// <summary>
    /// The source for light shafts, should be placed on the same entity as the light component which will be used for light shafts
    /// </summary>
    [Display("Light shaft", Expand = ExpandRule.Always)]
    [DataContract("LightShaftComponent")]
    [DefaultEntityComponentRenderer(typeof(LightShaftProcessor))]
    [ComponentCategory("Lights")]
    public class LightShaftComponent : ActivableEntityComponent
    {
        /// <summary>
        /// Density of the light shaft fog
        /// </summary>
        /// <userdoc>
        /// Higher values produce brighter light shafts
        /// </userdoc>
        [Display("Density")]
        public float DensityFactor { get; set; } = 0.002f;

        /// <summary>
        /// Number of samples taken per pixel
        /// </summary>
        /// <userdoc>
        /// Higher sample counts produce better light shafts but use more GPU
        /// </userdoc>
        [DataMemberRange(1, 0)]
        public int SampleCount { get; set; } = 16;

        /// <summary>
        /// If true, all bounding volumes will be drawn one by one.
        /// </summary>
        /// <remarks>
        /// If this is off, the light shafts might be lower in quality if the bounding volumes overlap (in the same pixel). 
        /// If this is on, and the bounding volumes overlap (in space), the light shafts inside the overlapping area will become twice as bright.
        /// </remarks>
        /// <userdoc>
        /// This preserves light shaft quality when seen through separate bounding boxes, but uses more GPU
        /// </userdoc>
        [Display("Process bounding volumes separately")]
        public bool SeparateBoundingVolumes { get; set; } = true;
    }
}
