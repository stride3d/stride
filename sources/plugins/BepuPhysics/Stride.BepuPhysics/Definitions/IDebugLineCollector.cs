// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Definitions;

/// <summary>
/// Receives the lines an <see cref="IDebugLineSource"/> draws in the debug view.
/// </summary>
public interface IDebugLineCollector
{
    /// <summary>
    /// Adds a segment from <paramref name="start"/> to <paramref name="end"/>.
    /// </summary>
    /// <param name="start">The world-space start of the segment.</param>
    /// <param name="end">The world-space end of the segment.</param>
    /// <param name="color">The color of the segment.</param>
    void AddLine(Vector3 start, Vector3 end, Color color);
}
