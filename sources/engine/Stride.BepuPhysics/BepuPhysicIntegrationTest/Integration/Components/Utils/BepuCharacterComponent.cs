using BepuPhysicIntegrationTest.Integration.Components.Containers;
using BepuPhysicIntegrationTest.Integration.Extensions;
using BepuPhysics;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace BepuPhysicIntegrationTest.Integration.Components.Utils;
public class BepuCharacterComponent : SyncScript
{
	public float Speed { get; set; } = 1f;
	public float JumpSpeed { get; set; } = 1f;

	[DataMemberIgnore]
	public Quaternion Orientation { get; set; }

	public BodyContainerComponent? CharacterBody { get; set; }

	private BodyReference? _bodyReference;

	public override void Start()
	{
		_bodyReference = CharacterBody?.GetPhysicBody().Value;
		// prevent tipping of character while moving
		_bodyReference.Value.LocalInertia = new BodyInertia { InverseMass = 1f };
	}

	public override void Update()
	{

	}

	public void Move(Vector3 direction)
	{
		var body = CharacterBody?.GetPhysicBody();
		body.Value.Velocity.Linear += direction.ToNumericVector() * Speed;
	}

	public void Rotate(Quaternion rotation)
	{
		var body = CharacterBody?.GetPhysicBody();
		body.Value.Pose.Orientation = rotation.ToNumericQuaternion();
	}
}
