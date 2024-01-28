using Stride.BepuPhysics.Components;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.BepuPhysics._2D
{

    [ComponentCategory("Bepu")]
    public class Simulation2DComponent : SimulationUpdateComponent
    {
        //public float MaxZLiberty { get; set; } = 0.05f;

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

                if (body is not Body2DComponent)
                    continue;

                //if (body.Position.Z > MaxZLiberty || body.Position.Z < -MaxZLiberty)
                if (body.Position.Z != 0)
                    body.Position *= new Vector3(1, 1, 0);//Fix Z = 0
                //if (body.LinearVelocity.Z > MaxZLiberty || body.LinearVelocity.Z < -MaxZLiberty)
                if (body.LinearVelocity.Z != 0)
                    body.LinearVelocity *= new Vector3(1, 1, 0);

                var bodyRot = body.Orientation;
                Quaternion.RotationYawPitchRoll(ref bodyRot, out var yaw, out var pitch, out var roll);
                //if (yaw > MaxZLiberty || pitch > MaxZLiberty || yaw < -MaxZLiberty || pitch < -MaxZLiberty)
                if (yaw != 0 || pitch != 0)
                    body.Orientation = Quaternion.RotationYawPitchRoll(0, 0, roll);
                //if (body.AngularVelocity.X > MaxZLiberty || body.AngularVelocity.Y > MaxZLiberty || body.AngularVelocity.X < -MaxZLiberty || body.AngularVelocity.Y < -MaxZLiberty)
                if (body.AngularVelocity.X != 0 || body.AngularVelocity.Y != 0)
                    body.AngularVelocity *= new Vector3(0, 0, 1);
            }
        }
        public override void Update()
        {
        }
    }
}
