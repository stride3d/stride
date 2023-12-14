using System.Diagnostics;
using BepuPhysics;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Definitions.Character;
using Stride.BepuPhysics.Extensions;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using NVector3 = System.Numerics.Vector3;

namespace Stride.BepuPhysics.Components.Character;

[ComponentCategory("Bepu - Character")]
public class CharacterComponent : SimulationUpdateComponent
{
    private CharacterContactEventHandler? _collisionEvents;
    private bool _tryJump { get; set; }

    public float Speed { get; set; } = 10f;
    public float JumpSpeed { get; set; } = 1f;

    [DataMemberIgnore]
    public Quaternion Orientation { get; set; }
    [DataMemberIgnore]
    public Vector3 Velocity { get; set; }
    [DataMemberIgnore]
    public bool IsGrounded { get; private set; }

#warning if it requires a 'BodyContainerComponent' we should consider inheriting from 'BodyContainerComponent', that way ownership over the component is implied and we can override some of the behaviors appropriately
    public BodyContainerComponent? CharacterBody { get; set; }


    public override void Start()
    {
        base.Start();

        if (CharacterBody == null)
            throw new NullReferenceException(nameof(CharacterBody));

        if (BepuSimulation == null)
            return;

        CharacterBody.FrictionCoefficient = 0f;
        CharacterBody.UpdateInertia(new BodyInertia { InverseMass = 1f });

        _collisionEvents = new(this);
        CharacterBody.ContactEventHandler = _collisionEvents;
    }

    public override void Update()
    {
        DebugText.Print($"Mouse delta : {Input.MouseDelta}", new Int2(50, 950));
        DebugText.Print($"Velocity : {Velocity}", new Int2(50, 975));
        DebugText.Print($"Orientation : {Orientation}", new Int2(50, 1000));
        DebugText.Print($"IsGrounded : {IsGrounded}", new Int2(50, 1025));
        DebugText.Print($"ContactPoints count : {_collisionEvents?.Contacts.Count ?? 0}", new Int2(50, 1050));
    }

    public void Move(Vector3 direction)
    {
        Velocity = direction * Speed;
    }

    public void Rotate(Quaternion rotation)
    {
        Orientation = rotation;
    }

    /// <summary>
    /// Will jump if grounded
    /// </summary>
    public void TryJump()
    {
        _tryJump = true;
    }

    public override void SimulationUpdate(float simTimeStep)
    {
        CheckGrounded();

        if(CharacterBody == null)
			return;

		CharacterBody.Awake = true;

		CharacterBody.Orientation = Orientation;
		CharacterBody.LinearVelocity = new Vector3(Velocity.X, CharacterBody.LinearVelocity.Y, Velocity.Z);

        if (_tryJump)
        {
            if (IsGrounded)
				CharacterBody.ApplyImpulse(Vector3.UnitY * JumpSpeed * 10);
            _tryJump = false;
        }
    }
    public override void AfterSimulationUpdate(float simTimeStep)
    {
        if (CharacterBody == null)
            return;
        
        if (IsGrounded)
        {
            var linVeloExceptY = CharacterBody.LinearVelocity * new Vector3(1, 0, 1);
            var linVeloExceptYLen = linVeloExceptY.Length();
        
            if (linVeloExceptYLen < 0.8f && linVeloExceptYLen > 0.000001f)
            {
				CharacterBody.LinearVelocity = new Vector3(0, 0, 0);
                CharacterBody.IgnoreGravity = true;
            }
            return;
        }
        CharacterBody.IgnoreGravity = false;
    }
    private void CheckGrounded()
    {
        IsGrounded = false;
        if (_collisionEvents == null || CharacterBody?.Simulation == null || _collisionEvents.Contacts.Count == 0)
            return;

        var gravity = CharacterBody.Simulation.PoseGravity.ToNumericVector();
        foreach (var contact in _collisionEvents.Contacts)
        {
            var contactNormal = contact.Contact.Normal;

            if (NVector3.Dot(gravity, contactNormal) < 0) // If the body is supported by a contact whose surface is against gravity
            {
                IsGrounded = true;
                return;
            }
        }
    }
}


