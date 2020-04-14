// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Particles.ShapeBuilders
{
    /// <summary>
    /// Specifies if the ribbon should be additionally smoothed or rendered as is.
    /// </summary>
    [DataContract("SmoothingPolicy")]
    [Display("Smoothing")]
    public enum SmoothingPolicy
    {
        /// <summary>
        /// Ribbons only use control points and edges are hard. Good for straight lines
        /// </summary>
        None,

        /// <summary>
        /// Smoothing using Catmull-Rom interpolation. Generally looks good
        /// </summary>
        Fast,

        /// <summary>
        /// Smoothing based on circumcircles generated around every three adjacent points. Best suited for rapid, circular motions
        /// </summary>
        Best, 
    }
}
