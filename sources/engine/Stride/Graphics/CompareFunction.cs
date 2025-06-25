// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Graphics;

[DataContract]
public enum CompareFunction
{
    /// <summary>
    /// Comparison options.
    /// </summary>
    /// <remarks>
    /// A comparison option determines whether how the runtime compares source (new) data against destination (existing) data before storing the new data. 
    /// The comparison option is declared in a description before an object is created. 
    /// The API allows you to set a comparison option for a depth-stencil buffer (see <see cref="IDepthStencilState"/>), depth-stencil operations, or sampler state (see <see cref="SamplerState"/>).
    /// </remarks>
        /// <summary>
        /// Never pass the comparison.
        /// </summary>
        /// <summary>
        /// If the source data is less than the destination data, the comparison passes.
        /// </summary>
        /// <summary>
        /// If the source data is equal to the destination data, the comparison passes.
        /// </summary>
        /// <summary>
        /// If the source data is less than or equal to the destination data, the comparison passes.
        /// </summary>
        /// <summary>
        /// If the source data is greater than the destination data, the comparison passes.
        /// </summary>
        /// <summary>
        /// If the source data is not equal to the destination data, the comparison passes.
        /// </summary>
            /// <summary>
        /// If the source data is greater than or equal to the destination data, the comparison passes.
        /// </summary>
        /// <summary>
        /// Always pass the comparison.
        /// </summary>
    /// </summary>
    Never = 1,

    Less = 2,

    Equal = 3,

    LessEqual = 4,

    Greater = 5,

    NotEqual = 6,

    GreaterEqual = 7,

    Always = 8
}
