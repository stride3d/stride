using DotRecast.Core.Numerics;
using DotRecast.Detour;
using DotRecast.Recast.Toolset;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.DotRecast.Extensions;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.DotRecast.Definitions;

[DataContract]
[DefaultEntityComponentProcessor(typeof(DotRecastNavMeshProcessor), ExecutionMode = ExecutionMode.Runtime)]
public sealed class DotRecastNavMeshComponent : StartupScript
{
    public List<DotRecastGeometryProvider> GeometryProviders = [];

    public DotRecastCollectionMethod CollectionMethod = DotRecastCollectionMethod.Scene;

    public NavMeshLayer NavMeshLayer;

    public RcNavMeshBuildSettings NavMeshBuildSettings = new();

    [DataMemberIgnore] 
    public HashSet<Guid> EntityIds = [];

    [DataMemberIgnore] 
    public Dictionary<Entity, DotRecastShapeData> ShapeData = [];

    [DataMemberIgnore]
    public DtNavMesh NavMesh { get; internal set; }

    [DataMemberIgnore]
    public bool IsDirty { get; set; }

    // The size of the area to search for the nearest polygon.
    private readonly RcVec3f _polyPickExt = new(0.5f, 0.5f, 0.5f);

    public override void Start()
    {
        foreach (var provider in GeometryProviders)
        {
            provider.Initialize(Services);
        }
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
    /// Tries to find a path from the start to the end. This uses the default <see cref="PathfindingSettings"/>.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="polys"></param>
    /// <param name="smoothPath"></param>
    /// <returns></returns>
    public bool TryFindPath(Vector3 start, Vector3 end, ref List<long> polys, ref List<Vector3> smoothPath)
    {
        var queryFilter = new DtQueryDefaultFilter();
        var dtNavMeshQuery = new DtNavMeshQuery(NavMesh);

        dtNavMeshQuery.FindNearestPoly(start.ToDotRecastVector(), _polyPickExt, queryFilter, out long startRef, out _, out _);

        dtNavMeshQuery.FindNearestPoly(end.ToDotRecastVector(), _polyPickExt, queryFilter, out long endRef, out _, out _);
        // find the nearest point on the navmesh to the start and end points
        var result = dtNavMeshQuery.FindFollowPath(startRef, endRef, start.ToDotRecastVector(), end.ToDotRecastVector(), queryFilter, true, ref polys, polys.Count, ref smoothPath, new());

        return result.Succeeded();
    }

    internal void CheckEntity(Entity entity)
    {
        foreach (var provider in GeometryProviders)
        {
            if (provider.TryGetTransformedShapeInfo(entity, out var shapeData))
            {
                EntityIds.Add(entity.Id);
                ShapeData[entity] = shapeData;
            }
        }
    }
}

public enum DotRecastCollectionMethod
{
    /// <summary>
    /// Collects all entities in the scene of the entity with the <see cref="DotRecastNavMeshComponent"/>"/>
    /// </summary>
    Scene,

    /// <summary>
    /// Collects all children of the entity with the <see cref="DotRecastNavMeshComponent"/>"/>
    /// </summary>
    Children,

    /// <summary>
    /// Collects all entitys with a valid component in a boundingbox volume
    /// </summary>
    BoundingBox,
}
