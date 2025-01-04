// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Concurrent;
using DotRecast.Core;
using DotRecast.Detour;
using DotRecast.Detour.Dynamic;
using DotRecast.Recast;
using Stride.Core;
using Stride.DotRecast.Definitions;

namespace Stride.BepuPhysics.Navigation.Definitions;

[DataContract]
public class BepuNavMeshInfo
{
    public bool IsDynamic { get; set; } = true;

    public BuildSettings BuildSettings { get; set; } = new();

    /// <summary>
    /// Collision masks that will be included in the navigation mesh build.
    /// </summary>
    public CollisionMask CollisionMask { get; set; } = CollisionMask.Everything;

    [DataMemberIgnore]
    public DtNavMesh NavMesh { get; set; }
    [DataMemberIgnore]
    public DtDynamicNavMeshConfig Config;
    [DataMemberIgnore]
    public RcContext Context { get; set; }
    [DataMemberIgnore]
    public RcBuilder Builder { get; set; }
    [DataMemberIgnore]
    public Dictionary<long, DtDynamicTile> Tiles = [];
    [DataMemberIgnore]
    public DtNavMeshParams NavMeshParams;
    [DataMemberIgnore]
    public Dictionary<int, StaticComponent> StaticComponents = [];
    [DataMemberIgnore]
    public BlockingCollection<IDtDaynmicTileJob> UpdateQueue = [];
}
