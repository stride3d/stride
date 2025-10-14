// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Mathematics;

/// <summary>
/// Allows to determine intersections with a <see cref="Plane"/>.
/// </summary>
public interface IIntersectableWithPlane
{
    /// <summary>
    /// Determines if there is an intersection between the current object and a <see cref="Plane"/>.
    /// </summary>
    /// <param name="plane">The plane to test.</param>
    /// <returns>Whether the two objects intersected.</returns>
    public PlaneIntersectionType Intersects(ref readonly Plane plane);
}
