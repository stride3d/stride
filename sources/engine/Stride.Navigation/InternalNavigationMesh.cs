// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using DotRecast.Core.Numerics;
using DotRecast.Detour;
using Stride.Core.Mathematics;

namespace Stride.Navigation;

internal static class Vector3Extensions
{
    internal static RcVec3f ToDotRecastVector(this Vector3 v)
    {
        return new RcVec3f(v.X, v.Y, v.Z);
    }
    internal static Vector3 ToStrideVector(this RcVec3f v)
    {
        return new Vector3(v.X, v.Y, v.Z);
    }
}
internal class InternalNavigationMesh
{
    private readonly DtNavMesh navMesh;
    private readonly DtNavMeshQuery navQuery;
    
    public InternalNavigationMesh(float cellTileSize)
    {
        DtNavMeshParams meshParams = new();
        meshParams.orig = RcVec3f.Zero;
        meshParams.tileWidth = cellTileSize;
        meshParams.tileHeight = cellTileSize;

        // TODO: Link these parameters to the builder
        const int tileBits = 14;
        const int polyBits = 22 - tileBits;
            
        meshParams.maxTiles = 1 << tileBits;
        meshParams.maxPolys = 1 << polyBits;

        // Initialize the query object
        navMesh = new DtNavMesh(meshParams, 2048);
        navQuery = new DtNavMeshQuery(navMesh);
    }

    public bool LoadTile(DtMeshData navData)
    {
        if (navMesh == null || navQuery == null)
            return false;
        if (navData == null)
            return false;

        long tileRef = navMesh.AddTile(navData, 0, 0);
        return tileRef != 0;
    }

    public bool RemoveTile(Point coord)
    {
        long tileRef = navMesh.GetTileRefAt(coord.X, coord.Y, 0);
        var status = navMesh.RemoveTile(tileRef);
        return status != 0;
    }
    
    public void DoPathFindQuery(PathFindQuery query, ref PathFindResult result)
    {
        // Reset result
        result.PathFound = false;

        // Find the starting polygons and point on it to start from
        DtQueryDefaultFilter filter = new DtQueryDefaultFilter();
        DtStatus status = navQuery.FindNearestPoly(query.Source.ToDotRecastVector(), query.FindNearestPolyExtent.ToDotRecastVector(), filter, out long startPoly, out RcVec3f startPoint, out _);
        if (status.Failed())
            return;
        status = navQuery.FindNearestPoly(query.Target.ToDotRecastVector(), query.FindNearestPolyExtent.ToDotRecastVector(), filter, out long endPoly, out RcVec3f endPoint, out _);
        if (status.Failed())
            return;
        
        List<long> polys = new(query.MaxPathPoints);
        status = navQuery.FindPath(startPoly, endPoly, startPoint, endPoint, filter, ref polys, DtFindPathOption.NoOption);
        if (status.Failed() || status.IsPartial())
            return;
        
        status = navQuery.FindStraightPath(startPoint, endPoint, polys, ref result.PathPoints, query.MaxPathPoints, 0);
        if (status.Failed())
            return;
        result.PathFound = true;
    }

    public void DoRaycastQuery(RaycastQuery query, out NavigationRaycastResult result)
    {
        // Reset result
        result = new NavigationRaycastResult { Hit = false };
        DtQueryDefaultFilter filter = new DtQueryDefaultFilter();

        DtStatus status = navQuery.FindNearestPoly(query.Source.ToDotRecastVector(), query.FindNearestPolyExtent.ToDotRecastVector(), filter, out long startPoly, out _, out _);
        if (status.Failed())
            return;
        
        List<long> polys = new (query.MaxPathPoints);
        var normal = result.Normal.ToDotRecastVector();
        status = navQuery.Raycast(startPoly, query.Source.ToDotRecastVector(), 
            query.Target.ToDotRecastVector(), filter, 
            out float t, out normal, ref polys);
        result.Normal = new(normal.X, normal.Y, normal.Z);
        
        if (status.Failed())
            return;
        
        result.Hit = true;
        result.Position = Vector3.Lerp(query.Source, query.Target, t);
    }
}
