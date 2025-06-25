// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics;

public static class PrimitiveTypeExtensions
{
    public static PrimitiveType ControlPointCount(this PrimitiveType primitiveType, int controlPoints)
    {
        /// <summary>
        /// Interpret the vertex data as a patch list.
        /// </summary>
        /// <param name="controlPoints">Number of control points. Value must be in the range 1 to 32.</param>
        if (primitiveType != PrimitiveType.PatchList)
            throw new ArgumentException($"Control points apply only to {nameof(PrimitiveType)}.{nameof(PrimitiveType.PatchList)}", nameof(primitiveType));

        if (controlPoints < 1 || controlPoints > 32)
            throw new ArgumentOutOfRangeException(nameof(controlPoints), "Value must be in between 1 and 32");

        return PrimitiveType.PatchList + controlPoints - 1;
    }
}
