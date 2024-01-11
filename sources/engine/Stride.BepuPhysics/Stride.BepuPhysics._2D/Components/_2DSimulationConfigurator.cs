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
        private _2DBodyContainerComponent[] bodies = Array.Empty<_2DBodyContainerComponent>();

        public override void SimulationUpdate(float simTimeStep)
        {

        }

        public override void AfterSimulationUpdate(float simTimeStep)
        {
            foreach (var body in bodies)
            {
                body.Position *= new Vector3(1, 1, 0);//Fix Z = 0
                body.LinearVelocity *= new Vector3(1, 1, 0);

                var bodyRot = body.Orientation;
                Quaternion.RotationYawPitchRoll(ref bodyRot, out var yaw, out var pitch, out var roll);
                body.Orientation = Quaternion.RotationYawPitchRoll(0, 0, roll);
                body.AngularVelocity *= new Vector3(0, 0, 1);
            }
        }

        public override void Update()
        {
             bodies = BepuSimulation.BodiesContainers.Values.OfType<_2DBodyContainerComponent>().ToArray();
        }
    }
}
