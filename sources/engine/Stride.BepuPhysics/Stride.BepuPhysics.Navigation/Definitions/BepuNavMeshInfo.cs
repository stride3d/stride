// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using DotRecast.Core;
using DotRecast.Detour;
using DotRecast.Detour.Dynamic;
using DotRecast.Recast;
using Stride.Core;
using Stride.Core.Mathematics;
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


    public Span<Vector3> GetNavMeshTileVerts()
    {
        if (NavMesh is null) return null;
    
        List<Vector3> verts = [];
    
        for (int i = 0; i < NavMesh.GetMaxTiles(); i++)
        {
            var tile = NavMesh.GetTile(i);
            if (tile?.data != null)
            {
                for (int j = 0; j < tile.data.verts.Length; j += 3)
                {
                    var point = new Vector3(
                        tile.data.verts[j],
                        tile.data.verts[j + 1],
                        tile.data.verts[j + 2]);
                    verts.Add(point);
                }
            }
        }
    
        return CollectionsMarshal.AsSpan(verts);
    }
}
