// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;

namespace Xenko.Particles.ShapeBuilders
{
    /// <summary>
    /// Specifies if the trail lies on one edge on the axis or is the axis in its center.
    /// </summary>
    [DataContract("EdgePolicy")]
    [Display("Edge")]
    public enum EdgePolicy
    {
        /// <summary>
        /// The line between the control points will be used as an edge for the trail or ribbon
        /// </summary>
        Edge,

        /// <summary>
        /// The line between the control points will be used as a central axis for the trail or ribbon
        /// </summary>
        Center,
    }
}
