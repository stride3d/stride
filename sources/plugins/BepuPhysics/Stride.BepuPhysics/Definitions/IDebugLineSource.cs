// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.BepuPhysics.Definitions;

/// <summary>
/// Implemented by a <see cref="CollidableComponent"/> to draw lines of its own in the debug view, besides the wireframe of its collider.
/// </summary>
public interface IDebugLineSource
{
    /// <summary>
    /// Adds the lines this collidable shows for the current frame.
    /// </summary>
    /// <param name="lines">Receives the lines, in world space.</param>
    void AddDebugLines(IDebugLineCollector lines);
}
