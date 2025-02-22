// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.BepuPhysics.Navigation.Definitions;
[DataContract()]
[Display("Pathfinding Settings")]
public class PathfindingSettings
{
    public int MaxAllowedVisitedTiles { get; set; } = 16;
}
