using System.Runtime.InteropServices;
using DotRecast.Detour;
using DotRecast.Recast;
using DotRecast.Recast.Geom;
using DotRecast.Recast.Toolset;
using DotRecast.Recast.Toolset.Builder;
using DotRecast.Recast.Toolset.Geom;
using Stride.BepuPhysics.Navigation.Components;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Core;
using Stride.Input;

namespace Stride.BepuPhysics.Navigation.Processors;
public class RecastMeshProcessor : EntityProcessor<BepuNavigationBoundingBoxComponent>
{
	private RcNavMeshBuildSettings _navSettings = new();
	private DtNavMesh? _navMesh;
	private List<BepuNavigationBoundingBoxComponent> _boundingBoxes = new();
	private SceneSystem _sceneSystem;
	private InputManager _input;
	private BepuStaticColliderProcessor _colliderProcessor = new();
	private CancellationTokenSource _rebuildingTask = new();
	private Task<DtNavMesh>? _runningRebuild;

	public RecastMeshProcessor()
	{
		// this is done to ensure that this processor runs after the BepuPhysicsProcessors
		Order = 20000;
	}

	protected override void OnSystemAdd()
	{
		base.OnSystemAdd();
		_sceneSystem = Services.GetService<SceneSystem>();
		_input = Services.GetSafeServiceAs<InputManager>();
		_sceneSystem.SceneInstance.Processors.Add(_colliderProcessor);
	}

	protected override void OnEntityComponentAdding(Entity entity, [NotNull] BepuNavigationBoundingBoxComponent component, [NotNull] BepuNavigationBoundingBoxComponent data)
	{
		_boundingBoxes.Add(data);
	}

	protected override void OnEntityComponentRemoved(Entity entity, [NotNull] BepuNavigationBoundingBoxComponent component, [NotNull] BepuNavigationBoundingBoxComponent data)
	{
		_boundingBoxes.Remove(data);
	}

	public override void Update(GameTime time)
	{
		if (_runningRebuild?.Status == TaskStatus.RanToCompletion)
		{
			_navMesh = _runningRebuild.Result;
			_runningRebuild = null;
		}

#warning Remove debug logic
		if (_input.IsKeyPressed(Keys.Space))
		{
			RebuildNavMesh();
		}
	}

	public Task RebuildNavMesh()
	{
		// Cancel any ongoing rebuild
		_rebuildingTask.Cancel();
		_rebuildingTask = new CancellationTokenSource();

		// Fetch mesh data from the scene - this may be too slow
		// There are a couple of avenues we could go down into to fix this but none of them are easy
		// Something we'll have to investigate later.
		var points = new List<Vector3>();
		var indices = new List<int>();
		foreach(var shape in _colliderProcessor.BodyShapes)
		{
			// Copy vertices
			int vBase = points.Count;
			points.AddRange(shape.Value.Points);

			// Copy indices with offset applied
			indices.Capacity += shape.Value.Indices.Count;
			foreach (int index in shape.Value.Indices)
			{
				indices.Add(index + vBase);
			}
		}

		var settingsCopy = new RcNavMeshBuildSettings
		{
			cellSize = _navSettings.cellSize,
			cellHeight = _navSettings.cellHeight,
			agentHeight = _navSettings.agentHeight,
			agentRadius = _navSettings.agentRadius,
			agentMaxClimb = _navSettings.agentMaxClimb,
			agentMaxSlope = _navSettings.agentMaxSlope,
			agentMaxAcceleration = _navSettings.agentMaxAcceleration,
			agentMaxSpeed = _navSettings.agentMaxSpeed,
			minRegionSize = _navSettings.minRegionSize,
			mergedRegionSize = _navSettings.mergedRegionSize,
			partitioning = _navSettings.partitioning,
			filterLowHangingObstacles = _navSettings.filterLowHangingObstacles,
			filterLedgeSpans = _navSettings.filterLedgeSpans,
			filterWalkableLowHeightSpans = _navSettings.filterWalkableLowHeightSpans,
			edgeMaxLen = _navSettings.edgeMaxLen,
			edgeMaxError = _navSettings.edgeMaxError,
			vertsPerPoly = _navSettings.vertsPerPoly,
			detailSampleDist = _navSettings.detailSampleDist,
			detailSampleMaxError = _navSettings.detailSampleMaxError,
			tiled = _navSettings.tiled,
			tileSize = _navSettings.tileSize,
		};
		var token = _rebuildingTask.Token;
		var task = Task.Run(() => _navMesh = CreateNavMesh(settingsCopy, points, indices, token), token);
		_runningRebuild = task;
		return task;
	}

	private static DtNavMesh CreateNavMesh(RcNavMeshBuildSettings _navSettings, List<Vector3> Points, List<int> Indices, CancellationToken cancelToken)
	{
		// Get the backing array of this list,
		// get a span to that backing array,
		var spanToPoints = CollectionsMarshal.AsSpan(Points);
		// cast the type of span to read it as if it was a series of contiguous floats instead of contiguous vectors
		var reinterpretedPoints = MemoryMarshal.Cast<Vector3, float>(spanToPoints);
		StrideGeomProvider geom = new StrideGeomProvider(reinterpretedPoints.ToArray(), Indices.ToArray());

		cancelToken.ThrowIfCancellationRequested();

		RcPartition partitionType = RcPartitionType.OfValue(_navSettings.partitioning);
		RcConfig cfg = new RcConfig(
			useTiles: true,
			_navSettings.tileSize,
			_navSettings.tileSize,
			RcConfig.CalcBorder(_navSettings.agentRadius, _navSettings.cellSize),
			partitionType,
			_navSettings.cellSize,
			_navSettings.cellHeight,
			_navSettings.agentMaxSlope,
			_navSettings.agentHeight,
			_navSettings.agentRadius,
			_navSettings.agentMaxClimb,
			(_navSettings.minRegionSize * _navSettings.minRegionSize) * _navSettings.cellSize * _navSettings.cellSize,
			(_navSettings.mergedRegionSize * _navSettings.mergedRegionSize) * _navSettings.cellSize * _navSettings.cellSize,
			_navSettings.edgeMaxLen,
			_navSettings.edgeMaxError,
			_navSettings.vertsPerPoly,
			_navSettings.detailSampleDist,
			_navSettings.detailSampleMaxError,
			_navSettings.filterLowHangingObstacles,
			_navSettings.filterLedgeSpans,
			_navSettings.filterWalkableLowHeightSpans,
			SampleAreaModifications.SAMPLE_AREAMOD_WALKABLE,
			buildMeshDetail: true);

		cancelToken.ThrowIfCancellationRequested();

		List<DtMeshData> dtMeshes = new();
		foreach (RcBuilderResult result in new RcBuilder().BuildTiles(geom, cfg, Task.Factory))
		{
			DtNavMeshCreateParams navMeshCreateParams = DemoNavMeshBuilder.GetNavMeshCreateParams(geom, _navSettings.cellSize, _navSettings.cellHeight, _navSettings.agentHeight, _navSettings.agentRadius, _navSettings.agentMaxClimb, result);
			navMeshCreateParams.tileX = result.tileX;
			navMeshCreateParams.tileZ = result.tileZ;
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
		option.tileWidth = _navSettings.tileSize * _navSettings.cellSize;
		option.tileHeight = _navSettings.tileSize * _navSettings.cellSize;
		option.maxTiles = GetMaxTiles(geom, _navSettings.cellSize, _navSettings.tileSize);
		option.maxPolys = GetMaxPolysPerTile(geom, _navSettings.cellSize, _navSettings.tileSize);
		DtNavMesh navMesh = new DtNavMesh(option, _navSettings.vertsPerPoly);
		foreach (DtMeshData dtMeshData1 in dtMeshes)
		{
			navMesh.AddTile(dtMeshData1, 0, 0L);
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
		RcCommons.CalcGridSize(geom.GetMeshBoundsMin(), geom.GetMeshBoundsMax(), cellSize, out var sizeX, out var sizeZ);
		int num = (sizeX + tileSize - 1) / tileSize;
		int num2 = (sizeZ + tileSize - 1) / tileSize;
		return Math.Min(DtUtils.Ilog2(DtUtils.NextPow2(num * num2)), 14);
	}

	private static int[] GetTiles(DemoInputGeomProvider geom, float cellSize, int tileSize)
	{
		RcCommons.CalcGridSize(geom.GetMeshBoundsMin(), geom.GetMeshBoundsMax(), cellSize, out var sizeX, out var sizeZ);
		int num = (sizeX + tileSize - 1) / tileSize;
		int num2 = (sizeZ + tileSize - 1) / tileSize;
		return [num, num2];
	}
}
