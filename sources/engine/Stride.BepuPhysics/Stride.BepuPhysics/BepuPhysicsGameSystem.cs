using BepuPhysics;
using Stride.BepuPhysics.Configurations;
using Stride.BepuPhysics.Extensions;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Engine.Design;
using Stride.Games;

namespace Stride.BepuPhysics
{
    internal class BepuPhysicsGameSystem : GameSystemBase
    {
        private BepuConfiguration _bepuConfiguration;

        public BepuPhysicsGameSystem(IServiceRegistry registry) : base(registry)
        {
            var gameSettings = Services.GetSafeServiceAs<IGameSettingsService>();

            _bepuConfiguration = gameSettings.Settings.Configurations.Get<BepuConfiguration>();

            if (_bepuConfiguration.BepuSimulations.Count == 0)
            {
                _bepuConfiguration.BepuSimulations.Add(new BepuSimulation());
            }

            Services.AddService(_bepuConfiguration);
            Services.AddService(new BepuShapeCacheSystem(registry)); //Debug rendering & Navigation

            UpdateOrder = -1000; //make sure physics runs before everything
            Enabled = true; //enabled by default
        }

        public override void Update(GameTime time)
        {
            var dt = (float)time.Elapsed.TotalMilliseconds;
            if (dt == 0f)
                return;

            //GC.Collect(); //looks like to prevent crash (but RIP FPS)

            foreach (var bepuSim in _bepuConfiguration.BepuSimulations)
            {
                if (!bepuSim.Enabled)
                    continue;

                var totalTimeStepInMillisec = dt * bepuSim.TimeWarp; //Calculate the theoretical time step of the simulation
                bepuSim.RemainingUpdateTime += totalTimeStepInMillisec; //Add it to the counter

                int stepCount = 0;
                while (bepuSim.RemainingUpdateTime >= bepuSim.SimulationFixedStep & (stepCount < bepuSim.MaxStepPerFrame || bepuSim.MaxStepPerFrame == -1))
                {
                    var simTimeStepInSec = bepuSim.SimulationFixedStep / 1000f;
                    bepuSim.CallSimulationUpdate(simTimeStepInSec);//call the SimulationUpdate with the real step time of the sim in secs
                    bepuSim.Simulation.Timestep(simTimeStepInSec, bepuSim.ThreadDispatcher); //perform physic simulation using bepuSim.SimulationFixedStep
                    bepuSim.ContactEvents.Flush(); //Fire event handler stuff.
                    bepuSim.RemainingUpdateTime -= bepuSim.SimulationFixedStep; //in millisec
                    stepCount++;
                    bepuSim.CallAfterSimulationUpdate(simTimeStepInSec);//call the AfterSimulationUpdate with the real step time of the sim in secs
                }

                if (bepuSim.ParallelUpdate)
                {
                    Dispatcher.For(0, bepuSim.Simulation.Bodies.ActiveSet.Count, (i) => UpdateBodiesPositionFunction(bepuSim.Simulation.Bodies.ActiveSet.IndexToHandle[i], bepuSim));
                }
                else
                {
                    for (int i = 0; i < bepuSim.Simulation.Bodies.ActiveSet.Count; i++)
                    {
                        UpdateBodiesPositionFunction(bepuSim.Simulation.Bodies.ActiveSet.IndexToHandle[i], bepuSim);
                    }
                }
            }
        }
        private static void UpdateBodiesPositionFunction(BodyHandle handle, BepuSimulation bepuSim)
        {
            var bodyContainer = bepuSim.BodiesContainers[handle];
            var body = bepuSim.Simulation.Bodies[handle];

            var parentEntityPosition = new Vector3();
            var parentEntityRotation = Quaternion.Identity;
            var parent = bodyContainer.Entity.Transform.Parent;
            if (parent != null)
            {
                parent.WorldMatrix.Decompose(out Vector3 _, out parentEntityRotation, out parentEntityPosition);
            }

            var entityTransform = bodyContainer.Entity.Transform;

            Vector3 localPosition = (body.Pose.Position.ToStrideVector() - parentEntityPosition);
            entityTransform.Rotation = body.Pose.Orientation.ToStrideQuaternion() * Quaternion.Invert(parentEntityRotation);
            entityTransform.Position = Vector3.Transform(localPosition, Quaternion.Invert(parentEntityRotation)) - Vector3.Transform(bodyContainer.CenterOfMass, entityTransform.Rotation);

            if (bodyContainer.ChildsContainerComponent.Count > 0)
            {
                foreach (var item in bodyContainer.ChildsContainerComponent)
                {
                    //We need to call 
                    if (item.ContainerData != null && !item.ContainerData.IsStatic)
                    {
                        //Warning this may cause threading-race issues (but i did large tests and never had issues)
                        entityTransform.UpdateWorldMatrix();
                        UpdateBodiesPositionFunction(item.ContainerData.BHandle, bepuSim);
                    }
                }
            }

        }

    }
}
