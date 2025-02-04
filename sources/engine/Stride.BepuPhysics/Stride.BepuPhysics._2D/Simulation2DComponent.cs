// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.BepuPhysics.Components;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.BepuPhysics._2D;

[ComponentCategory("Bepu")]
public class Simulation2DComponent : SyncScript, ISimulationUpdate
{
    //public float MaxZLiberty { get; set; } = 0.05f;

    public void SimulationUpdate(BepuSimulation sim, float simTimeStep)
    {

    }
    public void AfterSimulationUpdate(BepuSimulation sim, float simTimeStep)
    {
        for (int i = 0; i < sim.Simulation.Bodies.ActiveSet.Count; i++)
        {
            var handle = sim.Simulation.Bodies.ActiveSet.IndexToHandle[i];
            var body = sim.GetComponent(handle);

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
