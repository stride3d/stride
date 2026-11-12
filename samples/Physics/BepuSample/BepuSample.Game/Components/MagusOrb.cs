using System;
using System.Collections.Generic;
using BepuPhysics.Collidables;
using Stride.BepuPhysics;
using Stride.BepuPhysics.Components;
using Stride.BepuPhysics.Definitions;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace BepuSample.Game.Components;

public class MagusOrb : SyncScript, ISimulationUpdate
{
    private readonly Dictionary<BepuSimulation, Vector3> _affectedSims = new();
    private Quaternion _localRotation = Quaternion.Identity;

    public required BodyComponent Orb { get; set; }
    public required TransformComponent Camera { get; set; }

    public Vector3 CameraLocalOffset { get; set; } = new Vector3(0, 3, 12);
    public Quaternion CameraLocalOffsetRot { get; set; } = Quaternion.RotationX(MathUtil.DegreesToRadians(-20));

    public float TiltFactor { get; set; } = 0.35f;
    public float RotationFactor { get; set; } = 1f;
    public float CameraSmoothness { get; set; } = 0.25f;

    public override void Update()
    {
        var deltaTime = (float)this.Game.UpdateTime.Elapsed.TotalSeconds;
        
        if (Input.IsKeyDown(Keys.A))
            _localRotation *= Quaternion.RotationY(MathUtil.Pi * RotationFactor * deltaTime);
        if (Input.IsKeyDown(Keys.D))
            _localRotation *= Quaternion.RotationY(-MathUtil.Pi * RotationFactor * deltaTime);

        var gravityInducedRot = GravityTilt();
        gravityInducedRot.Invert();
        
        var expDecay = 1f - MathF.Exp(-100 * deltaTime * CameraSmoothness);
        var cameraTargetRotation = CameraLocalOffsetRot
                          * _localRotation
                          * gravityInducedRot
                          // +Z faces away from view
                          * Quaternion.RotationY(MathUtil.Pi);
        Camera.Rotation = Quaternion.Slerp(Camera.Rotation, cameraTargetRotation, expDecay);

        // Picking the transform position instead of rigidbody, we want the interpolated position, not the physics engine one
        var orbPosition = Orb.Entity.Transform.Position;
        Camera.Position = orbPosition + Camera.Rotation * CameraLocalOffset;

        // Prevents camera going through world geometry
        var sim = this.Entity.GetSimulation();
        var vector = Camera.Position - orbPosition;
        if (sim.SweepCast(
                new Sphere(0.2f), 
                new RigidPose(orbPosition, Quaternion.Identity), 
                new BodyVelocity(vector, default), 1f, out var result))
        {
            Camera.Position = orbPosition + vector * result.Distance;
        }
    }

    public override void Cancel()
    {
        base.Cancel();

        foreach (var (simulation, gravity) in _affectedSims)
            simulation.PoseGravity = gravity;
        _affectedSims.Clear();
    }

    public void SimulationUpdate(BepuSimulation simulation, float simTimeStep)
    {
        _affectedSims.TryAdd(simulation, simulation.PoseGravity);
        simulation.PoseGravity = GravityTilt() * _affectedSims[simulation];
    }

    private Quaternion GravityTilt()
    {
        float gravityTilt = 0;
        var forward = _localRotation * Vector3.UnitZ;
        if (Input.IsKeyDown(Keys.W))
            gravityTilt += TiltFactor;
        if (Input.IsKeyDown(Keys.S))
            gravityTilt -= TiltFactor;

        var downToForward = Quaternion.BetweenDirections(new Vector3(0, -1, 0), forward);
        return Quaternion.Slerp(Quaternion.Identity, downToForward, gravityTilt);
    }

    public void AfterSimulationUpdate(BepuSimulation simulation, float simTimeStep)
    {
        // unused
    }
}
