using BepuPhysicIntegrationTest.Integration.Components.Containers;
using BepuPhysicIntegrationTest.Integration.Extensions;
using BepuPhysics;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace BepuPhysicIntegrationTest.Integration.Components.Utils;
public class BepuCharacterComponent : SyncScript
{
	public float Speed { get; set; } = 1f;
	public float JumpSpeed { get; set; } = 1f;

	public BodyContainerComponent? CharacterBody { get; set; }

	private BodyReference? _bodyReference;

	public override void Start()
	{
		_bodyReference = CharacterBody?.GetPhysicBody().Value;
	}

	public override void Update()
	{
		//_bodyReference.Value.Pose.Orientation = Quaternion.Identity.ToNumericQuaternion();
	}

	public void Move(Vector3 direction)
	{
		_bodyReference.Value.Velocity.Linear += direction.ToNumericVector() * Speed;
	}
}
