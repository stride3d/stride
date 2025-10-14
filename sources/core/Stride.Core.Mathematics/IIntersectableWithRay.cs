// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Mathematics;

/// <summary>
/// Allows to determine intersections with a <see cref="Ray"/>.
/// </summary>
public interface IIntersectableWithRay
{
    /// <summary>
    /// Determines if there is an intersection between the current object and a <see cref="Ray"/>.
    /// </summary>
    /// <param name="ray">The ray to test.</param>
    /// <returns>Whether the two objects intersected.</returns>
    public bool Intersects(ref readonly Ray ray);

    /// <summary>
    /// Determines if there is an intersection between the current object and a <see cref="Ray"/>.
    /// </summary>
    /// <param name="ray">The ray to test.</param>
    /// <param name="distance">When the method completes, contains the distance of the intersection,
    /// or 0 if there was no intersection.</param>
    /// <returns>Whether the two objects intersected.</returns>
    public bool Intersects(ref readonly Ray ray, out float distance);

    /// <summary>
    /// Determines if there is an intersection between the current object and a <see cref="Ray"/>.
    /// </summary>
    /// <param name="ray">The ray to test.</param>
    /// <param name="point">When the method completes, contains the point of intersection,
    /// or <see cref="Vector3.Zero"/> if there was no intersection.</param>
    /// <returns>Whether the two objects intersected.</returns>
    public bool Intersects(ref readonly Ray ray, out Vector3 point);
}
