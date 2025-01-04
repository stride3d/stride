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
using Stride.BepuPhysics.Definitions.Colliders;
using System.ComponentModel;
using DotRecast.Core;
using DotRecast.Detour.Dynamic.Colliders;
using DotRecast.Detour.Dynamic;
using Stride.BepuPhysics.Definitions;
using System.Collections.Concurrent;
using DotRecast.Recast;

namespace Stride.BepuPhysics.Navigation.Processors;

public class RecastMeshSystem : GameSystemBase
{
    public TimeSpan LastShapeCacheTime { get; private set; }
    public TimeSpan LastNavMeshBuildTime { get; private set; }

    // The size of the area to search for the nearest polygon.
    private readonly RcVec3f _polyPickExt = new(2, 4, 2);

    private readonly Stopwatch _stopwatch = new();
    private readonly SceneSystem _sceneSystem;
    private readonly ShapeCacheSystem _shapeCache;

    private Task<DtNavMesh>? _runningRebuild;

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

        _collidableProcessor = _sceneSystem.SceneInstance.GetProcessor<CollidableProcessor>()!;
        _collidableProcessor.OnPostAdd += StartTrackingCollidable;
        _collidableProcessor.OnPreRemove += ClearTrackingForCollidable;
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

    public override void Update(GameTime time)
    {
        foreach(var navSettings in _navSettings.NavMeshes)
        {
            if (!navSettings.IsDynamic)
            {
                if (_runningRebuild?.Status == TaskStatus.RanToCompletion)
                {
                    navSettings.NavMesh = _runningRebuild.Result;
                    _runningRebuild = null;
                    LastNavMeshBuildTime = _stopwatch.Elapsed;
                    _stopwatch.Reset();
                }
            }
        }

        foreach (var navSettings in _navSettings.NavMeshes)
        {
            if (navSettings.IsDynamic)
            {
                Rebuild(ProcessQueue(navSettings), navSettings);
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
        var task = Task.Run(() => _navSettings.NavMeshes[meshToRebuild].NavMesh = BepuNavMeshBuilder.CreateBepuNavMesh(settingsCopy, asyncInput, _navSettings.UsableThreadCount, token), token);
        _runningRebuild = task;
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

    //public List<Vector3>? GetNavMeshTiles()
    //{
    //    if (_navMesh is null) return null;
    //
    //    List<Vector3> verts = [];
    //
    //    for (int i = 0; i < _navMesh.GetMaxTiles(); i++)
    //    {
    //        var tile = _navMesh.GetTile(i);
    //        if (tile?.data != null)
    //        {
    //            for (int j = 0; j < tile.data.verts.Length; j += 3)
    //            {
    //                var point = new Vector3(
    //                    tile.data.verts[j],
    //                    tile.data.verts[j + 1],
    //                    tile.data.verts[j + 2]);
    //                verts.Add(point);
    //            }
    //        }
    //    }
    //
    //    return verts;
    //}

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
                navMeshInfo.StaticComponents.Remove(colliderId);
                RemoveCollider(navMeshInfo, colliderId);
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

    private bool Rebuild(ICollection<DtDynamicTile> tiles, BepuNavMeshInfo navSettings)
    {
        if(navSettings.NavMesh is null) return false;

        foreach (DtDynamicTile tile in tiles)
            Rebuild(tile, navSettings);

        return UpdateNavMesh(navSettings);
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

    private bool UpdateNavMesh(BepuNavMeshInfo navSettings)
    {
        if (_dirty)
        {
            _dirty = false;

            DtNavMesh navMesh = new();
            navMesh.Init(navSettings.NavMeshParams, navSettings.BuildSettings.VertsPerPoly);

            foreach (DtDynamicTile t in navSettings.Tiles.Values)
            {
                t.AddTo(navMesh);
            }

            navSettings.NavMesh = navMesh;
            return true;
        }

        return false;
    }

    private static long LookupKey(long x, long z)
    {
        return (z << 32) | x;
    }
}
