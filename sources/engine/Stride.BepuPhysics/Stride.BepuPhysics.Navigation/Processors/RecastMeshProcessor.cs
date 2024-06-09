using System.Runtime.InteropServices;
using DotRecast.Detour;
using DotRecast.Recast;
using DotRecast.Recast.Geom;
using DotRecast.Recast.Toolset;
using DotRecast.Recast.Toolset.Builder;
using DotRecast.Core.Numerics;
using Stride.BepuPhysics.Definitions;
using Stride.Core.Annotations;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using Stride.Core.Mathematics;
using Stride.BepuPhysics.Systems;
using Stride.Core;
using System.Diagnostics;
using Stride.BepuPhysics.Navigation.Extensions;
using Stride.BepuPhysics.Navigation.Definitions;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Navigation.Processors;
public class RecastMeshProcessor : GameSystemBase
{

    public TimeSpan LastShapeCacheTime { get; private set; }

    public const int MaxPolys = 256;
    public const int MaxSmooth = 2048;

    private readonly RcVec3f polyPickExt = new(2, 4, 2);

    private readonly Stopwatch _stopwatch = new();
    private readonly SceneSystem _sceneSystem;
    private readonly InputManager _input;
    private readonly ShapeCacheSystem _shapeCache;
    private ContainerProcessor _containerProcessor;

    private DtNavMesh? _navMesh;
    private Task<DtNavMesh>? _runningRebuild;

    private CancellationTokenSource _rebuildingTask = new();
    private RecastNavigationConfiguration _navSettings = new();

    private DtNavMeshQuery _dtNavMeshQuery;

    public RecastMeshProcessor([NotNull] IServiceRegistry registry) : base(registry)
    {
        UpdateOrder = 20000;
        Enabled = true; //enabled by default

        registry.AddService(this);

        _sceneSystem = registry.GetService<SceneSystem>();
        _input = registry.GetService<InputManager>();
        _shapeCache = registry.GetService<ShapeCacheSystem>();

        var gameSettings = registry.GetService<IGameSettingsService>();
        _navSettings = gameSettings.Settings.Configurations.Get<RecastNavigationConfiguration>();
        _navSettings ??= new();
    }

    public override void Update(GameTime time)
    {
        _containerProcessor ??= _sceneSystem.SceneInstance.Processors.Get<ContainerProcessor>();

        if (_runningRebuild?.Status == TaskStatus.RanToCompletion)
        {
            _navMesh = _runningRebuild.Result;
            _runningRebuild = null;
        }
    }

    public Task RebuildNavMesh()
    {
        // The goal of this method is to do the strict minimum here on the main thread, gathering data for the async thread to do the rest on its own

        // Cancel any ongoing rebuild
        _rebuildingTask.Cancel();
        _rebuildingTask = new CancellationTokenSource();

        _stopwatch.Start();

        // Fetch mesh data from the scene - this may be too slow
        // There are a couple of avenues we could go down into to fix this but none of them are easy
        // Something we'll have to investigate later.
        var asyncInput = new AsyncInput();
        for (var e = _containerProcessor.ComponentDataEnumerator; e.MoveNext();)
        {
            var container = e.Current.Value;

#warning should we really ignore all bodies ?
            if (container is BodyComponent)
                continue;

            // No need to store cache, nav mesh recompute should be rare enough were it would waste more memory than necessary
            container.Collider.AppendModel(asyncInput.shapeData, _shapeCache, out object? cache);
            var shapeCount = container.Collider.Transforms;
            for (int i = shapeCount - 1; i >= 0; i--)
                asyncInput.transformsOut.Add(default);
            container.Collider.GetLocalTransforms(container, CollectionsMarshal.AsSpan(asyncInput.transformsOut)[^shapeCount..]);
            asyncInput.matrices.Add((container.Entity.Transform.WorldMatrix, shapeCount));
        }

        _stopwatch.Stop();
        LastShapeCacheTime = _stopwatch.Elapsed;
        _stopwatch.Reset();

        var settingsCopy = new RcNavMeshBuildSettings
        {
            cellSize = _navSettings.BuildSettings.cellSize,
            cellHeight = _navSettings.BuildSettings.cellHeight,
            agentHeight = _navSettings.BuildSettings.agentHeight,
            agentRadius = _navSettings.BuildSettings.agentRadius,
            agentMaxClimb = _navSettings.BuildSettings.agentMaxClimb,
            agentMaxSlope = _navSettings.BuildSettings.agentMaxSlope,
            agentMaxAcceleration = _navSettings.BuildSettings.agentMaxAcceleration,
            //agentMaxSpeed = _navSettings.BuildSettings.agentMaxSpeed,
            minRegionSize = _navSettings.BuildSettings.minRegionSize,
            mergedRegionSize = _navSettings.BuildSettings.mergedRegionSize,
            partitioning = _navSettings.BuildSettings.partitioning,
            filterLowHangingObstacles = _navSettings.BuildSettings.filterLowHangingObstacles,
            filterLedgeSpans = _navSettings.BuildSettings.filterLedgeSpans,
            filterWalkableLowHeightSpans = _navSettings.BuildSettings.filterWalkableLowHeightSpans,
            edgeMaxLen = _navSettings.BuildSettings.edgeMaxLen,
            edgeMaxError = _navSettings.BuildSettings.edgeMaxError,
            vertsPerPoly = _navSettings.BuildSettings.vertsPerPoly,
            detailSampleDist = _navSettings.BuildSettings.detailSampleDist,
            detailSampleMaxError = _navSettings.BuildSettings.detailSampleMaxError,
            tiled = _navSettings.BuildSettings.tiled,
            tileSize = _navSettings.BuildSettings.tileSize,
        };
        var token = _rebuildingTask.Token;
        var task = Task.Run(() => _navMesh = CreateNavMesh(settingsCopy, asyncInput, _navSettings.UsableThreadCount, token), token);
        _runningRebuild = task;
        return task;
    }

    private static DtNavMesh CreateNavMesh(RcNavMeshBuildSettings _navSettings, AsyncInput input, int threads, CancellationToken cancelToken)
    {
        // /!\ THIS IS NOT RUNNING ON THE MAIN THREAD /!\

        var verts = new List<VertexPosition3>();
        var indices = new List<int>();
        for (int containerI = 0, shapeI = 0; containerI < input.matrices.Count; containerI++)
        {
            var (containerMatrix, shapeCount) = input.matrices[containerI];
            containerMatrix.Decompose(out _, out Matrix worldMatrix, out var translation);
            worldMatrix.TranslationVector = translation;

            for (int j = 0; j < shapeCount; j++, shapeI++)
            {
                var transform = input.transformsOut[shapeI];
                Matrix.Transformation(ref transform.Scale, ref transform.RotationLocal, ref transform.PositionLocal, out var localMatrix);
                var finalMatrix = localMatrix * worldMatrix;

                var shape = input.shapeData[shapeI];
                verts.EnsureCapacity(verts.Count + shape.Vertices.Length);
                indices.EnsureCapacity(indices.Count + shape.Indices.Length);

                int vertexBufferStart = verts.Count;

                for (int i = 0; i < shape.Indices.Length; i += 3)
                {
                    var index0 = shape.Indices[i];
                    var index1 = shape.Indices[i + 1];
                    var index2 = shape.Indices[i + 2];
                    indices.Add(vertexBufferStart + index0);
                    indices.Add(vertexBufferStart + index2);
                    indices.Add(vertexBufferStart + index1);
                }

                //foreach (int index in shape.Indices)
                //    indices.Add(vertexBufferStart + index);

                for (int l = 0; l < shape.Vertices.Length; l++)
                {
                    var vertex = shape.Vertices[l].Position;
                    Vector3.Transform(ref vertex, ref finalMatrix, out Vector3 transformedVertex);
                    verts.Add(new(transformedVertex));
                }
            }
        }

        // Get the backing array of this list,
        // get a span to that backing array,
        var spanToPoints = CollectionsMarshal.AsSpan(verts);
        // cast the type of span to read it as if it was a series of contiguous floats instead of contiguous vectors
        var reinterpretedPoints = MemoryMarshal.Cast<VertexPosition3, float>(spanToPoints);
        StrideGeomProvider geom = new(reinterpretedPoints.ToArray(), [.. indices]);

        cancelToken.ThrowIfCancellationRequested();

        RcPartition partitionType = RcPartitionType.OfValue(_navSettings.partitioning);
        RcConfig cfg = new(
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

        List<DtMeshData> dtMeshes = [];
        foreach (RcBuilderResult result in new RcBuilder().BuildTiles(geom, cfg, true, false, threads, cancellation: cancelToken))
        {
            DtNavMeshCreateParams navMeshCreateParams = DemoNavMeshBuilder.GetNavMeshCreateParams(geom, _navSettings.cellSize, _navSettings.cellHeight, _navSettings.agentHeight, _navSettings.agentRadius, _navSettings.agentMaxClimb, result);
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
        option.tileWidth = _navSettings.tileSize * _navSettings.cellSize;
        option.tileHeight = _navSettings.tileSize * _navSettings.cellSize;
        option.maxTiles = GetMaxTiles(geom, _navSettings.cellSize, _navSettings.tileSize);
        option.maxPolys = GetMaxPolysPerTile(geom, _navSettings.cellSize, _navSettings.tileSize);
        DtNavMesh navMesh = new(option, _navSettings.vertsPerPoly);
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

    public bool TryFindPath(Vector3 start, Vector3 end, ref List<long> polys, ref List<Vector3> smoothPath)
    {
        if (_navMesh is null) return false;

        var queryFilter = new DtQueryDefaultFilter();
        _dtNavMeshQuery = new DtNavMeshQuery(_navMesh);

        _dtNavMeshQuery.FindNearestPoly(start.ToDotRecastVector(), polyPickExt, queryFilter, out var startRef, out var _, out var _);

        _dtNavMeshQuery.FindNearestPoly(end.ToDotRecastVector(), polyPickExt, queryFilter, out var endRef, out var _, out var _);
        // find the nearest point on the navmesh to the start and end points
        var result = FindFollowPath(_dtNavMeshQuery, startRef, endRef, start.ToDotRecastVector(), end.ToDotRecastVector(), queryFilter, true, ref polys, ref smoothPath);

        return result.Succeeded();
    }

    public List<Vector3>? GetNavMeshTiles()
    {
        if (_navMesh is null) return null;

        List<Vector3> verts = [];

        for (int i = 0; i < _navMesh.GetMaxTiles(); i++)
        {
            var tile = _navMesh.GetTile(i);
            if (tile != null && tile.data != null)
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

        return verts;
    }

    public static DtStatus FindFollowPath(DtNavMeshQuery navQuery, long startRef, long endRef, RcVec3f startPt, RcVec3f endPt, IDtQueryFilter filter, bool enableRaycast, ref List<long> polys, ref List<Vector3> smoothPath)
    {
        if (startRef == 0 || endRef == 0)
        {
            polys?.Clear();
            smoothPath?.Clear();

            return DtStatus.DT_FAILURE;
        }

        polys ??= [];
        smoothPath ??= [];

        polys.Clear();
        smoothPath.Clear();

        var opt = new DtFindPathOption(enableRaycast ? DtFindPathOptions.DT_FINDPATH_ANY_ANGLE : 0, float.MaxValue);
        navQuery.FindPath(startRef, endRef, startPt, endPt, filter, ref polys, opt);
        if (0 >= polys.Count)
            return DtStatus.DT_FAILURE;

        // Iterate over the path to find smooth path on the detail mesh surface.
        navQuery.ClosestPointOnPoly(startRef, startPt, out var iterPos, out _);
        navQuery.ClosestPointOnPoly(polys[polys.Count - 1], endPt, out var targetPos, out _);

        float STEP_SIZE = 0.5f;
        float SLOP = 0.01f;

        smoothPath.Clear();
        smoothPath.Add(iterPos.ToStrideVector());
        var visited = new List<long>();

        // Move towards target a small advancement at a time until target reached or
        // when ran out of memory to store the path.
        while (0 < polys.Count && smoothPath.Count < MaxSmooth)
        {
            // Find location to steer towards.
            if (!DtPathUtils.GetSteerTarget(navQuery, iterPos, targetPos, SLOP,
                    polys, out var steerPos, out var steerPosFlag, out _))
            {
                break;
            }

            bool endOfPath = (steerPosFlag & DtStraightPathFlags.DT_STRAIGHTPATH_END) != 0;
            bool offMeshConnection = (steerPosFlag & DtStraightPathFlags.DT_STRAIGHTPATH_OFFMESH_CONNECTION) != 0;

            // Find movement delta.
            RcVec3f delta = RcVec3f.Subtract(steerPos, iterPos);
            float len = MathF.Sqrt(RcVec3f.Dot(delta, delta));
            // If the steer target is end of path or off-mesh link, do not move past the location.
            if ((endOfPath || offMeshConnection) && len < STEP_SIZE)
            {
                len = 1;
            }
            else
            {
                len = STEP_SIZE / len;
            }

            RcVec3f moveTgt = RcVecUtils.Mad(iterPos, delta, len);

            // Move
            navQuery.MoveAlongSurface(polys[0], iterPos, moveTgt, filter, out var result, ref visited);

            iterPos = result;

            polys = DtPathUtils.MergeCorridorStartMoved(polys, 0, 256, visited);
            polys = DtPathUtils.FixupShortcuts(polys, navQuery);

            var status = navQuery.GetPolyHeight(polys[0], result, out var h);
            if (status.Succeeded())
            {
                iterPos.Y = h;
            }

            // Handle end of path and off-mesh links when close enough.
            //if (endOfPath && DtPathUtils.InRange(iterPos, steerPos, SLOP, 1.0f))
            //{
            //	// Reached end of path.
            //	iterPos = targetPos;
            //	if (smoothPath.Count < MaxSmooth)
            //	{
            //		smoothPath.Add(iterPos.ToStrideVector());
            //	}
            //
            //	break;
            //}
            //else if (offMeshConnection && DtPathUtils.InRange(iterPos, steerPos, SLOP, 1.0f))
            //{
            //	// Reached off-mesh connection.
            //	RcVec3f startPos = RcVec3f.Zero;
            //	RcVec3f endPos = RcVec3f.Zero;
            //
            //	// Advance the path up to and over the off-mesh connection.
            //	long prevRef = 0;
            //	long polyRef = polys[0];
            //	int npos = 0;
            //	while (npos < polys.Count && polyRef != steerPosRef)
            //	{
            //		prevRef = polyRef;
            //		polyRef = polys[npos];
            //		npos++;
            //	}
            //
            //	polys = polys.GetRange(npos, polys.Count - npos);
            //
            //	// Handle the connection.
            //	var status2 = navMesh.GetOffMeshConnectionPolyEndPoints(prevRef, polyRef, ref startPos, ref endPos);
            //	if (status2.Succeeded())
            //	{
            //		if (smoothPath.Count < MaxSmooth)
            //		{
            //			smoothPath.Add(startPos.ToStrideVector());
            //			// Hack to make the dotted path not visible during off-mesh connection.
            //			if ((smoothPath.Count & 1) != 0)
            //			{
            //				smoothPath.Add(startPos.ToStrideVector());
            //			}
            //		}
            //
            //		// Move position at the other side of the off-mesh link.
            //		iterPos = endPos;
            //		navQuery.GetPolyHeight(polys[0], iterPos, out var eh);
            //		iterPos.Y = eh;
            //	}
            //}

            // Store results.
            if (smoothPath.Count < MaxSmooth)
            {
                smoothPath.Add(iterPos.ToStrideVector());
            }
        }

        return DtStatus.DT_SUCCESS;
    }

    class AsyncInput
    {
        public List<BasicMeshBuffers> shapeData = [];
        public List<ShapeTransform> transformsOut = [];
        public List<(Matrix entity, int count)> matrices = [];
    }

}
