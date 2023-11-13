using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BepuPhysicIntegrationTest.Integration.Components.Simulations;
using BepuPhysicIntegrationTest.Integration.Components.Utils;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Engine;
using Stride.Games;
using Stride.Particles;

namespace BepuPhysicIntegrationTest.Integration.Processors
{
    public class SimulationProcessor : EntityProcessor<SimulationComponentBase>
    {
        private readonly List<SimulationComponent> _simulationComponents = new();

        private bool para = true;

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

            var totalWatch = new Stopwatch();
            var simUpdWatch = new Stopwatch();
            var simStepWatch = new Stopwatch();
            var parForWatch = new Stopwatch();

            totalWatch.Start();

            foreach (var item in _simulationComponents)
            {
                if (!item.Enabled)
                    continue;

                var SimTimeStep = dt * item.TimeWrap; //calcul the timeStep of the simulation

                simUpdWatch.Start();
                item.CallSimulationUpdate(SimTimeStep); //cal the SimulationUpdate with simTimeStep
                simUpdWatch.Stop();

                simStepWatch.Start();
                item.Simulation.Timestep(SimTimeStep, item.ThreadDispatcher); //perform physic sim using simTimeStep
                simStepWatch.Stop();

                parForWatch.Start();
                if (para)
                {
                    var a = Parallel.For(0, item.Simulation.Bodies.ActiveSet.Count, (i) =>
                    {
                            var handle = item.Simulation.Bodies.ActiveSet.IndexToHandle[i];
                            var entity = item.Bodies[handle];
                            var body = item.Simulation.Bodies[handle];

                            var entityTransform = entity.Transform;
                            entityTransform.Position = body.Pose.Position.ToStrideVector();
                            entityTransform.Rotation = body.Pose.Orientation.ToStrideQuaternion();
                            entityTransform.UpdateWorldMatrix();
                    });
                }
                else
                {
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
                parForWatch.Stop();
            }
            totalWatch.Stop();

            Debug.WriteLine($"total : {totalWatch.ElapsedMilliseconds} \n    sim update : {simUpdWatch.ElapsedMilliseconds}\n    sim step : {simStepWatch.ElapsedMilliseconds}\n    Position update : {parForWatch.ElapsedMilliseconds}");
            base.Update(time);
        }
    }
}
