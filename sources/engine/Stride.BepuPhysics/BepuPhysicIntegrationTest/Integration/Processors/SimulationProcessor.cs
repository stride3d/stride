using System.Collections.Generic;
using System.Linq;
using BepuPhysicIntegrationTest.Integration.Components.Simulations;
using BepuPhysicIntegrationTest.Integration.Components.Utils;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Engine;
using Stride.Games;

namespace BepuPhysicIntegrationTest.Integration.Processors
{
    public class SimulationProcessor : EntityProcessor<SimulationComponentBase>
    {
        private readonly List<SimulationComponent> _simulationComponents = new();

        public SimulationProcessor()
        {
            Order = 10000;
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] SimulationComponentBase component, [NotNull] SimulationComponentBase data)
        {
            base.OnEntityComponentAdding(entity, component, data);
            if (component is SimulationComponent sim)
            {
                _simulationComponents.Add(sim);
            }
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] SimulationComponentBase component, [NotNull] SimulationComponentBase data)
        {
            base.OnEntityComponentRemoved(entity, component, data);
            if (component is SimulationComponent sim)
            {
                _simulationComponents.Remove(sim);
                sim.Destroy();
            }
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

                var SimTimeStep = dt * item.TimeWrap; //calcul the timeStep of the simulation

                item.CallSimulationUpdate(SimTimeStep); //cal the SimulationUpdate with simTimeStep
                item.Simulation.Timestep(SimTimeStep, item.ThreadDispatcher); //perform physic sim using simTimeStep

                for (int i = 0; i < item.Simulation.Bodies.ActiveSet.Count; i++) //Update active body positions and rotation.
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
