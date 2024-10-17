using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Navigation.Components;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Engine;
using Stride.Games;
using System.Collections.Concurrent;

namespace Stride.BepuPhysics.Navigation.Processors;

public sealed class RecastNavigationProcessor : EntityProcessor<RecastNavigationComponent>
{
    private RecastMeshProcessor _recastMeshProcessor;
    private readonly List<RecastNavigationComponent> _components = new();
    private readonly ConcurrentQueue<RecastNavigationComponent> _tryGetPathQueue = new();

    public RecastNavigationProcessor()
    {
        //run after the RecastMeshProcessor
        Order = 20001;
        _recastMeshProcessor = null!; // Initialized below
    }

    protected override void OnSystemAdd()
    {
        ServicesHelper.LoadBepuServices(Services, out _, out _, out _);
        if (Services.GetService<RecastMeshProcessor>() is { } recastMeshProcessor)
        {
            _recastMeshProcessor = recastMeshProcessor;
        }
        else
        {
            // add the RecastMeshProcessor if it doesn't exist
            _recastMeshProcessor = new RecastMeshProcessor(Services);
            // add to the Scenes processors
            var sceneSystem = Services.GetSafeServiceAs<SceneSystem>();
            sceneSystem.Game!.GameSystems.Add(_recastMeshProcessor);
        }

        Services.AddService(this);
    }

    protected override void OnEntityComponentAdding(Entity entity, RecastNavigationComponent component, RecastNavigationComponent data)
    {
        _components.Add(component);
    }

    protected override void OnEntityComponentRemoved(Entity entity, RecastNavigationComponent component, RecastNavigationComponent data)
    {
        _components.Remove(component);
    }

    public override void Update(GameTime time)
    {
        var deltaTime = (float)time.Elapsed.TotalSeconds;

        for (int i = 0; i < 10; i++)
        {
            if (_tryGetPathQueue.IsEmpty) break;

            if (_tryGetPathQueue.TryDequeue(out var pathfinding))
            {
                // cannot use dispatcher here because of the TryFindPath method.
                SetNewPath(pathfinding);
            }
        }

        Dispatcher.For(0, _components.Count, i =>
        {
            var component = _components[i];

            if(component.State == NavigationState.QueuePathPlanning)
            {
                _tryGetPathQueue.Enqueue(component);
                component.State = NavigationState.PlanningPath;
            }

            component.Update(deltaTime);
        });
    }

    public bool SetNewPath(RecastNavigationComponent pathfinder)
    {
        if (_recastMeshProcessor.TryFindPath(pathfinder.Entity.Transform.WorldMatrix.TranslationVector, pathfinder.Target, ref pathfinder.Polys, ref pathfinder.Path))
        {
            pathfinder.State = NavigationState.PathIsReady;
            return true;
        }
        return false;
    }
}
