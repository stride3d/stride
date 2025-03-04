// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using DotRecast.Detour;
using DotRecast.Recast.Toolset;
using DotRecast.Core.Numerics;
using Stride.Engine;
using Stride.Games;
using Stride.Core.Mathematics;
using Stride.BepuPhysics.Systems;
using Stride.Core;
using System.Diagnostics;
using Stride.BepuPhysics.Navigation.Definitions;
using Stride.Engine.Design;
using Stride.BepuPhysics.Navigation.GenericBuilder;
using Stride.DotRecast.Extensions;
using Stride.DotRecast.Definitions;
using DotRecast.Core;
using DotRecast.Detour.Dynamic.Colliders;
using DotRecast.Detour.Dynamic;
using Stride.BepuPhysics.Definitions;
using DotRecast.Recast;
using DotRecast.Detour.Dynamic.Io;
using DotRecast.Recast.Toolset.Builder;

namespace Stride.BepuPhysics.Navigation.Processors;

public class RecastMeshSystem : GameSystemBase
{
    public TimeSpan LastShapeCacheTime { get; private set; }
    public TimeSpan LastNavMeshBuildTime { get; private set; }

    // The size of the area to search for the nearest polygon.
    private readonly RcVec3f _polyPickExt = new(0.5f, 0.5f, 0.5f);

    private readonly Stopwatch _stopwatch = new();
    private readonly SceneSystem _sceneSystem;
    private readonly ShapeCacheSystem _shapeCache;

    private Task<DtNavMesh>? _runningRebuild;
    private Task<List<RcBuilderResult>>? _runningBuildResults;

    private CancellationTokenSource _rebuildingTask = new();
    private RecastNavigationConfiguration _navSettings;

    private CollidableProcessor _collidableProcessor = null!;
    private bool _dirty = true;

    public RecastMeshSystem(IServiceRegistry registry) : base(registry)
    {
        UpdateOrder = 20000;
        Enabled = true; //enabled by default

        registry.AddService(this);

        _sceneSystem = registry.GetSafeServiceAs<SceneSystem>();
        _shapeCache = registry.GetSafeServiceAs<ShapeCacheSystem>();

        var gameSettings = registry.GetSafeServiceAs<IGameSettingsService>();
        _navSettings = gameSettings.Settings.Configurations.Get<RecastNavigationConfiguration>() ?? new();

        if(_navSettings.NavMeshes.Count == 0)
        {
            _navSettings.NavMeshes.Add(new BepuNavMeshInfo());
        }

        InitializeNavMeshes();
    }

    private void InitializeNavMeshes()
    {
        foreach (var navSettings in _navSettings.NavMeshes)
        {
            navSettings.Config = navSettings.BuildSettings.CreateDynamicRecastSettings();

            navSettings.NavMeshParams = new DtNavMeshParams();
            navSettings.NavMeshParams.orig = new RcVec3f(0, 0, 0);
            navSettings.NavMeshParams.tileWidth = navSettings.BuildSettings.TileSize;
            navSettings.NavMeshParams.tileHeight = navSettings.BuildSettings.TileSize;
            navSettings.NavMeshParams.maxTiles = 0x8000; //TODO: make this configurable
            navSettings.NavMeshParams.maxPolys = 0x8000; //TODO: make this configurable

            navSettings.Builder = new RcBuilder();
            navSettings.Context = new RcContext();
        }
    }

    protected override void Destroy()
    {
        if (_collidableProcessor is not null)
        {
            _collidableProcessor.OnPostAdd -= StartTrackingCollidable;
            _collidableProcessor.OnPreRemove -= ClearTrackingForCollidable;
        }

        base.Destroy();
    }

    public override void Update(GameTime time)
    {
        if(_collidableProcessor is null)
        {
            _collidableProcessor = _sceneSystem.SceneInstance.GetProcessor<CollidableProcessor>()!;
            _collidableProcessor.OnPostAdd += StartTrackingCollidable;
            _collidableProcessor.OnPreRemove += ClearTrackingForCollidable;
            return;
        }

        foreach(var bepuNavMesh in _navSettings.NavMeshes)
        {
            if (bepuNavMesh.IsDynamic)
            {
                // only rebuild the tiles affected by the static colliders that have been added or removed.
                Rebuild(ProcessQueue(bepuNavMesh), bepuNavMesh);
            }
            else
            {
                if (_runningRebuild?.Status == TaskStatus.RanToCompletion)
                {
                    bepuNavMesh.NavMesh = _runningRebuild.Result;
                    _runningRebuild = null;
                    LastNavMeshBuildTime = _stopwatch.Elapsed;
                    _stopwatch.Reset();
                }
            }
        }
    }

    public Task RebuildNavMesh(int meshToRebuild = 0)
    {
        if(_navSettings.NavMeshes.Count <= meshToRebuild) return Task.CompletedTask;

#warning Right now no systems calls for a rebuild of the navmesh, users have to explicitly do it from their side, not the best ...
        // The goal of this method is to do the strict minimum here on the main thread, gathering data for the async thread to do the rest on its own

        // Cancel any ongoing rebuild
        _rebuildingTask.Cancel();
        _rebuildingTask = new CancellationTokenSource();

        _stopwatch.Start();

        // Fetch mesh data from the scene - this may be too slow
        // There are a couple of avenues we could go down into to fix this but none of them are easy
        // Something we'll have to investigate later.
        var asyncInput = new AsyncMeshInput();
        var containerProcessor = _sceneSystem.SceneInstance.Processors.Get<CollidableProcessor>();
        for (var e = containerProcessor.ComponentDataEnumerator; e.MoveNext();)
        {
            var collidable = e.Current.Value;

            // Only use StaticColliders for the nav mesh build.
            if (collidable is BodyComponent)
                continue;

            // skip if the collision layer should not be used for the nav mesh build.
            if (!_navSettings.NavMeshes[meshToRebuild].CollisionMask.IsSet(collidable.CollisionLayer))
                continue;

            // No need to store cache, nav mesh recompute should be rare enough were it would waste more memory than necessary
            collidable.Collider.AppendModel(asyncInput.ShapeData, _shapeCache, out object? cache);
            int shapeCount = collidable.Collider.Transforms;
            for (int i = shapeCount - 1; i >= 0; i--)
                asyncInput.TransformsOut.Add(default);
            collidable.Collider.GetLocalTransforms(collidable, CollectionsMarshal.AsSpan(asyncInput.TransformsOut)[^shapeCount..]);
            asyncInput.Matrices.Add((collidable.Entity.Transform.WorldMatrix, shapeCount));
        }

        LastShapeCacheTime = _stopwatch.Elapsed;
        _stopwatch.Reset();

        RcNavMeshBuildSettings settingsCopy = _navSettings.NavMeshes[meshToRebuild].BuildSettings.CreateRecastSettings();

        CancellationToken token = _rebuildingTask.Token;
        _stopwatch.Start();
        var task = Task.Run(() => BepuNavMeshBuilder.CreateNavMesh(settingsCopy, asyncInput, _navSettings.UsableThreadCount, token), token);
        _runningRebuild = task;
        return task;
    }


    public Task BuildNavMeshResults(int meshToRebuild = 0)
    {
        if (_navSettings.NavMeshes.Count <= meshToRebuild) return Task.CompletedTask;

        // Cancel any ongoing rebuild
        _rebuildingTask.Cancel();
        _rebuildingTask = new CancellationTokenSource();

        _stopwatch.Start();

        // Fetch mesh data from the scene - this may be too slow
        // There are a couple of avenues we could go down into to fix this but none of them are easy
        // Something we'll have to investigate later.
        var asyncInput = new AsyncMeshInput();
        var containerProcessor = _sceneSystem.SceneInstance.Processors.Get<CollidableProcessor>();
        for (var e = containerProcessor.ComponentDataEnumerator; e.MoveNext();)
        {
            var collidable = e.Current.Value;

            // Only use StaticColliders for the nav mesh build.
            if (collidable is BodyComponent)
                continue;

            // skip if the collision layer should not be used for the nav mesh build.
            if (!_navSettings.NavMeshes[meshToRebuild].CollisionMask.IsSet(collidable.CollisionLayer))
                continue;

            // No need to store cache, nav mesh recompute should be rare enough were it would waste more memory than necessary
            collidable.Collider.AppendModel(asyncInput.ShapeData, _shapeCache, out object? cache);
            int shapeCount = collidable.Collider.Transforms;
            for (int i = shapeCount - 1; i >= 0; i--)
                asyncInput.TransformsOut.Add(default);
            collidable.Collider.GetLocalTransforms(collidable, CollectionsMarshal.AsSpan(asyncInput.TransformsOut)[^shapeCount..]);
            asyncInput.Matrices.Add((collidable.Entity.Transform.WorldMatrix, shapeCount));
        }

        LastShapeCacheTime = _stopwatch.Elapsed;
        _stopwatch.Reset();

        RcNavMeshBuildSettings settingsCopy = _navSettings.NavMeshes[meshToRebuild].BuildSettings.CreateRecastSettings();

        CancellationToken token = _rebuildingTask.Token;
        _stopwatch.Start();
        var task = Task.Run(() => BepuNavMeshBuilder.CreateBuildResults(settingsCopy, asyncInput, _navSettings.UsableThreadCount, token), token);
        _runningBuildResults = task;
        return task;
    }

    /// <summary>
    /// Tries to find a path from the start to the end.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="polys"></param>
    /// <param name="smoothPath"></param>
    /// <param name="pathfindingSettings"></param>
    /// <returns></returns>
    public bool TryFindPath(Vector3 start, Vector3 end, ref List<long> polys, ref List<Vector3> smoothPath, PathfindingSettings pathfindingSettings, int navMeshIndex)
    {
        if (navMeshIndex >= _navSettings.NavMeshes.Count || _navSettings.NavMeshes[navMeshIndex].NavMesh is null) return false;

        var queryFilter = new DtQueryDefaultFilter();
        var dtNavMeshQuery = new DtNavMeshQuery(_navSettings.NavMeshes[navMeshIndex].NavMesh);

        dtNavMeshQuery.FindNearestPoly(start.ToDotRecastVector(), _polyPickExt, queryFilter, out long startRef, out _, out _);

        dtNavMeshQuery.FindNearestPoly(end.ToDotRecastVector(), _polyPickExt, queryFilter, out long endRef, out _, out _);
        // find the nearest point on the navmesh to the start and end points
        var result = dtNavMeshQuery.FindFollowPath(startRef, endRef, start.ToDotRecastVector(), end.ToDotRecastVector(), queryFilter, true, ref polys, polys.Count, ref smoothPath, pathfindingSettings);

        return result.Succeeded();
    }

    /// <summary>
    /// Tries to find a path from the start to the end. This uses the default <see cref="PathfindingSettings"/>.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="polys"></param>
    /// <param name="smoothPath"></param>
    /// <returns></returns>
    public bool TryFindPath(Vector3 start, Vector3 end, ref List<long> polys, ref List<Vector3> smoothPath, int navMeshIndex)
    {
        if (navMeshIndex >= _navSettings.NavMeshes.Count || _navSettings.NavMeshes[navMeshIndex].NavMesh is null) return false;

        var queryFilter = new DtQueryDefaultFilter();
        var dtNavMeshQuery = new DtNavMeshQuery(_navSettings.NavMeshes[navMeshIndex].NavMesh);

        dtNavMeshQuery.FindNearestPoly(start.ToDotRecastVector(), _polyPickExt, queryFilter, out long startRef, out _, out _);

        dtNavMeshQuery.FindNearestPoly(end.ToDotRecastVector(), _polyPickExt, queryFilter, out long endRef, out _, out _);
        // find the nearest point on the navmesh to the start and end points
        var result = dtNavMeshQuery.FindFollowPath(startRef, endRef, start.ToDotRecastVector(), end.ToDotRecastVector(), queryFilter, true, ref polys, polys.Count, ref smoothPath, _navSettings.PathfindingSettings);

        return result.Succeeded();
    }

    private void StartTrackingCollidable(CollidableComponent collidable)
    {
        foreach(var navMeshInfo in _navSettings.NavMeshes)
        {
            if (collidable is StaticComponent staticComponent)
            {
                var colliderId = staticComponent.StaticReference.Value.Handle.Value;
                navMeshInfo.StaticComponents.Add(colliderId, staticComponent);
                AddCollider(navMeshInfo, colliderId);
            }
        }
    }

    private void ClearTrackingForCollidable(CollidableComponent collidable)
    {
        foreach(var navMeshInfo in _navSettings.NavMeshes)
        {
            if (collidable is StaticComponent staticComponent)
            {
                var colliderId = staticComponent.StaticReference.Value.Handle.Value;
                RemoveCollider(navMeshInfo, colliderId);
                navMeshInfo.StaticComponents.Remove(colliderId);
            }

        }
    }

    public long AddCollider(BepuNavMeshInfo navMeshInfo, int cid)
    {
        DtTrimeshCollider collider = CreateTriMeshCollider(navMeshInfo.StaticComponents[cid]);
        navMeshInfo.UpdateQueue.Add(new DtDynamicTileColliderAdditionJob(cid, collider, GetTiles(collider.Bounds(), navMeshInfo)));
        return cid;
    }

    public void RemoveCollider(BepuNavMeshInfo navMeshInfo, long colliderId)
    {
        navMeshInfo.UpdateQueue.Add(new DtDynamicTileColliderRemovalJob(colliderId, GetTilesByCollider(colliderId, navMeshInfo)));
    }

    private DtTrimeshCollider CreateTriMeshCollider(StaticComponent staticComponent)
    {
        var input = new AsyncMeshInput();

        staticComponent.Collider.AppendModel(input.ShapeData, _shapeCache, out _);
        int shapeCount = staticComponent.Collider.Transforms;
        for (int i = shapeCount - 1; i >= 0; i--)
        {
            input.TransformsOut.Add(default);
        }
        staticComponent.Collider.GetLocalTransforms(staticComponent, CollectionsMarshal.AsSpan(input.TransformsOut)[^shapeCount..]);
        input.Matrices.Add((staticComponent.Entity.Transform.WorldMatrix, shapeCount));


        var verts = new List<VertexPosition3>();
        var indices = new List<int>();
        for (int collidableI = 0, shapeI = 0; collidableI < input.Matrices.Count; collidableI++)
        {
            (Matrix collidableMatrix, int _) = input.Matrices[collidableI];
            collidableMatrix.Decompose(out _, out Matrix worldMatrix, out Vector3 translation);
            worldMatrix.TranslationVector = translation;

            for (int j = 0; j < shapeCount; j++, shapeI++)
            {
                ShapeTransform transform = input.TransformsOut[shapeI];
                Matrix.Transformation(ref transform.Scale, ref transform.RotationLocal, ref transform.PositionLocal, out Matrix localMatrix);
                Matrix finalMatrix = localMatrix * worldMatrix;

                BasicMeshBuffers shape = input.ShapeData[shapeI];
                verts.EnsureCapacity(verts.Count + shape.Vertices.Length);
                indices.EnsureCapacity(indices.Count + shape.Indices.Length);

                int vertexBufferStart = verts.Count;

                for (int i = 0; i < shape.Indices.Length; i += 3)
                {
                    int index0 = shape.Indices[i];
                    int index1 = shape.Indices[i + 1];
                    int index2 = shape.Indices[i + 2];
                    indices.Add(vertexBufferStart + index0);
                    indices.Add(vertexBufferStart + index2);
                    indices.Add(vertexBufferStart + index1);
                }

                for (int l = 0; l < shape.Vertices.Length; l++)
                {
                    Vector3 vertex = shape.Vertices[l].Position;
                    Vector3.Transform(ref vertex, ref finalMatrix, out Vector3 transformedVertex);
                    verts.Add(new(transformedVertex));
                }
            }
        }

        // Get the backing array of this list,
        // get a span to that backing array,
        Span<VertexPosition3> spanToPoints = CollectionsMarshal.AsSpan(verts);
        // cast the type of span to read it as if it was a series of contiguous floats instead of contiguous vectors
        Span<float> reinterpretedPoints = MemoryMarshal.Cast<VertexPosition3, float>(spanToPoints);


        return new DtTrimeshCollider(reinterpretedPoints.ToArray(), [.. indices], 0x2, 10);
    }

    private HashSet<DtDynamicTile> ProcessQueue(BepuNavMeshInfo navSettings)
    {
        List<IDtDaynmicTileJob> items = ConsumeQueue(navSettings);
        foreach (IDtDaynmicTileJob item in items)
        {
            Process(item);
        }

        return items.SelectMany(i => i.AffectedTiles()).ToHashSet();
    }

    private List<IDtDaynmicTileJob> ConsumeQueue(BepuNavMeshInfo navSettings)
    {
        List<IDtDaynmicTileJob> items = [];
        while (navSettings.UpdateQueue.TryTake(out IDtDaynmicTileJob? item))
        {
            items.Add(item);
        }

        return items;
    }

    private static void Process(IDtDaynmicTileJob item)
    {
        foreach (DtDynamicTile? tile in item.AffectedTiles())
        {
            item.Process(tile);
        }
    }

    private bool Rebuild(ICollection<DtDynamicTile> tiles, BepuNavMeshInfo bepuNavMesh)
    {
        if (bepuNavMesh.Tiles.Count == 0 || bepuNavMesh.StaticComponents.Count == 0)
        {
            InitializeDynamicMesh(bepuNavMesh);
            return false;
        }

        foreach (DtDynamicTile tile in tiles)
            Rebuild(tile, bepuNavMesh);

        return UpdateNavMesh(bepuNavMesh);
    }

    /// <summary>
    /// Initializes the navigation mesh with all of the currently known Static colliders.
    /// </summary>
    /// <param name="bepuNavMesh"></param>
    private void InitializeDynamicMesh(BepuNavMeshInfo bepuNavMesh)
    {
        if(_runningBuildResults is null && bepuNavMesh.Tiles.Count == 0)
        {
            BuildNavMeshResults();
            return;
        }
        if (_runningBuildResults?.Status == TaskStatus.RanToCompletion)
        {
            var buildResults = _runningBuildResults.Result;
            _runningBuildResults = null;

            RcNavMeshBuildSettings navSettings = bepuNavMesh.BuildSettings.CreateRecastSettings();

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

            var voxelFile = DtVoxelFile.From(cfg, buildResults);

            bepuNavMesh.Tiles.Clear();
            foreach (var t in voxelFile.tiles)
            {
                bepuNavMesh.Tiles.Add(LookupKey(t.tileX, t.tileZ), new DtDynamicTile(t));
            }
        }

        for (var e = _collidableProcessor.ComponentDataEnumerator; e.MoveNext();)
        {
            if (e.Current.Value is StaticComponent staticCollider && staticCollider.StaticReference is not null)
            {
                var colliderId = staticCollider.StaticReference.Value.Handle.Value;
                bepuNavMesh.StaticComponents.Add(colliderId, staticCollider);
                AddCollider(bepuNavMesh, colliderId);
            }
        }
    }

    private ICollection<DtDynamicTile> GetTiles(float[] bounds, BepuNavMeshInfo navSettings)
    {
        if (bounds == null)
        {
            return navSettings.Tiles.Values;
        }

        int minx = (int)MathF.Floor((bounds[0] - navSettings.NavMeshParams.orig.X) / navSettings.NavMeshParams.tileWidth) - 1;
        int minz = (int)MathF.Floor((bounds[2] - navSettings.NavMeshParams.orig.Z) / navSettings.NavMeshParams.tileHeight) - 1;
        int maxx = (int)MathF.Floor((bounds[3] - navSettings.NavMeshParams.orig.X) / navSettings.NavMeshParams.tileWidth) + 1;
        int maxz = (int)MathF.Floor((bounds[5] - navSettings.NavMeshParams.orig.Z) / navSettings.NavMeshParams.tileHeight) + 1;
        List<DtDynamicTile> tiles = [];
        for (int z = minz; z <= maxz; ++z)
        {
            for (int x = minx; x <= maxx; ++x)
            {
                navSettings.Tiles.TryGetValue(LookupKey(x, z), out DtDynamicTile? tile);
                if (tile != null && IntersectsXZ(tile, bounds))
                {
                    tiles.Add(tile);
                }
            }
        }

        return tiles;
    }

    private static bool IntersectsXZ(DtDynamicTile tile, float[] bounds)
    {
        return tile.voxelTile.boundsMin.X <= bounds[3] && tile.voxelTile.boundsMax.X >= bounds[0] &&
               tile.voxelTile.boundsMin.Z <= bounds[5] && tile.voxelTile.boundsMax.Z >= bounds[2];
    }

    private List<DtDynamicTile> GetTilesByCollider(long cid, BepuNavMeshInfo navSettings)
    {
        return navSettings.Tiles.Values.Where(t => t.ContainsCollider(cid)).ToList();
    }

    private void Rebuild(DtDynamicTile tile, BepuNavMeshInfo navSettings)
    {
        _dirty |= tile.Build(navSettings.Builder, navSettings.Config, navSettings.Context);
    }

    private bool UpdateNavMesh(BepuNavMeshInfo bepuNavMesh)
    {
        if (_dirty)
        {
            _dirty = false;

            DtNavMesh navMesh = new();
            navMesh.Init(bepuNavMesh.NavMeshParams, bepuNavMesh.BuildSettings.VertsPerPoly);

            foreach (DtDynamicTile t in bepuNavMesh.Tiles.Values)
            {
                t.AddTo(navMesh);
            }

            bepuNavMesh.NavMesh = navMesh;
            return true;
        }

        return false;
    }

    private static long LookupKey(long x, long z)
    {
        return (z << 32) | x;
    }
}
