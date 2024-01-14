using Silk.NET.OpenGL;
using Stride.BepuPhysics._2D.Components.Containers;
using Stride.BepuPhysics.Components;
using Stride.BepuPhysics.Configurations;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using static Stride.Graphics.Buffer;

namespace Stride.BepuPhysics._2D.Components
{

    [ComponentCategory("Bepu - 2D")]
    public class _2DSimulationConfigurator : SimulationUpdateComponent
    {
        public override void SimulationUpdate(float simTimeStep)
        {

        }
        public override void AfterSimulationUpdate(float simTimeStep)
        {
            if (BepuSimulation == null)
                return;

            for (int i = 0; i < BepuSimulation.Simulation.Bodies.ActiveSet.Count; i++)
            {
                var handle = BepuSimulation.Simulation.Bodies.ActiveSet.IndexToHandle[i];
                var body = BepuSimulation.GetContainer(handle);

                if (body is not _2DBodyContainerComponent)
                    continue;

                if (body.Position.Z > 0.05f || body.Position.Z < -0.05f)
                    body.Position *= new Vector3(1, 1, 0);//Fix Z = 0
                if (body.LinearVelocity.Z > 0.05f || body.LinearVelocity.Z < -0.05f)
                    body.LinearVelocity *= new Vector3(1, 1, 0);

                var bodyRot = body.Orientation;
                Quaternion.RotationYawPitchRoll(ref bodyRot, out var yaw, out var pitch, out var roll);
                if (yaw > 0.05f || pitch > 0.05f || yaw < -0.05f || pitch < -0.05f)
                    body.Orientation = Quaternion.RotationYawPitchRoll(0, 0, roll);
                if (body.AngularVelocity.X > 0.05f || body.AngularVelocity.Y > 0.05f || body.AngularVelocity.X < -0.05f || body.AngularVelocity.Y < -0.05f)
                    body.AngularVelocity *= new Vector3(0, 0, 1);
            }
        }
        public override void Update()
        {
        }
    }
}
