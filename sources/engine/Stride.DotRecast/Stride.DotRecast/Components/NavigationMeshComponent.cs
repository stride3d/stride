// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using DotRecast.Core.Numerics;
using DotRecast.Detour;
using DotRecast.Detour.Dynamic;
using DotRecast.Recast.Toolset;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.DotRecast.Definitions;
using Stride.DotRecast.Extensions;
using Stride.DotRecast.Processors;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.DotRecast.Components;

[DataContract]
[ComponentCategory("DotRecast")]
[DefaultEntityComponentProcessor(typeof(DotRecastNavMeshProcessor), ExecutionMode = ExecutionMode.Runtime)]
public sealed class NavigationMeshComponent : StartupScript
{
    public List<DotRecastGeometryProvider> GeometryProviders = [];

    public DotRecastCollectionMethod CollectionMethod = DotRecastCollectionMethod.Scene;

    public NavMeshLayer NavMeshLayer;

    public RcNavMeshBuildSettings NavMeshBuildSettings = new();

    [DataMemberIgnore]
    public Dictionary<Entity, DotRecastShapeData> ShapeData = [];

    [DataMemberIgnore]
    public DtDynamicNavMesh DynamicNavMesh { get; internal set; }

    [DataMemberIgnore]
    public readonly Dictionary<NavigationObstacleComponent, long> ObstacleRefs = [];

    // The size of the area to search for the nearest polygon.
    private readonly RcVec3f _polyPickExt = new(0.5f, 0.5f, 0.5f);

    private List<NavigationObstacleComponent> _newlyAddedObstacles = [];
    private List<NavigationObstacleComponent> _newlyRemovedObstacles = [];

    public override void Start()
    {
        foreach (var provider in GeometryProviders)
        {
            provider.Initialize(Services);
        }
    }

    public void Update()
    {
        if(DynamicNavMesh is null)
        {
            CreateDynamicMesh();
            if(DynamicNavMesh is null)
            {
                return;
            }
        }

        foreach (var obstacle in _newlyAddedObstacles)
        {
            ObstacleRefs[obstacle] = DynamicNavMesh.AddCollider(obstacle.GetCollider());
        }
        _newlyAddedObstacles.Clear();
        
        foreach (var obstacle in _newlyRemovedObstacles)
        {
            DynamicNavMesh.RemoveCollider(ObstacleRefs[obstacle]);
        }
        _newlyRemovedObstacles.Clear();
        
        UpdateDynamicMesh();
    }

    private void UpdateDynamicMesh()
    {
        DynamicNavMesh.Update();
    }

    private void CreateDynamicMesh()
    {
        var shapeData = GetCombinedShapeData();
        if (shapeData is null)
        {
            return;
        }

        // get a span to that backing array,
        var spanToPoints = CollectionsMarshal.AsSpan(shapeData.Points);
        // cast the type of span to read it as if it was a series of contiguous floats instead of contiguous vectors
        var reinterpretedPoints = MemoryMarshal.Cast<Vector3, float>(spanToPoints);
        SrideGeomProvider geom = new(reinterpretedPoints.ToArray(), [.. shapeData.Indices]);

        var result = NavMeshBuilder.CreateTiledDynamicNavMesh(NavMeshBuildSettings, geom, new CancellationToken());

        DynamicNavMesh = result;
        DynamicNavMesh.Build();
    }

    public DotRecastShapeData GetCombinedShapeData()
    {
        var shapeData = new DotRecastShapeData();
        foreach (var data in ShapeData.Values)
        {
            shapeData.AppendOther(data);
        }
        return shapeData;
    }

    /// <summary>
    /// Tries to find a path from the start to the end. This uses default <see cref="PathfindingSettings"/>.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="polys"></param>
    /// <param name="smoothPath"></param>
    /// <returns></returns>
    public bool TryFindPath(Vector3 start, Vector3 end, ref List<long> polys, ref List<Vector3> smoothPath)
    {
        var queryFilter = new DtQueryDefaultFilter();
        var dtNavMeshQuery = new DtNavMeshQuery(DynamicNavMesh.NavMesh());

        dtNavMeshQuery.FindNearestPoly(start.ToDotRecastVector(), _polyPickExt, queryFilter, out var startRef, out _, out _);

        dtNavMeshQuery.FindNearestPoly(end.ToDotRecastVector(), _polyPickExt, queryFilter, out var endRef, out _, out _);
        // find the nearest point on the navmesh to the start and end points
        var result = dtNavMeshQuery.FindFollowPath(startRef, endRef, start.ToDotRecastVector(), end.ToDotRecastVector(), queryFilter, true, ref polys, polys.Count, ref smoothPath, new());

        return result.Succeeded();
    }

    /// <summary>
    /// Tries to find a path from the start to the end.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="polys"></param>
    /// <param name="smoothPath"></param>
    /// <returns></returns>
    public bool TryFindPath(Vector3 start, Vector3 end, ref List<long> polys, ref List<Vector3> smoothPath, PathfindingSettings settings)
    {
        var queryFilter = new DtQueryDefaultFilter();
        var dtNavMeshQuery = new DtNavMeshQuery(DynamicNavMesh.NavMesh());

        dtNavMeshQuery.FindNearestPoly(start.ToDotRecastVector(), _polyPickExt, queryFilter, out var startRef, out _, out _);

        dtNavMeshQuery.FindNearestPoly(end.ToDotRecastVector(), _polyPickExt, queryFilter, out var endRef, out _, out _);
        // find the nearest point on the navmesh to the start and end points
        var result = dtNavMeshQuery.FindFollowPath(startRef, endRef, start.ToDotRecastVector(), end.ToDotRecastVector(), queryFilter, true, ref polys, polys.Count, ref smoothPath, settings);

        return result.Succeeded();
    }

    internal void CheckEntity(Entity entity)
    {
        var obstacleComponent = entity.Get<NavigationObstacleComponent>();

        if (obstacleComponent is not null)
        {
            AddObstacle(entity.Get<NavigationObstacleComponent>());
        }
        else
        {
            foreach (var provider in GeometryProviders)
            {
                if (provider.TryGetTransformedShapeInfo(entity, out var shapeData))
                {
                    ShapeData[entity] = shapeData;
                }
            }
        }
    }

    internal void AddObstacle(NavigationObstacleComponent component)
    {
        _newlyAddedObstacles.Add(component);
    }

    internal void RemoveObstacle(NavigationObstacleComponent component)
    {
        _newlyRemovedObstacles.Add(component);
        DynamicNavMesh.RemoveCollider(ObstacleRefs[component]);
    }
}

public enum DotRecastCollectionMethod
{
    /// <summary>
    /// Collects all entities in the scene of the entity with the <see cref="NavigationMeshComponent"/>"/>
    /// </summary>
    Scene,

    /// <summary>
    /// Collects all children of the entity with the <see cref="NavigationMeshComponent"/>"/>
    /// </summary>
    Children,

    /// <summary>
    /// Collects all entitys with a valid component in a boundingbox volume
    /// </summary>
    BoundingBox,
}
