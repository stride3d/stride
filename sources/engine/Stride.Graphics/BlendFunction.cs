// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Graphics;

/// <summary>
///   Defines a function for color blending.
/// </summary>
/// <remarks>
///   The runtime implements color (RGB) blending and alpha blending separately. Therefore, a Blend State requires separate blend operations
///   for RGB data and alpha data. These blend operations are specified in a <see cref="BlendStateDescription"/> and in a <see cref="BlendStateRenderTargetDescription"/>.
/// </remarks>
[DataContract]
public enum BlendFunction
{
    /// <summary>
    ///   The function adds destination to the source: <c>(srcColor * srcBlend) + (destColor * destBlend)</c>
    /// </summary>
    Add = 1,

    /// <summary>
    ///   The function subtracts destination from source: <c>(srcColor * srcBlend) − (destColor * destBlend)</c>
    /// </summary>
    Subtract = 2,

    /// <summary>
    ///   The function subtracts source from destination: <c>(destColor * destBlend) - (srcColor * srcBlend)</c>
    /// </summary>
    ReverseSubtract = 3,

    /// <summary>
    ///   The function finds the minimum of the source and destination: <c>min( (srcColor * srcBlend), (destColor * destBlend) )</c>
    /// </summary>
    Min = 4,

    /// <summary>
    ///   The function finds the maximum of the source and destination: <c>max( (srcColor * srcBlend), (destColor * destBlend) )</c>
    /// </summary>
    Max = 5
}
