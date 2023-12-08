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

            var instance = entity.Get<InstanceComponent>();
            if (instance != null)
            {
                instance.Master = Instancing;
            }
            
            Entity.AddChild(entity);

            var container = entity.Get<ContainerComponent>();
            if (container != null && container is BodyContainerComponent body)
            {
                body.SimulationIndex = SimulationIndex;
                var bepuBody = body.GetPhysicBody();
                bepuBody?.ApplyImpulse(Impulse.ToNumericVector(), ImpulsePos.ToNumericVector());
            }

        }

    }
}
