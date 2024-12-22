// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using DotRecast.Detour;
using DotRecast.Recast.Geom;
using DotRecast.Recast;
using DotRecast.Recast.Toolset.Builder;
using DotRecast.Recast.Toolset;

namespace Stride.DotRecast;
public class NavMeshBuilder
{
    public static DtNavMesh CreateNavMeshFromGeometry(RcNavMeshBuildSettings navSettings, IInputGeomProvider geom, int threads, CancellationToken cancelToken)
    {
        cancelToken.ThrowIfCancellationRequested();

        RcConfig cfg = new(
            useTiles: true,
            navSettings.tileSize,
            navSettings.tileSize,
            RcConfig.CalcBorder(navSettings.agentRadius, navSettings.cellSize),
            RcPartitionType.OfValue(navSettings.partitioning),
            navSettings.cellSize,
            navSettings.cellHeight,
            navSettings.agentMaxSlope,
            navSettings.agentHeight,
            navSettings.agentRadius,
            navSettings.agentMaxClimb,
            (navSettings.minRegionSize * navSettings.minRegionSize) * navSettings.cellSize * navSettings.cellSize,
            (navSettings.mergedRegionSize * navSettings.mergedRegionSize) * navSettings.cellSize * navSettings.cellSize,
            navSettings.edgeMaxLen,
            navSettings.edgeMaxError,
            navSettings.vertsPerPoly,
            navSettings.detailSampleDist,
            navSettings.detailSampleMaxError,
            navSettings.filterLowHangingObstacles,
            navSettings.filterLedgeSpans,
            navSettings.filterWalkableLowHeightSpans,
            SampleAreaModifications.SAMPLE_AREAMOD_WALKABLE,
            buildMeshDetail: true);

        cancelToken.ThrowIfCancellationRequested();

        List<DtMeshData> dtMeshes = [];
        foreach (RcBuilderResult result in new RcBuilder().BuildTiles(geom, cfg, true, false, threads, cancellation: cancelToken))
        {
            DtNavMeshCreateParams navMeshCreateParams = DemoNavMeshBuilder.GetNavMeshCreateParams(geom, navSettings.cellSize, navSettings.cellHeight, navSettings.agentHeight, navSettings.agentRadius, navSettings.agentMaxClimb, result);
            navMeshCreateParams.tileX = result.TileX;
            navMeshCreateParams.tileZ = result.TileZ;
            DtMeshData dtMeshData = DtNavMeshBuilder.CreateNavMeshData(navMeshCreateParams);
            if (dtMeshData != null)
            {
                dtMeshes.Add(DemoNavMeshBuilder.UpdateAreaAndFlags(dtMeshData));
            }

            cancelToken.ThrowIfCancellationRequested();
        }

        cancelToken.ThrowIfCancellationRequested();

        DtNavMeshParams option = default;
        option.orig = geom.GetMeshBoundsMin();
        option.tileWidth = navSettings.tileSize * navSettings.cellSize;
        option.tileHeight = navSettings.tileSize * navSettings.cellSize;
        option.maxTiles = GetMaxTiles(geom, navSettings.cellSize, navSettings.tileSize);
        option.maxPolys = GetMaxPolysPerTile(geom, navSettings.cellSize, navSettings.tileSize);
        DtNavMesh navMesh = new DtNavMesh();
        navMesh.Init(option, navSettings.vertsPerPoly);
        foreach (DtMeshData dtMeshData1 in dtMeshes)
        {
            navMesh.AddTile(dtMeshData1, 0, 0L, out _);
        }

        cancelToken.ThrowIfCancellationRequested();

        return navMesh;
    }

    private static int GetMaxTiles(IInputGeomProvider geom, float cellSize, int tileSize)
    {
        int tileBits = GetTileBits(geom, cellSize, tileSize);
        return 1 << tileBits;
    }

    private static int GetMaxPolysPerTile(IInputGeomProvider geom, float cellSize, int tileSize)
    {
        int num = 22 - GetTileBits(geom, cellSize, tileSize);
        return 1 << num;
    }

    private static int GetTileBits(IInputGeomProvider geom, float cellSize, int tileSize)
    {
        RcRecast.CalcGridSize(geom.GetMeshBoundsMin(), geom.GetMeshBoundsMax(), cellSize, out var sizeX, out var sizeZ);
        int num = (sizeX + tileSize - 1) / tileSize;
        int num2 = (sizeZ + tileSize - 1) / tileSize;
        return Math.Min(DtUtils.Ilog2(DtUtils.NextPow2(num * num2)), 14);
    }
}
