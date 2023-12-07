using BepuPhysicIntegrationTest.Integration.Components.Character;
using BepuPhysicIntegrationTest.Integration.Components.Containers;
using BepuPhysicIntegrationTest.Integration.Extensions;
using BepuPhysics;
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

	private CharacterCollisionEvents _collisionEvents = new();

	public override void Start()
	{
		var body = CharacterBody?.GetPhysicBody().Value;
		// prevent tipping of character while moving
		body.Value.LocalInertia = new BodyInertia { InverseMass = 1f };


		base.Start();
		_collisionEvents.Simulation = BepuSimulation.Simulation;
		CharacterBody.ContactEventHandler = _collisionEvents;
	}

	public override void Update()
	{
		DebugText.Print(Input.MouseDelta.ToString(), new Int2(50, 50));
		DebugText.Print(Velocity.ToString(), new Int2(50, 75));
		DebugText.Print(Orientation.ToString(), new Int2(50, 100));
		DebugText.Print(IsGrounded.ToString(), new Int2(50, 125));
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

		body.Value.Velocity.Linear = new System.Numerics.Vector3
			(Velocity.ToNumericVector().X, body.Value.Velocity.Linear.Y, Velocity.ToNumericVector().Z);

		// prevent character from sliding
		if(Velocity.Length() < 0.01f)
			body.Value.Velocity.Linear = new System.Numerics.Vector3(0, body.Value.Velocity.Linear.Y, 0);

        if (TryJump)
        {
			body.Value.ApplyLinearImpulse(System.Numerics.Vector3.UnitY * JumpSpeed * 10);
			TryJump = false;
        }
    }

	private void CheckGrounded()
	{
		IsGrounded = false;
		foreach(var contact in _collisionEvents.ContactPoints.Values)
		{
			var worldPos = Entity.Transform.WorldMatrix.TranslationVector;
			var v = new Vector2(contact.X, contact.Z);
			var u = new Vector2(worldPos.X, worldPos.Z);
			var vLength = v.Length();
			var uLength = u.Length();
			var dot = Vector2.Dot(v, u);

			var angleInRadians = (float)Math.Acos(dot / (vLength * uLength));

			DebugText.Print($"grounded {angleInRadians}", new Int2(50, 150));
			DebugText.Print($"grounded {contact}", new Int2(50, 175));
			DebugText.Print($"grounded {Entity.Transform.WorldMatrix.TranslationVector}", new Int2(50, 200));
			if (angleInRadians > -2.7 && angleInRadians < -2.3)
			{
				IsGrounded = true;
				return;
			}
		}
	}
}


