using BepuPhysicIntegrationTest.Integration.Components.Containers;
using BepuPhysicIntegrationTest.Integration.Extensions;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace BepuPhysicIntegrationTest.Integration.Components.Utils;
public class BepuCharacterComponent : StartupScript
{
	public float Speed { get; set; } = 1f;
	public float JumpSpeed { get; set; } = 1f;

	public BodyContainerComponent? CharacterBody { get; set; }

	public override void Start()
	{
	}

	public void Move(Vector3 direction)
	{
		CharacterBody.GetPhysicBody().Value.Velocity.Linear += direction.ToNumericVector() * Speed;
	}
}
