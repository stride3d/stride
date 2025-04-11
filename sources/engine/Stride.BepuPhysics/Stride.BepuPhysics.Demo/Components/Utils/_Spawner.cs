// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using Stride.BepuPhysics.Components;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Systems;
using Stride.Core.Mathematics;
using Stride.Engine;


namespace Stride.BepuPhysics.Demo.Components.Utils
{
    public abstract class Spawner : SyncScript
    {
        public Prefab? SpawnPrefab { get; set; }
        public InstancingComponent? Instancing { get; set; }

        [DefaultValueIsSceneBased]
        public ISimulationSelector SimulationSelector { get; set; } = SceneBasedSimulationSelector.Shared;

        protected void Spawn(Vector3 position, Vector3 Impulse, Vector3 ImpulsePos)
        {
            if (SpawnPrefab == null)
                return;

            var entity = SpawnPrefab.Instantiate().First();
            entity.Transform.Position = position;

            var instance = entity.Get<InstanceComponent>();
            if (instance != null)
            {
                instance.Master = Instancing;
            }

            Entity.AddChild(entity);

            if (entity.Get<CollidableComponent>() is BodyComponent body)
            {
                body.SimulationSelector = SimulationSelector;
                body.ApplyImpulse(Impulse, ImpulsePos);
            }
        }
    }
}
