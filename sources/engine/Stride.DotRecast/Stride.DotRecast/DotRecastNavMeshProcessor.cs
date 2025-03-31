using System.Runtime.InteropServices;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.DotRecast.Definitions;
using Stride.Engine;
using Stride.Games;

namespace Stride.DotRecast;
public class DotRecastNavMeshProcessor : EntityProcessor<DotRecastNavMeshComponent>
{
    private SceneSystem _sceneSystem = null!;

    private readonly Queue<DotRecastNavMeshComponent> _addedComponents = new();
    private readonly Queue<DotRecastNavMeshComponent> _removedComponents = new();

    public DotRecastNavMeshProcessor()
    {
        Order = 50_000;
    }

    protected override void OnSystemAdd()
    {
        Services.AddService(this);

        _sceneSystem = Services.GetSafeServiceAs<SceneSystem>();
    }

    protected override void OnEntityComponentAdding(Entity entity, [NotNull] DotRecastNavMeshComponent component, [NotNull] DotRecastNavMeshComponent data)
    {
        _addedComponents.Enqueue(component);
    }

    protected override void OnEntityComponentRemoved(Entity entity, [NotNull] DotRecastNavMeshComponent component, [NotNull] DotRecastNavMeshComponent data)
    {
        _removedComponents.Enqueue(component);
    }

    public override void Update(GameTime time)
    {
        foreach (var component in _addedComponents)
        {
            GetInitialObjectsToBuild(component);
        }
        _addedComponents.Clear();

        foreach (var component in ComponentDatas.Values)
        {
            if (component.IsDirty)
            {
                RebuildNavMesh(component);
            }
        }
    }

    private void RebuildNavMesh(DotRecastNavMeshComponent component)
    {
        var shapeData = component.GetCombinedShapeData();
        if(shapeData is null)
        {
            return;
        }

        // get a span to that backing array,
        var spanToPoints = CollectionsMarshal.AsSpan(shapeData.Points);
        // cast the type of span to read it as if it was a series of contiguous floats instead of contiguous vectors
        var reinterpretedPoints = MemoryMarshal.Cast<Vector3, float>(spanToPoints);
        SimpleGeomProvider geom = new(reinterpretedPoints.ToArray(), [.. shapeData.Indices]);

        var result = NavMeshBuilder.CreateNavMeshFromGeometry(component.NavMeshBuildSettings, geom, Dispatcher.MaxDegreeOfParallelism, new CancellationToken());

        component.NavMesh = result;
        component.IsDirty = false;
    }

    private void OnNavigationColliderRemoved(INavigationObstacle component)
    {
        //pendingRebuild = true;
    }

    private void OnNavigationColliderAdded(INavigationObstacle component)
    {
        //pendingRebuild = true;
    }

    private void GetInitialObjectsToBuild(DotRecastNavMeshComponent component)
    {
        switch(component.CollectionMethod)
        {
            case DotRecastCollectionMethod.Scene:
                GetObjectsInScene(component);
                break;
            case DotRecastCollectionMethod.Children:
                GetObjectsInChildren(component);
                break;
            case DotRecastCollectionMethod.BoundingBox:
                throw new NotImplementedException("Bounding boxes are not yet supported for nav mesh generation.");
        }
    }

    private void GetObjectsInScene(DotRecastNavMeshComponent component)
    {
        // Due to how the Transform processor works there shouldnt be a need for recursion here.
        foreach (var entity in _sceneSystem.SceneInstance)
        {
            component.CheckEntity(entity);
            component.IsDirty = true;
        }
    }

    private void GetObjectsInChildren(DotRecastNavMeshComponent component)
    {

    }

}
