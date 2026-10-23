// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.BepuPhysics;
using Stride.BepuPhysics.Soft;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace BepuSoftBodySample.Game.Components;

/// <summary>
/// Holding the left mouse button grabs what is under the crosshair and pulls it to a point in front of the camera,
/// the mouse wheel moves that point, F launches what is held; F or the middle button with nothing held throws a ball.
/// </summary>
/// <remarks> Soft bodies are held by the particles around the grabbed point </remarks>
[ComponentCategory("BepuSoftBodyDemo")]
public class SoftGravityGunComponent : SyncScript
{
    private readonly List<(int Particle, Vector3 Offset, float Grip)> _particles = new();
    private readonly List<Entity> _markers = new();
    private SoftBodyComponent? _soft;
    private BodyComponent? _body;
    private Vector3 _bodyOffset;
    private float _distance;

    /// <summary> The entity the camera follows, rays start from it </summary>
    public Entity? CameraEntity { get; set; }

    /// <summary> Placed at the point held objects are pulled to, and along the way to it </summary>
    public Prefab? MarkerPrefab { get; set; }

    /// <summary> Thrown with F or the middle mouse button, it should have a <see cref="BodyComponent"/> </summary>
    public Prefab? BallPrefab { get; set; }

    /// <summary> How fast held objects follow the point in front of the camera, per second </summary>
    public float Responsiveness { get; set; } = 12f;

    /// <summary> The fastest a held soft body is pulled, in meters per second </summary>
    public float MaximumPullSpeed { get; set; } = 10f;

    /// <summary> The speed of launched objects and thrown balls, in meters per second </summary>
    public float LaunchSpeed { get; set; } = 16f;

    public override void Update()
    {
        if (CameraEntity is null || Entity.GetSimulation() is not { } simulation)
            return;

        var origin = CameraEntity.Transform.WorldMatrix.TranslationVector;
        var forward = Vector3.Normalize(Vector3.TransformNormal(-Vector3.UnitZ, CameraEntity.Transform.WorldMatrix));

        if (Input.IsMouseButtonPressed(MouseButton.Left))
            Grab(simulation, origin, forward);
        else if (Input.IsMouseButtonReleased(MouseButton.Left))
            Release();

        if (Holding)
        {
            _distance = Math.Clamp(_distance + Input.MouseWheelDelta * 0.4f, 1f, 40f);
            if (Input.IsKeyPressed(Keys.F))
            {
                Launch(forward * LaunchSpeed);
                Release();
            }
            else
            {
                Pull(origin + forward * _distance);
            }
        }
        else if (Input.IsKeyPressed(Keys.F) || Input.IsMouseButtonPressed(MouseButton.Middle))
        {
            Throw(origin + forward, forward * LaunchSpeed);
        }

        ShowMarkers(origin, forward);
        var aiming = simulation.RayCast(origin, forward, 100f, out var hit) && (hit.Collidable is SoftBodyComponent || hit.Collidable is BodyComponent { Kinematic: false });
        var center = new Int2(Game.Window.ClientBounds.Width / 2, Game.Window.ClientBounds.Height / 2);
        DebugText.Print(Holding ? "( )" : aiming ? "[+]" : "+", center - new Int2(Holding || aiming ? 12 : 4, 8), Holding ? Color.Cyan : aiming ? Color.Yellow : Color.White);
    }

    private bool Holding => _soft is not null || _body is not null;

    private void Grab(BepuSimulation simulation, Vector3 origin, Vector3 forward)
    {
        Release();
        if (simulation.RayCast(origin, forward, 100f, out var hit) == false)
            return;

        _distance = hit.Distance;
        if (hit.Collidable is SoftBodyComponent soft)
        {
            var closest = float.MaxValue;
            for (int i = 0; i < soft.ParticleCount; i++)
                closest = MathF.Min(closest, Vector3.Distance(soft.GetParticlePosition(i), hit.Point));
            var reach = closest + 0.35f;
            for (int i = 0; i < soft.ParticleCount; i++)
            {
                var offset = soft.GetParticlePosition(i) - hit.Point;
                var distance = offset.Length();
                if (distance <= reach && soft.IsParticlePinned(i) == false)
                    _particles.Add((i, offset, MathF.Max(0.15f, 1f - distance / reach)));
            }
            if (_particles.Count > 0)
                _soft = soft;
        }
        else if (hit.Collidable is BodyComponent { Kinematic: false } body)
        {
            _body = body;
            _bodyOffset = Vector3.Transform(hit.Point - body.Position, Quaternion.Invert(body.Orientation));
        }
    }

    private void Release()
    {
        _soft = null;
        _body = null;
        _particles.Clear();
    }

    private void Pull(Vector3 target)
    {
        if (_soft is { } soft)
        {
            if (soft.ParticleCount == 0)
            {
                Release();
                return;
            }
            foreach (var (particle, offset, grip) in _particles)
            {
                var pull = (target + offset - soft.GetParticlePosition(particle)) * Responsiveness;
                if (pull.Length() > MaximumPullSpeed)
                    pull = Vector3.Normalize(pull) * MaximumPullSpeed;
                soft.SetParticleVelocity(particle, Vector3.Lerp(soft.GetParticleVelocity(particle), pull, grip));
            }
        }
        else if (_body is { } body)
        {
            if (body.Simulation is null)
            {
                Release();
                return;
            }
            body.LinearVelocity = (target - body.Position - Vector3.Transform(_bodyOffset, body.Orientation)) * Responsiveness;
            body.AngularVelocity *= 0.9f;
            body.Awake = true;
        }
    }

    private void Launch(Vector3 velocity)
    {
        if (_soft is { } soft)
        {
            for (int i = 0; i < soft.ParticleCount; i++)
                soft.SetParticleVelocity(i, velocity);
        }
        else if (_body is { } body)
        {
            body.LinearVelocity = velocity;
        }
    }

    private void Throw(Vector3 position, Vector3 velocity)
    {
        if (BallPrefab is null)
            return;

        foreach (var ball in BallPrefab.Instantiate())
        {
            ball.Transform.Position = position;
            Entity.Scene.Entities.Add(ball);
            if (ball.Get<BodyComponent>() is { } body)
                body.LinearVelocity = velocity;
        }
    }

    private void ShowMarkers(Vector3 origin, Vector3 forward)
    {
        if (MarkerPrefab is null)
            return;

        while (_markers.Count < 9)
        {
            foreach (var marker in MarkerPrefab.Instantiate())
            {
                _markers.Add(marker);
                Entity.Scene.Entities.Add(marker);
            }
        }

        // One marker where held objects are pulled to, the others in a line from just below the crosshair
        var target = origin + forward * _distance;
        var muzzle = origin + Vector3.TransformNormal(new Vector3(0.25f, -0.2f, -0.6f), CameraEntity!.Transform.WorldMatrix);
        for (int i = 0; i < _markers.Count; i++)
        {
            _markers[i].Transform.Position = i == 0 ? target : Vector3.Lerp(muzzle, target, (float)i / _markers.Count);
            _markers[i].Transform.Scale = new Vector3(i == 0 ? 1f : 0.3f);
            foreach (var model in _markers[i].GetAll<ModelComponent>())
                model.Enabled = Holding;
        }
    }
}
