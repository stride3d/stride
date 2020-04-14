// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Graphics
{
    /// <summary>
    /// Comparison options.
    /// </summary>
    /// <remarks>
    /// A comparison option determines whether how the runtime compares source (new) data against destination (existing) data before storing the new data. 
    /// The comparison option is declared in a description before an object is created. 
    /// The API allows you to set a comparison option for a depth-stencil buffer (see <see cref="IDepthStencilState"/>), depth-stencil operations, or sampler state (see <see cref="SamplerState"/>).
    /// </remarks>
    [DataContract]
    public enum CompareFunction
    {
        /// <summary>
        /// Never pass the comparison.
        /// </summary>
        Never = 1,

        /// <summary>
        /// If the source data is less than the destination data, the comparison passes.
        /// </summary>
        Less = 2,

        /// <summary>
        /// If the source data is equal to the destination data, the comparison passes.
        /// </summary>
        Equal = 3,

        /// <summary>
        /// If the source data is less than or equal to the destination data, the comparison passes.
        /// </summary>
        LessEqual = 4,

        /// <summary>
        /// If the source data is greater than the destination data, the comparison passes.
        /// </summary>
        Greater = 5,

        /// <summary>
        /// If the source data is not equal to the destination data, the comparison passes.
        /// </summary>
        NotEqual = 6,

            /// <summary>
        /// If the source data is greater than or equal to the destination data, the comparison passes.
        /// </summary>
        GreaterEqual = 7,

        /// <summary>
        /// Always pass the comparison.
        /// </summary>
        Always = 8,
    }
}
