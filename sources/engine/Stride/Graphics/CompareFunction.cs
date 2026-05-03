// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Graphics;

/// <summary>
///   Identifies comparison functions that can be used to determine how the runtime compares
///   source (new) data against destination (existing) data before storing the new data.
/// </summary>
/// <remarks>
///   The comparison functions can be used for a Depth-Stencil Buffer (see <see cref="DepthStencilState"/>)
///   for depth comparisons or rejections, or for stencil operations, or for Texture sampling
///   (see <see cref="SamplerState"/>).
/// </remarks>
[DataContract]
public enum CompareFunction
{
    /// <summary>
    ///   <strong>Never</strong> pass the comparison.
    /// </summary>
    Never = 1,

    /// <summary>
    ///   If the source data is <strong>less than</strong> the destination data, the comparison passes.
    /// </summary>
    Less = 2,

    /// <summary>
    ///   If the source data is <strong>equal</strong> to the destination data, the comparison passes.
    /// </summary>
    Equal = 3,

    /// <summary>
    ///   If the source data is <strong>less than or equal</strong> to the destination data, the comparison passes.
    /// </summary>
    LessEqual = 4,

    /// <summary>
    ///   If the source data is <strong>greater than</strong> the destination data, the comparison passes.
    /// </summary>
    Greater = 5,

    /// <summary>
    ///   If the source data is <strong>not equal</strong> to the destination data, the comparison passes.
    /// </summary>
    NotEqual = 6,

    /// <summary>
    ///   If the source data is <strong>greater than or equal</strong> to the destination data, the comparison passes.
    /// </summary>
    GreaterEqual = 7,

    /// <summary>
    ///   <strong>Always</strong> pass the comparison.
    /// </summary>
    Always = 8
}
