// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Engine.FlexibleProcessing;
using Stride.Games;

namespace Stride.DotRecast.Definitions;

public interface INavigationCollider : IComponent<INavigationCollider.NavigationColliderProcessor, INavigationCollider>
{

    /// <summary>
    /// Determines the navigation layers that this collider will affect.
    /// </summary>
    public NavMeshLayerGroup NavigationLayers { get; set; }

    /// <summary>
    /// Hash value of the collider. Used to determine if the collider has changed.
    /// </summary>
    /// <returns></returns>
    public bool ColliderValueHash();

    public NavigationColliderData GetNavigationMeshInputData();

    public class NavigationColliderProcessor : IProcessor, IUpdateProcessor
    {
        public int Order => 0;

        public List<INavigationCollider> Components { get; } = [];

        public delegate void CollectionChangedEventHandler(INavigationCollider component);

        public event CollectionChangedEventHandler ColliderAdded;
        public event CollectionChangedEventHandler ColliderRemoved;

        public void SystemAdded(IServiceRegistry registryParam)
        {
            registryParam.AddService(this);

            // Check if the processor was added before the DotRecast processor
            var boundingBoxProcessor = registryParam.GetService<DotRecastNavigationProcessor>();
            boundingBoxProcessor?.InitializeNavigationColliderProcessor(this);
        }

        public void SystemRemoved()
        {

        }

        public void OnComponentAdded(INavigationCollider item)
        {
            ColliderAdded?.Invoke(item);
            Components.Add(item);
        }

        public void OnComponentRemoved(INavigationCollider item)
        {
            ColliderRemoved?.Invoke(item);
            Components.Remove(item);
        }


        public void Update(GameTime gameTime)
        {
            foreach (var component in Components)
            {
                if (component.ColliderValueHash())
                {
                    // Update something
                }
            }
        }
    }
}
