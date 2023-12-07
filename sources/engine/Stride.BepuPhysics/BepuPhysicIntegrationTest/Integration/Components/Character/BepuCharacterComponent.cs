using BepuPhysicIntegrationTest.Integration.Components.Character;
using BepuPhysicIntegrationTest.Integration.Components.Colliders;
using BepuPhysicIntegrationTest.Integration.Components.Containers;
using BepuPhysicIntegrationTest.Integration.Extensions;
using BepuPhysics;
using Silk.NET.OpenGL;
using Stride.Core;
using Stride.Core.Mathematics;
using System;

namespace BepuPhysicIntegrationTest.Integration.Components.Utils;
public class BepuCharacterComponent : SimulationUpdateComponent
{
    public float Speed { get; set; } = 10f;
    public float JumpSpeed { get; set; } = 1f;

    [DataMemberIgnore]
    public Quaternion Orientation { get; set; }
    [DataMemberIgnore]
    public Vector3 Velocity { get; set; }
    [DataMemberIgnore]
    public bool TryJump { get; set; }
    [DataMemberIgnore]
    public bool IsGrounded { get; set; }

    public BodyContainerComponent? CharacterBody { get; set; }
    public CapsuleColliderComponent? CharacterCapsule { get; set; }

    private CharacterCollisionEvents _collisionEvents = new();
    private float angleInRadians;

    public override void Start()
    {
        var body = CharacterBody?.GetPhysicBody();
        // prevent tipping of character while moving
        body.Value.LocalInertia = new BodyInertia { InverseMass = 1f };


        base.Start();
        _collisionEvents.Simulation = BepuSimulation.Simulation;
        CharacterBody.ContactEventHandler = _collisionEvents;
    }

    public override void Update()
    {
        DebugText.Print($"Mouse delta : {Input.MouseDelta}", new Int2(50, 50));
        DebugText.Print($"Velocity : {Velocity}", new Int2(50, 75));
        DebugText.Print($"Orientation : {Orientation}", new Int2(50, 100));
        DebugText.Print($"Contact Angle (rad) {angleInRadians}", new Int2(50, 125));
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
        TryJump = true;
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

        if (TryJump)
        {
            if (IsGrounded)
                body.Value.ApplyLinearImpulse(System.Numerics.Vector3.UnitY * JumpSpeed * 10);
            TryJump = false;
        }
    }

    private void CheckGrounded()
    {
        float capsuleLength = CharacterCapsule?.Length ?? 0.5f;
        float capsuleRadius = (CharacterCapsule?.Radius ?? 0.35f) * 1.15f;

        foreach (var contact in _collisionEvents.ContactPoints)
        {
            var contactWorldPos = contact.ToStrideVector();
            var capsuleTop = Entity.Transform.WorldMatrix.TranslationVector + (Entity.Transform.WorldMatrix.Up * capsuleLength / 2);
            var capsuleBottom = Entity.Transform.WorldMatrix.TranslationVector - (Entity.Transform.WorldMatrix.Up * capsuleLength / 2);

            var distanceToTop = Vector3.Distance(contactWorldPos, capsuleTop);
            var distanceToBottom = Vector3.Distance(contactWorldPos, capsuleBottom);

            // If the contact point is close enough to the capsule's top or bottom, consider the entity grounded
            if (distanceToTop < capsuleRadius || distanceToBottom < capsuleRadius)
            {
                IsGrounded = true;
                return;
            }
        }
        IsGrounded = false;
        angleInRadians = 0;
    }
}


