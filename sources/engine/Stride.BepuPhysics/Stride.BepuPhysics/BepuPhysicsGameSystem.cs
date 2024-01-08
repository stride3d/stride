using BepuPhysics;
using Stride.BepuPhysics.Configurations;
using Stride.BepuPhysics.Extensions;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Games;

namespace Stride.BepuPhysics
{
    internal class BepuPhysicsGameSystem : GameSystemBase
    {
        private BepuConfiguration _bepuConfiguration;

        public BepuPhysicsGameSystem(IServiceRegistry registry) : base(registry)
        {
            _bepuConfiguration = registry.GetService<BepuConfiguration>();
            UpdateOrder = -1000; //make sure physics runs before everything
            Enabled = true; //enabled by default


            foreach (var bepuSim in _bepuConfiguration.BepuSimulations)
            {
                bepuSim.ResetSoftStart();
            }
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
                    if (bepuSim.SoftStartDuration != 0 && bepuSim.SoftStartRemainingDurationMs == bepuSim.SoftStartDuration)
                    {
                        bepuSim.Simulation.Solver.SubstepCount = bepuSim.SolveSubStep * bepuSim.SoftStartSoftness;
                        bepuSim.SoftStartRemainingDurationMs--;
                    }
                    else if (bepuSim.SoftStartRemainingDurationMs == 0)
                    {
                        bepuSim.Simulation.Solver.SubstepCount = bepuSim.SolveSubStep / bepuSim.SoftStartSoftness;
                        bepuSim.SoftStartRemainingDurationMs = -1;
                    }

                    if (bepuSim.SoftStartRemainingDurationMs > 0)
                    {
                        bepuSim.SoftStartRemainingDurationMs = Math.Max(0, bepuSim.SoftStartRemainingDurationMs - bepuSim.SimulationFixedStep);
                    }

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

            var entityTransform = bodyContainer.Entity.Transform;
            var localPosition = body.Pose.Position.ToStrideVector();
            var localRotation = body.Pose.Orientation.ToStrideQuaternion();
            if (bodyContainer.Entity.Transform.Parent is { } parent)
            {
                parent.WorldMatrix.Decompose(out Vector3 _, out Quaternion parentEntityRotation, out Vector3 parentEntityPosition);
                localRotation = localRotation * Quaternion.Invert(parentEntityRotation);
                localPosition = Vector3.Transform(localPosition - parentEntityPosition, Quaternion.Invert(parentEntityRotation));
            }

            entityTransform.Rotation = localRotation;
            entityTransform.Position = localPosition - Vector3.Transform(bodyContainer.CenterOfMass, entityTransform.Rotation);

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
