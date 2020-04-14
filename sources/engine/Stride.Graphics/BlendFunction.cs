// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Graphics
{
    /// <summary>
    /// RGB or alpha blending operation.
    /// </summary>
    /// <remarks>
    /// The runtime implements RGB blending and alpha blending separately. Therefore, blend state requires separate blend operations for RGB data and alpha data. These blend operations are specified in a <see cref="BlendState"/>.
    /// </remarks>
    [DataContract]
    public enum BlendFunction
    {
        /// <summary>
        /// Add source 1 and source 2. 
        /// </summary>
        Add = 1,

        /// <summary>
        /// Subtract source 1 from source 2. 
        /// </summary>
        Subtract = 2,

        /// <summary>
        /// Subtract source 2 from source 1. 
        /// </summary>
        ReverseSubtract = 3,

        /// <summary>
        /// Find the minimum of source 1 and source 2. 
        /// </summary>
        Min = 4,

        /// <summary>
        /// Find the maximum of source 1 and source 2. 
        /// </summary>
        Max = 5,
    }
}
