// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Xenko.Rendering
{
    /// <summary>
    /// Describe the different tessellation methods used in Xenko.
    /// </summary>
    [Flags]
    public enum XenkoTessellationMethod
    {
        /// <summary>
        /// No tessellation
        /// </summary>
        None = 0,

        /// <summary>
        /// Flat tessellation. Also known as dicing tessellation.
        /// </summary>
        Flat = 1,

        /// <summary>
        /// Point normal tessellation.
        /// </summary>
        PointNormal = 1,

        /// <summary>
        /// Adjacent edge average.
        /// </summary>
        AdjacentEdgeAverage = 2,
    }
}
