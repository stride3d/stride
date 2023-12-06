using BepuPhysicIntegrationTest.Integration.Components.Containers;
using BepuPhysicIntegrationTest.Integration.Extensions;
using BepuPhysics;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace BepuPhysicIntegrationTest.Integration.Components.Utils;
public class BepuCharacterComponent : SimulationUpdateComponent
{
	public float Speed { get; set; } = 1f;
	public float JumpSpeed { get; set; } = 1f;

	[DataMemberIgnore]
	public Quaternion Orientation { get; set; }
	[DataMemberIgnore]
	public Vector3 Velocity { get; set; }

	public BodyContainerComponent? CharacterBody { get; set; }

	private BodyReference? _bodyReference;

	public override void Start()
	{
		_bodyReference = CharacterBody?.GetPhysicBody().Value;
		// prevent tipping of character while moving
		_bodyReference.Value.LocalInertia = new BodyInertia { InverseMass = 1f };

		base.Start();
	}

	public override void Update()
	{
		DebugText.Print(Input.MouseDelta.ToString(), new Int2(50, 50));
		DebugText.Print(Velocity.ToString(), new Int2(50, 75));
		DebugText.Print(Orientation.ToString(), new Int2(50, 100));
	}

	public void Move(Vector3 direction)
	{
		Velocity = direction * Speed;
	}

	public void Rotate(Quaternion rotation)
	{
		Orientation = rotation;
	}

	public override void SimulationUpdate(float simTimeStep)
	{
		var body = CharacterBody.GetPhysicBody();

		DebugText.Print(body.Value.Awake.ToString(), new Int2(50, 125));

		// probably inneficient but I needed a way to wake up the body
		// else it sleeps after a second.
		body.Value.SetLocalInertia(body.Value.LocalInertia);

		body.Value.Pose.Orientation = Orientation.ToNumericQuaternion();
		body.Value.Velocity.Linear += Velocity.ToNumericVector();
	}
}
