using System.Diagnostics;
using BepuPhysics;
using Stride.BepuPhysics.Components;
using Stride.BepuPhysics.Components.Containers;
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
            UpdateOrder = BepuOrderHelper.ORDER_OF_GAME_SYSTEM; 
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

                var totalTimeStepInMillisec = (int)(dt * bepuSim.TimeWarp); //Calculate the theoretical time step of the simulation
                bepuSim.RemainingUpdateTimeMs += totalTimeStepInMillisec; //Add it to the counter

                int stepCount = 0;
                while (bepuSim.RemainingUpdateTimeMs >= bepuSim.SimulationFixedStep & (stepCount < bepuSim.MaxStepPerFrame || bepuSim.MaxStepPerFrame == -1))
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
                    bepuSim.RemainingUpdateTimeMs -= bepuSim.SimulationFixedStep; //in millisec
                    stepCount++;

                    if (bepuSim.ParallelUpdate)
                    {
                        Dispatcher.For(0, bepuSim.Simulation.Bodies.ActiveSet.Count, (i) => SyncTransformsWithPhysics(bepuSim.Simulation.Bodies.ActiveSet.IndexToHandle[i], bepuSim));
                    }
                    else
                    {
                        for (int i = 0; i < bepuSim.Simulation.Bodies.ActiveSet.Count; i++)
                        {
                            SyncTransformsWithPhysics(bepuSim.Simulation.Bodies.ActiveSet.IndexToHandle[i], bepuSim);
                        }
                    }

                    bepuSim.CallAfterSimulationUpdate(simTimeStepInSec);//call the AfterSimulationUpdate with the real step time of the sim in secs & set previousPose/CurrentPose of each containers.
                }

                // Find the interpolation factor, a value [0,1] which represents the ratio of the current time relative to the previous and the next physics step,
                // a value of 0.5 means that we're halfway to the next physics update, just have to wait for the same amount of time.
                float interpolationFactor = bepuSim.RemainingUpdateTimeMs / bepuSim.SimulationFixedStep;
                interpolationFactor = MathF.Min(interpolationFactor, 1f);
                if (bepuSim.ParallelUpdate)
                {
                    Dispatcher.For(0, bepuSim.InterpolatedBodies.Count, (i) => InterpolateTransforms(bepuSim.InterpolatedBodies[i], interpolationFactor));
                }
                else
                {
                    foreach (var body in bepuSim.InterpolatedBodies)
                    {
                        InterpolateTransforms(body, interpolationFactor);
                    }
                }
            }
        }

        private static void SyncTransformsWithPhysics(BodyHandle handle, BepuSimulation bepuSim)
        {
            var bodyContainer = bepuSim.BodiesContainers[handle];
            Debug.Assert(bodyContainer.ContainerData is not null);

            if (bodyContainer.ContainerData.Parent is {} containerParent)
            {
                Debug.Assert(containerParent.ContainerData is not null);
                // Have to go through our parents to make sure they're up to date since we're reading from the parent's world matrix
                // This means that we're potentially updating bodies that are not part of the active set but checking that may be more costly than just doing the thing
                SyncTransformsWithPhysics(containerParent.ContainerData.BHandle, bepuSim);
                // This can be slower than expected when we have multiple containers as parents recursively since we would recompute the topmost container n times, the second topmost n-1 etc.
                // It's not that likely but should still be documented as suboptimal somewhere
                containerParent.Entity.Transform.Parent.UpdateWorldMatrix();
            }

            var body = bepuSim.Simulation.Bodies[handle];
            var localPosition = body.Pose.Position.ToStrideVector();
            var localRotation = body.Pose.Orientation.ToStrideQuaternion();

            var entityTransform = bodyContainer.Entity.Transform;
            if (entityTransform.Parent is { } parent)
            {
                parent.WorldMatrix.Decompose(out Vector3 _, out Quaternion parentEntityRotation, out Vector3 parentEntityPosition);
                var iRotation = Quaternion.Invert(parentEntityRotation);
                localPosition = Vector3.Transform(localPosition - parentEntityPosition, iRotation);
                localRotation = localRotation * iRotation;
            }

            entityTransform.Rotation = localRotation;
            entityTransform.Position = localPosition - Vector3.Transform(bodyContainer.CenterOfMass, localRotation);
        }

        private static void InterpolateTransforms(BodyContainerComponent body, float interpolationFactor)
        {
            Debug.Assert(body.ContainerData is not null);

            // Have to go through our parents to make sure they're up to date since we're reading from the parent's world matrix
            // This means that we're potentially updating bodies that are not part of the active set but checking that may be more costly than just doing the thing
            for (var containerParent = body.ContainerData.Parent; containerParent != null; containerParent = containerParent.ContainerData!.Parent)
            {
                if (containerParent is BodyContainerComponent parentBody && parentBody.Interpolation != Interpolation.None)
                {
                    InterpolateTransforms(parentBody, interpolationFactor); // That guy will take care of his parents too
                    // This can be slower than expected when we have multiple containers as parents recursively since we would recompute the topmost container n times, the second topmost n-1 etc.
                    // It's not that likely but should still be documented as suboptimal somewhere
                    containerParent.Entity.Transform.Parent.UpdateWorldMatrix();
                    break;
                }
            }

            if (body.Interpolation == Interpolation.Extrapolated)
                interpolationFactor += 1f;

            var interpolatedPosition = System.Numerics.Vector3.Lerp(body.PreviousPose.Position, body.CurrentPos.Position, interpolationFactor).ToStrideVector();
            // We may be able to get away with just a Lerp instead of Slerp, not sure if it needs to be normalized though at which point it may not be that much faster
            var interpolatedRotation = System.Numerics.Quaternion.Slerp(body.PreviousPose.Orientation, body.CurrentPos.Orientation, interpolationFactor).ToStrideQuaternion();

            var entityTransform = body.Entity.Transform;
            if (entityTransform.Parent is { } parent)
            {
                parent.WorldMatrix.Decompose(out Vector3 _, out Quaternion parentEntityRotation, out Vector3 parentEntityPosition);
                var iRotation = Quaternion.Invert(parentEntityRotation);
                interpolatedPosition = Vector3.Transform(interpolatedPosition - parentEntityPosition, iRotation);
                interpolatedRotation = interpolatedRotation * iRotation;
            }

            entityTransform.Rotation = interpolatedRotation;
            entityTransform.Position = interpolatedPosition - Vector3.Transform(body.CenterOfMass, interpolatedRotation);
        }
    }
}
