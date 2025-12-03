// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using DotRecast.Core;
using DotRecast.Core.Numerics;
using DotRecast.Detour;
using DotRecast.Recast;
using Stride.Core.Mathematics;

namespace Stride.Navigation;

internal class NavigationBuilder(BuildSettings buildSettings)
{
    private BuildSettings buildSettings = buildSettings;
    private readonly GeneratedData result = new();
    private readonly RcContext context = new();
    private int[] triAreas;
    private RcHeightfield solid;
    private RcCompactHeightfield chf;
    private RcContourSet contourSet;
    private RcPolyMesh polyMesh;
    private RcPolyMeshDetail meshDetail;
    private DtMeshData navmeshData;
    
    public GeneratedData BuildNavmesh(ref Vector3[] vertices, ref int[] indices)
    {
        GeneratedData ret = result;
        ret.Success = false;

        RcVec3f bmin = new (buildSettings.BoundingBox.Minimum.X, buildSettings.BoundingBox.Minimum.Y, buildSettings.BoundingBox.Minimum.Z);
        RcVec3f bmax = new (buildSettings.BoundingBox.Maximum.X, buildSettings.BoundingBox.Maximum.Y, buildSettings.BoundingBox.Maximum.Z);

        RcVec3f bbSize = bmax - bmin;

        if (bbSize.X <= 0.0f || bbSize.Y <= 0.0f || bbSize.Z <= 0.0f)
            return ret; // Negative or empty bounding box
        
        if (buildSettings.DetailSampleDist < 1.0f)
            return ret;
        if (buildSettings.DetailSampleMaxError <= 0.0f)
            return ret;
        if (buildSettings.EdgeMaxError < 0.1f)
            return ret;
        if (buildSettings.EdgeMaxLen < 0.0f)
            return ret;
        if (buildSettings.RegionMinArea < 0.0f)
            return ret;
        if (buildSettings.RegionMergeArea < 0.0f)
            return ret;
        if (buildSettings.TileSize <= 0)
            return ret;
        
        if (buildSettings.CellSize < 0.01f)
            buildSettings.CellSize = 0.01f;
        if (buildSettings.CellHeight < 0.01f)
            buildSettings.CellHeight = 0.01f;

        int maxEdgeLen = (int)(buildSettings.EdgeMaxLen / buildSettings.CellSize);
        float maxSimplificationError = buildSettings.EdgeMaxError;
        const int maxVertsPerPoly = 6;
        float detailSampleDist = buildSettings.CellSize * buildSettings.DetailSampleDist;
        float detailSampleMaxError = buildSettings.CellHeight * buildSettings.DetailSampleMaxError;

        int walkableHeight = (int)MathF.Ceiling(buildSettings.AgentHeight / buildSettings.CellHeight);
        int walkableClimb = (int)MathF.Floor(buildSettings.AgentMaxClimb / buildSettings.CellHeight);
        int walkableRadius = (int)MathF.Ceiling(buildSettings.AgentRadius / buildSettings.CellSize);

        // Size of the tile border
        int borderSize = walkableRadius + 3;
        int tileSize = buildSettings.TileSize;

        // Expand bounding box by border size so that all required geometry is included
        bmin.X -= borderSize * buildSettings.CellSize;
        bmin.Z -= borderSize * buildSettings.CellSize;
        bmax.X += borderSize * buildSettings.CellSize;
        bmax.Z += borderSize * buildSettings.CellSize;

        int width = tileSize + borderSize * 2;
        int height = tileSize + borderSize * 2;
        
        if (vertices?.Length == 0 || indices?.Length == 0 || walkableClimb < 0)
            return ret;

        solid = new RcHeightfield(width, height, bmin, bmax, buildSettings.CellSize, buildSettings.CellHeight, borderSize);
        
        int numTriangles = indices!.Length / 3;

        float[] verts = new float[vertices!.Length * 3];
        for (int i = 0; i < vertices!.Length; i++)
        {
            verts[i * 3 + 0] = vertices[i].X;
            verts[i * 3 + 1] = vertices[i].Y;
            verts[i * 3 + 2] = vertices[i].Z;
        }
        
        // Find walkable triangles and rasterize into heightfield
        triAreas = RcCommons.MarkWalkableTriangles(context, buildSettings.AgentMaxSlope, verts, indices, numTriangles, new RcAreaModification(RcAreaModification.RC_AREA_FLAGS_MASK));
        RcRasterizations.RasterizeTriangles(context,  verts, indices, triAreas, numTriangles, solid, walkableClimb);
        
        // Filter walkable surfaces.
        RcFilters.FilterLowHangingWalkableObstacles(context, walkableClimb, solid);
        RcFilters.FilterLedgeSpans(context, walkableHeight, walkableClimb, solid);
        RcFilters.FilterWalkableLowHeightSpans(context, walkableHeight, solid);
        
        // Compact the heightfield so that it is faster to handle from now on.
        // This will result more cache coherent data as well as the neighbours
        // between walkable cells will be calculated.
        chf = RcCompacts.BuildCompactHeightfield(context, walkableHeight, walkableClimb, solid);

        // Erode the walkable area by agent radius.
        RcAreas.ErodeWalkableArea(context, walkableRadius, chf);
            
        // Prepare for region partitioning, by calculating distance field along the walkable surface.
        RcRegions.BuildDistanceField(context, chf);
        
        // Partition the walkable surface into simple regions without holes.
        RcRegions.BuildRegions(context, chf, buildSettings.RegionMinArea, buildSettings.RegionMergeArea);

        // Create contours.
        contourSet = RcContours.BuildContours(context, chf, maxSimplificationError, maxEdgeLen, RcBuildContoursFlags.RC_CONTOUR_TESS_WALL_EDGES);

        // Build polygon navmesh from the contours.
        polyMesh = RcMeshs.BuildPolyMesh(context, contourSet, maxVertsPerPoly);

        meshDetail = RcMeshDetails.BuildPolyMeshDetail(context, polyMesh, chf, detailSampleDist, detailSampleMaxError);

        // Update poly flags from areas.
        for (int i = 0; i < polyMesh.npolys; ++i)
        {
            if (polyMesh.areas[i] ==  RcConstants.RC_WALKABLE_AREA)
                polyMesh.areas[i] = 0;

            if (polyMesh.areas[i] == 0)
            {
                polyMesh.flags[i] = 1;
            }
        }

        // Generate native navmesh format and store the data pointers in the return structure
        if (!CreateDetourMesh())
            return ret;
        ret.NavmeshData = navmeshData;
        ret.Success = true;

        return ret;
    }

    private bool CreateDetourMesh()
    {
        DtNavMeshCreateParams createParams = new DtNavMeshCreateParams();
        createParams.verts = polyMesh.verts;
        createParams.vertCount = polyMesh.nverts;
        createParams.polys = polyMesh.polys;
        createParams.polyAreas = polyMesh.areas;
        createParams.polyFlags = polyMesh.flags;
        createParams.polyCount = polyMesh.npolys;
        createParams.nvp = polyMesh.nvp;
        createParams.detailMeshes = meshDetail?.meshes;
        createParams.detailVerts = meshDetail?.verts;
        if (meshDetail != null)
        {
            createParams.detailVertsCount = meshDetail.nverts;
            createParams.detailTris = meshDetail.tris;
            createParams.detailTriCount = meshDetail.ntris;
        }

        // TODO: Support off-mesh connections
        createParams.offMeshConVerts = null;
        createParams.offMeshConRad = null;
        createParams.offMeshConDir = null;
        createParams.offMeshConAreas = null;
        createParams.offMeshConFlags = null;
        createParams.offMeshConUserID = null;
        createParams.offMeshConCount = 0;
        createParams.walkableHeight = buildSettings.AgentHeight;
        createParams.walkableClimb = buildSettings.AgentMaxClimb;
        createParams.walkableRadius = buildSettings.AgentRadius;
        createParams.bmin = polyMesh.bmin;
        createParams.bmax = polyMesh.bmax;
        createParams.cs = buildSettings.CellSize;
        createParams.ch = buildSettings.CellHeight;
        createParams.buildBvTree = true;
        createParams.tileX = buildSettings.TilePosition.X;
        createParams.tileZ = buildSettings.TilePosition.Y;

        navmeshData = DtNavMeshBuilder.CreateNavMeshData(createParams);
        
        return navmeshData != null;
    }
}
