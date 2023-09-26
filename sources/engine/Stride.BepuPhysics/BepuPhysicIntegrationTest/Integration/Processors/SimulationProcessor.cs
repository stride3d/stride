using System.Collections.Generic;
using BepuPhysicIntegrationTest.Integration.Components.Simulations;
using Stride.Core.Annotations;
using Stride.Engine;
using Stride.Games;

namespace BepuPhysicIntegrationTest.Integration.Processors
{
    public class SimulationProcessor : EntityProcessor<SimulationComponent>
    {
        private readonly List<SimulationComponent> _simulationComponents = new(Extensions.LIST_SIZE);

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] SimulationComponent component, [NotNull] SimulationComponent data)
        {
            base.OnEntityComponentAdding(entity, component, data);
            _simulationComponents.Add(component);
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] SimulationComponent component, [NotNull] SimulationComponent data)
        {
            base.OnEntityComponentRemoved(entity, component, data);
            _simulationComponents.Remove(component);
        }

        public override void Update(GameTime time)
        {
            var dt = (float)time.Elapsed.TotalSeconds;
            if (dt == 0f)
                return;

            foreach (var item in _simulationComponents)
            {
                item.Simulation.Timestep(dt, item.ThreadDispatcher);
                for (int i = 0; i < item.Bodies.Count; i++)
                {
                    var strideTransform = item.Bodies[i].entity.Transform;
                    strideTransform.Position = item.Simulation.Bodies[item.Bodies[i].handle].Pose.Position.ToStrideVector();
                    strideTransform.Rotation = item.Simulation.Bodies[item.Bodies[i].handle].Pose.Orientation.ToStrideQuaternion();
                    strideTransform.UpdateWorldMatrix(); //Not sure if needed (it should make the position updated for this frame and not the next one)
                }
            }

            base.Update(time);
        }

    }
}
