using DotRecast.Detour;
using DotRecast.Recast.Geom;
using DotRecast.Recast.Toolset.Builder;
using DotRecast.Recast.Toolset.Geom;
using DotRecast.Recast.Toolset;
using DotRecast.Recast;

namespace Stride.BepuPhysics.Navigation;
public class StrideNavMeshBuilder
{
	public NavMeshBuildResult Build(IInputGeomProvider geom, RcNavMeshBuildSettings settings)
	{
		return Build(geom, settings.tileSize, RcPartitionType.OfValue(settings.partitioning), settings.cellSize, settings.cellHeight, settings.agentMaxSlope, settings.agentHeight, settings.agentRadius, settings.agentMaxClimb, settings.minRegionSize, settings.mergedRegionSize, settings.edgeMaxLen, settings.edgeMaxError, settings.vertsPerPoly, settings.detailSampleDist, settings.detailSampleMaxError, settings.filterLowHangingObstacles, settings.filterLedgeSpans, settings.filterWalkableLowHeightSpans);
	}

	public NavMeshBuildResult Build(IInputGeomProvider geom, int tileSize, RcPartition partitionType, float cellSize, float cellHeight, float agentMaxSlope, float agentHeight, float agentRadius, float agentMaxClimb, int regionMinSize, int regionMergeSize, float edgeMaxLen, float edgeMaxError, int vertsPerPoly, float detailSampleDist, float detailSampleMaxError, bool filterLowHangingObstacles, bool filterLedgeSpans, bool filterWalkableLowHeightSpans)
	{
		List<RcBuilderResult> list = BuildRecastResult(geom, tileSize, partitionType, cellSize, cellHeight, agentMaxSlope, agentHeight, agentRadius, agentMaxClimb, regionMinSize, regionMergeSize, edgeMaxLen, edgeMaxError, vertsPerPoly, detailSampleDist, detailSampleMaxError, filterLowHangingObstacles, filterLedgeSpans, filterWalkableLowHeightSpans);
		List<DtMeshData> meshData = BuildMeshData(geom, cellSize, cellHeight, agentHeight, agentRadius, agentMaxClimb, list);
		DtNavMesh navMesh = BuildNavMesh(geom, meshData, cellSize, tileSize, vertsPerPoly);
		return new NavMeshBuildResult(list, navMesh);
	}

	public List<RcBuilderResult> BuildRecastResult(IInputGeomProvider geom, int tileSize, RcPartition partitionType, float cellSize, float cellHeight, float agentMaxSlope, float agentHeight, float agentRadius, float agentMaxClimb, int regionMinSize, int regionMergeSize, float edgeMaxLen, float edgeMaxError, int vertsPerPoly, float detailSampleDist, float detailSampleMaxError, bool filterLowHangingObstacles, bool filterLedgeSpans, bool filterWalkableLowHeightSpans)
	{
		RcConfig cfg = new RcConfig(useTiles: true, tileSize, tileSize, RcConfig.CalcBorder(agentRadius, cellSize), partitionType, cellSize, cellHeight, agentMaxSlope, agentHeight, agentRadius, agentMaxClimb, (float)(regionMinSize * regionMinSize) * cellSize * cellSize, (float)(regionMergeSize * regionMergeSize) * cellSize * cellSize, edgeMaxLen, edgeMaxError, vertsPerPoly, detailSampleDist, detailSampleMaxError, filterLowHangingObstacles, filterLedgeSpans, filterWalkableLowHeightSpans, SampleAreaModifications.SAMPLE_AREAMOD_WALKABLE, buildMeshDetail: true);
		return new RcBuilder().BuildTiles(geom, cfg, Task.Factory);
	}

	public DtNavMesh BuildNavMesh(IInputGeomProvider geom, List<DtMeshData> meshData, float cellSize, int tileSize, int vertsPerPoly)
	{
		DtNavMeshParams option = default;
		option.orig = geom.GetMeshBoundsMin();
		option.tileWidth = tileSize * cellSize;
		option.tileHeight = tileSize * cellSize;
		option.maxTiles = GetMaxTiles(geom, cellSize, tileSize);
		option.maxPolys = GetMaxPolysPerTile(geom, cellSize, tileSize);
		DtNavMesh navMesh = new DtNavMesh(option, vertsPerPoly);
		meshData.ForEach(delegate (DtMeshData md)
		{
			navMesh.AddTile(md, 0, 0L);
		});
		return navMesh;
	}

	public List<DtMeshData> BuildMeshData(IInputGeomProvider geom, float cellSize, float cellHeight, float agentHeight, float agentRadius, float agentMaxClimb, IList<RcBuilderResult> results)
	{
		List<DtMeshData> list = new();
		foreach (RcBuilderResult result in results)
		{
			int tileX = result.tileX;
			int tileZ = result.tileZ;
			DtNavMeshCreateParams navMeshCreateParams = DemoNavMeshBuilder.GetNavMeshCreateParams(geom, cellSize, cellHeight, agentHeight, agentRadius, agentMaxClimb, result);
			navMeshCreateParams.tileX = tileX;
			navMeshCreateParams.tileZ = tileZ;
			DtMeshData dtMeshData = DtNavMeshBuilder.CreateNavMeshData(navMeshCreateParams);
			if (dtMeshData != null)
			{
				list.Add(DemoNavMeshBuilder.UpdateAreaAndFlags(dtMeshData));
			}
		}

		return list;
	}

	public int GetMaxTiles(IInputGeomProvider geom, float cellSize, int tileSize)
	{
		int tileBits = GetTileBits(geom, cellSize, tileSize);
		return 1 << tileBits;
	}

	public int GetMaxPolysPerTile(IInputGeomProvider geom, float cellSize, int tileSize)
	{
		int num = 22 - GetTileBits(geom, cellSize, tileSize);
		return 1 << num;
	}

	private int GetTileBits(IInputGeomProvider geom, float cellSize, int tileSize)
	{
		RcCommons.CalcGridSize(geom.GetMeshBoundsMin(), geom.GetMeshBoundsMax(), cellSize, out var sizeX, out var sizeZ);
		int num = (sizeX + tileSize - 1) / tileSize;
		int num2 = (sizeZ + tileSize - 1) / tileSize;
		return Math.Min(DtUtils.Ilog2(DtUtils.NextPow2(num * num2)), 14);
	}

	public int[] GetTiles(DemoInputGeomProvider geom, float cellSize, int tileSize)
	{
		RcCommons.CalcGridSize(geom.GetMeshBoundsMin(), geom.GetMeshBoundsMax(), cellSize, out var sizeX, out var sizeZ);
		int num = (sizeX + tileSize - 1) / tileSize;
		int num2 = (sizeZ + tileSize - 1) / tileSize;
		return [num, num2];
	}
}
