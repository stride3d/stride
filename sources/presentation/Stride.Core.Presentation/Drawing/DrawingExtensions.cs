// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using Stride.Core.Mathematics;

namespace Stride.Core.Presentation.Drawing;

/// <summary>
/// Conversions between the drawing geometry and the math types of Stride.
/// </summary>
public static class DrawingExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 ToVector2(this DrawingPoint point) => new((float)point.X, (float)point.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 ToVector2(this DrawingVector vector) => new((float)vector.X, (float)vector.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DrawingPoint ToDrawingPoint(this Vector2 point) => new(point.X, point.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DrawingVector ToDrawingVector(this Vector2 vector) => new(vector.X, vector.Y);
}
