using System.Linq;
using Stride.BepuPhysics.Components;
using Stride.Core.Mathematics;
using Stride.Engine;


namespace Stride.BepuPhysics.Demo.Components.Utils
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

            if (entity.Get<ContainerComponent>() is BodyComponent body)
            {
                body.SimulationIndex = SimulationIndex;
                body?.ApplyImpulse(Impulse, ImpulsePos);
            }
        }
    }
}
