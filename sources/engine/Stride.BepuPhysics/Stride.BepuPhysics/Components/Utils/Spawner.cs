using System.Linq;
using Stride.BepuPhysics.Components;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Extensions;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.BepuPhysics.Components.Utils
{
    public abstract class Spawner : SimulationUpdateComponent
    {
        public Prefab? SpawnPrefab { get; set; }
        public InstancingComponent? Instancing { get; set; }

        protected void Spawn(Vector3 position, Vector3 Impulse, Vector3 ImpulsePos)
        {
            if (SpawnPrefab == null)
                return;

            var entity = SpawnPrefab.Instantiate().First();
            entity.Transform.Position = position;
            Entity.AddChild(entity);

            var instance = entity.Get<InstanceComponent>();
            if (instance != null)
            {
                instance.Master = Instancing;
            }

            var container = entity.Get<ContainerComponent>();
            if (container != null && container is BodyContainerComponent body)
            {
                body.GetPhysicBody()?.ApplyImpulse(Impulse.ToNumericVector(), ImpulsePos.ToNumericVector());
            }
        }

    }
}
