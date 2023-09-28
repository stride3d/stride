using System;
using System.ComponentModel;
using System.Linq;
using BepuPhysicIntegrationTest.Integration.Components.Containers;
using BepuPhysics;
using BepuPhysics.Collidables;
using Silk.NET.OpenXR;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace BepuPhysicIntegrationTest.Integration.Components.Utils
{
    public abstract class Spawner : SimulationUpdateComponent
    {
        public Prefab SpawnPrefab { get; set; }
        public InstancingComponent Instancing { get; set; }

        protected void Spawn(Vector3 position, Vector3 Impulse, Vector3 ImpulsePos)
        {
            var entity = SpawnPrefab.Instantiate().First();
            entity.Transform.Position = position;
            Entity.AddChild(entity);

            var instance = entity.Get<InstanceComponent>();
            if (instance != null)
            {
                instance.Master = Instancing;
            }

            var container = entity.Get<ContainerComponent>();
            if (container != null)
            {
                container.BepuSimulation.Simulation.Bodies[container.ContainerData.BHandle].ApplyImpulse(Impulse.ToNumericVector(), ImpulsePos.ToNumericVector());
            }
        }

    }
}
