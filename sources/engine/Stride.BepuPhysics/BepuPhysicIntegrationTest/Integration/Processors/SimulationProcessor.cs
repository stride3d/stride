using System.Collections.Generic;
using BepuPhysicIntegrationTest.Integration.Components.Colliders;
using BepuPhysicIntegrationTest.Integration.Components.Simulations;
using BepuPhysicIntegrationTest.Integration.Components.Utils;
using BepuPhysicIntegrationTest.Integration.Configurations;
using Stride.Engine;
using Stride.Games;

namespace BepuPhysicIntegrationTest.Integration.Processors
{
    public class SimulationProcessor : EntityProcessor<ColliderComponent>
    {
        private readonly List<BepuSimulation> _simulationComponents = new();
        private BepuConfiguration _bepuconfiguration;

        public SimulationProcessor()
        {
            Order = 10000;
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

                var SimTimeStep = dt * item.TimeWrap; //calculate the timeStep of the simulation

                item.CallSimulationUpdate(SimTimeStep); //calculate the SimulationUpdate with simTimeStep
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
        }

    }
}
