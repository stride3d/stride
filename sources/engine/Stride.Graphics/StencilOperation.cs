// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Graphics;

/// <summary>
///   Specifies the operation to perform on the stencil part of the Depth-Stencil Buffer when rasterizing primitives.
/// </summary>
/// <remarks>
///   This enumeration is part of a Depth-Stencil State object description (see <see cref="DepthStencilStateDescription"/>).
/// </remarks>
[DataContract]
public enum StencilOperation
{
    /// <summary>
    ///   Keeps the current value of the stencil buffer, without modifying it.
    /// </summary>
    Keep = 1,

    /// <summary>
    ///   Sets the value of the stencil buffer to zero.
    /// </summary>
    Zero = 2,

    /// <summary>
    ///   Sets the value of the stencil buffer to a specified value when the stencil test passes.
    /// </summary>
    Replace = 3,

    /// <summary>
    ///   Increments the value of the stencil buffer by one, saturating at the maximum value allowed (usually 255 for 8-bit buffers).
    /// </summary>
    IncrementSaturation = 4,

    /// <summary>
    ///   Decrements the value of the stencil buffer by one, saturating at zero (usually 0 for 8-bit buffers).
    /// </summary>
    DecrementSaturation = 5,

    /// <summary>
    ///   Inverts all bits in the stencil buffer value, effectively flipping each bit from 0 to 1 and from 1 to 0.
    /// </summary>
    Invert = 6,

    /// <summary>
    ///   Increments the value of the stencil buffer by one, wrapping around to zero if it exceeds the maximum value.
    /// </summary>
    Increment = 7,

    /// <summary>
    ///   Decrements the value of the stencil buffer by one, wrapping around to the maximum value if it goes below zero.
    /// </summary>
    Decrement = 8
}
