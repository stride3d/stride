// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Annotations;
using Stride.DotRecast.Components;
using Stride.Engine;
using Stride.Games;

namespace Stride.DotRecast.Processors;

public class DotRecastNavMeshProcessor : EntityProcessor<NavigationMeshComponent>
{
    private SceneSystem _sceneSystem = null!;

    private readonly Queue<NavigationMeshComponent> _addedComponents = new();
    private readonly Queue<NavigationMeshComponent> _removedComponents = new();

    public DotRecastNavMeshProcessor()
    {
        Order = 50_000;
    }

    protected override void OnSystemAdd()
    {
        Services.AddService(this);

        _sceneSystem = Services.GetSafeServiceAs<SceneSystem>();
    }

    protected override void OnEntityComponentAdding(Entity entity, [NotNull] NavigationMeshComponent component, [NotNull] NavigationMeshComponent data)
    {
        _addedComponents.Enqueue(component);
    }

    protected override void OnEntityComponentRemoved(Entity entity, [NotNull] NavigationMeshComponent component, [NotNull] NavigationMeshComponent data)
    {
        _removedComponents.Enqueue(component);
    }

    public override void Update(GameTime time)
    {
        foreach (var component in _addedComponents)
        {
            InitializeNavMeshComponent(component);
        }
        _addedComponents.Clear();

        foreach (var component in ComponentDatas.Values)
        {
            component.Update();
        }
    }

    private void InitializeNavMeshComponent(NavigationMeshComponent component)
    {
        switch(component.CollectionMethod)
        {
            case DotRecastCollectionMethod.Scene:
                GetObjectsInScene(component);
                break;
            case DotRecastCollectionMethod.Children:
                throw new NotImplementedException("Getting Child entities is not yet supported for nav mesh generation.");
            case DotRecastCollectionMethod.BoundingBox:
                throw new NotImplementedException("Bounding boxes are not yet supported for nav mesh generation.");
        }

        _sceneSystem.SceneInstance.EntityAdded += SceneInstance_EntityAdded;
        _sceneSystem.SceneInstance.EntityRemoved += SceneInstance_EntityRemoved;
    }

    private void SceneInstance_EntityRemoved(object? sender, Entity e)
    {
        var component = e.Get<NavigationObstacleComponent>();
        if (component is not null)
        {
            foreach (var navMeshComponent in ComponentDatas.Values)
            {
                navMeshComponent.RemoveObstacle(component);
            }
        }
    }

    private void SceneInstance_EntityAdded(object? sender, Entity e)
    {
        var component = e.Get<NavigationObstacleComponent>();
        if (component is not null)
        {
            foreach (var navMeshComponent in ComponentDatas.Values)
            {
                navMeshComponent.AddObstacle(component);
            }
        }
    }

    private void GetObjectsInScene(NavigationMeshComponent component)
    {
        // Due to how the Transform processor works there shouldnt be a need for recursion here.
        foreach (var entity in _sceneSystem.SceneInstance)
        {
            component.CheckEntity(entity);
        }
    }

    private void GetObjectsInChildren(NavigationMeshComponent component)
    {

    }

}
