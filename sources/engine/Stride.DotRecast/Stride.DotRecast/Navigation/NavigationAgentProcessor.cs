// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Concurrent;
using Stride.Core.Threading;
using Stride.Engine;
using Stride.Games;

namespace Stride.DotRecast.Navigation;
public class NavigationAgentProcessor : EntityProcessor<NavigationAgentComponent>
{
    private readonly List<NavigationAgentComponent> _components = new();
    private readonly ConcurrentQueue<NavigationAgentComponent> _tryGetPathQueue = new();

    public NavigationAgentProcessor()
    {
        //run after the Mesh Processor
        Order = 50_001;
    }

    protected override void OnSystemAdd()
    {
        Services.AddService(this);
    }

    protected override void OnEntityComponentAdding(Entity entity, NavigationAgentComponent component, NavigationAgentComponent data)
    {
        _components.Add(component);
    }

    protected override void OnEntityComponentRemoved(Entity entity, NavigationAgentComponent component, NavigationAgentComponent data)
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

            if (component.State == NavigationState.QueuePathPlanning)
            {
                _tryGetPathQueue.Enqueue(component);
                component.State = NavigationState.PlanningPath;
            }

            component.Update(deltaTime);
        });
    }

    private static bool SetNewPath(NavigationAgentComponent pathfinder)
    {
        if (pathfinder.TryFindPath(pathfinder.Target))
        {
            pathfinder.State = NavigationState.PathIsReady;
            return true;
        }
        return false;
    }
}
