// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics;

/// <summary>
///   Defines extensions and helpers for <see cref="PrimitiveType"/>.
/// </summary>
public static class PrimitiveTypeExtensions
{
    /// <summary>
    ///   Interpret the input vertex data type as a <strong>patch list</strong> for tesselation with a
    ///   specific number of <strong>control points</strong>.
    /// </summary>
    /// <param name="controlPoints">The number of control points. It must be a value in the range 1 to 32, inclusive.</param>
    /// <returns>
    ///   A <see cref="PrimitiveType"/> value that represents a patch list with the specified number of control points.
    /// </returns>
    /// <exception cref="ArgumentException">Control points apply only to <see cref="PrimitiveType.PatchList"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="controlPoints"/> must be in the range 1 to 32, inclusive.</exception>"
    public static PrimitiveType ControlPointCount(this PrimitiveType primitiveType, int controlPoints)
    {
        if (primitiveType != PrimitiveType.PatchList)
            throw new ArgumentException($"Control points apply only to {nameof(PrimitiveType)}.{nameof(PrimitiveType.PatchList)}", nameof(primitiveType));

        const int MIN_CONTROL_POINTS = 1, MAX_CONTROL_POINTS = 32;

        if (controlPoints < MIN_CONTROL_POINTS || controlPoints > MAX_CONTROL_POINTS)
            throw new ArgumentOutOfRangeException(nameof(controlPoints), $"Value must be in between {MIN_CONTROL_POINTS} and {MAX_CONTROL_POINTS}");

        return PrimitiveType.PatchList + controlPoints - 1;
    }
}
