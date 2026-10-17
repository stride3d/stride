// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   A range of the sub-Resources of a <see cref="Texture"/>: mip-levels and array slices, numbered as
///   <see cref="Texture.GetSubResourceIndex"/> does.
/// </summary>
/// <remarks>
///   The depth slices of a three-dimensional Texture are not sub-Resources, so its range has a single array slice.
/// </remarks>
/// <param name="MipStart">The first mip-level.</param>
/// <param name="MipCount">The number of mip-levels.</param>
/// <param name="ArraySliceStart">The first array slice.</param>
/// <param name="ArraySliceCount">The number of array slices.</param>
internal readonly record struct TextureSubresourceRange(int MipStart, int MipCount, int ArraySliceStart, int ArraySliceCount)
{
    /// <summary>
    ///   Gets the mip-level after the last one in the range.
    /// </summary>
    public int MipEnd => MipStart + MipCount;

    /// <summary>
    ///   Gets the array slice after the last one in the range.
    /// </summary>
    public int ArraySliceEnd => ArraySliceStart + ArraySliceCount;
}
