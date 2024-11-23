// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.DotRecast.Definitions;
[DataContract()]
[Display("Pathfinding Settings")]
public class PathfindingSettings
{
    /// <summary>
    /// Max amount of visited tiles to search through at once.
    /// </summary>
    public int MaxAllowedVisitedTiles { get; set; } = 16;

    /// <summary>
    /// Max amount of smoothing to apply to the path.
    /// </summary>
    public int MaxSmoothing { get; set; } = 2048;

    /// <summary>
    /// Max amount of polygons to add for pathfinding.
    /// </summary>
    public int MaxPolys { get; set; } = 256;
}
