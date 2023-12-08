using BepuPhysics;
using Silk.NET.OpenGL;
using Stride.BepuPhysics.Components;
using Stride.BepuPhysics.Components.Colliders;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Definitions.Character;
using Stride.BepuPhysics.Extensions;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using System;

namespace Stride.BepuPhysics.Components.Character;

[ComponentCategory("Bepu - Character")]
public class CharacterComponent : SimulationUpdateComponent
{
    private CharacterContactEventHandler? _collisionEvents;
    private bool _tryJump { get; set; }

    public float Speed { get; set; } = 10f;
    public float JumpSpeed { get; set; } = 1f;

    [DataMemberIgnore]
    public Quaternion Orientation { get; private set; }
    [DataMemberIgnore]
    public Vector3 Velocity { get; private set; }
    [DataMemberIgnore]
    public bool IsGrounded { get; private set; }

    public BodyContainerComponent? CharacterBody { get; set; }
    public CapsuleColliderComponent? CharacterCapsule { get; set; }


    public override void Start()
    {
        base.Start();

        if (CharacterBody == null || BepuSimulation == null)
            return;

        var body = CharacterBody.GetPhysicBody();

        if (body == null)
            return;

        body.Value.LocalInertia = new BodyInertia { InverseMass = 1f };

        _collisionEvents = new(BepuSimulation.Simulation);
        CharacterBody.ContactEventHandler = _collisionEvents;
    }

    public override void Update()
    {
        DebugText.Print($"Mouse delta : {Input.MouseDelta}", new Int2(50, 50));
        DebugText.Print($"Velocity : {Velocity}", new Int2(50, 75));
        DebugText.Print($"Orientation : {Orientation}", new Int2(50, 100));
        DebugText.Print($"IsGrounded : {IsGrounded}", new Int2(50, 150));
        DebugText.Print($"ContactPoints count : {_collisionEvents.ContactPoints.Count}", new Int2(50, 175));
    }

    public void Move(Vector3 direction)
    {
        Velocity = direction * Speed;
    }

    public void Rotate(Quaternion rotation)
    {
        Orientation = rotation;
    }

    public void Jump()
    {
        _tryJump = true;
    }

    public override void SimulationUpdate(float simTimeStep)
    {
        var body = CharacterBody?.GetPhysicBody().Value;
        CheckGrounded();

        if (body == null)
        {
            return;
        }

        // needed a way to wake up the body or else it sleeps after a second.
        var value = body.Value;
        value.Awake = true;

        body.Value.Pose.Orientation = Orientation.ToNumericQuaternion();
        body.Value.Velocity.Linear = new System.Numerics.Vector3(Velocity.X, body.Value.Velocity.Linear.Y, Velocity.Z);

        // prevent character from sliding
        if (Velocity.Length() < 0.01f)
            body.Value.Velocity.Linear *= new System.Numerics.Vector3(.01f, 1, .01f);

        if (_tryJump)
        {
            if (IsGrounded)
                body.Value.ApplyLinearImpulse(System.Numerics.Vector3.UnitY * JumpSpeed * 10);
            _tryJump = false;
        }
    }

    private void CheckGrounded()
    {
        float capsuleLength = CharacterCapsule?.Length ?? 0.5f;
        float capsuleRadius = (CharacterCapsule?.Radius ?? 0.35f) * 1.15f;

        foreach (var contact in _collisionEvents.ContactPoints)
        {
            var contactWorldPos = contact.ToStrideVector() + Entity.Transform.GetWorldPos() + Entity.Get<BodyContainerComponent>().CenterOfMass;
            var capsuleBottom = Entity.Transform.WorldMatrix.TranslationVector - Entity.Transform.WorldMatrix.Up * capsuleLength / 2;

            var distanceToBottom = Vector3.Distance(contactWorldPos, capsuleBottom);

            // If the contact point is close enough to the capsule's top or bottom, consider the entity grounded
            if (distanceToBottom < capsuleRadius)
            {
                IsGrounded = true;
                return;
            }
        }
        if (_collisionEvents.ContactPoints.Count != 0)
        {
            var x = 0;
        }
        IsGrounded = false;
    }
}


