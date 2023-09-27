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
                if (!item.Enabled)
                    continue;

                item.Simulation.Timestep(dt * item.TimeWrap, item.ThreadDispatcher);
                for (int i = 0; i < item.Simulation.Bodies.ActiveSet.Count; i++)
                {
                    var handle = item.Simulation.Bodies.ActiveSet.IndexToHandle[i];
                    var entity = item.Bodies[handle];
                    var body = item.Simulation.Bodies[handle];

                    var entityTransform = entity.Transform;
                    entityTransform.Position = body.Pose.Position.ToStrideVector();
                    entityTransform.Rotation = body.Pose.Orientation.ToStrideQuaternion();
                    entityTransform.UpdateWorldMatrix();
                }
            }

            base.Update(time);
        }

    }
}
