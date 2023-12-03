using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BepuPhysicIntegrationTest.Integration.Components.Utils;
public class CharacterControllerComponent : SyncScript
{
	public CameraComponent? Camera { get; set; }
	public BepuCharacterComponent? Character { get; set; }

	public override void Start()
	{
	}

	public override void Update()
	{
		// Keyboard movement
		var moveDirection = Vector2.Zero;
		if (Input.IsKeyDown(Keys.W))
			moveDirection.Y = 1;
		if (Input.IsKeyDown(Keys.S))
			moveDirection.Y -= 1;
		if (Input.IsKeyDown(Keys.A))
			moveDirection.X -= 1;
		if (Input.IsKeyDown(Keys.D))
			moveDirection.X = 1;

		var velocity = new Vector3(moveDirection.X, 0, -moveDirection.Y);
		velocity.Normalize();

		velocity = Vector3.Transform(velocity, Camera.Entity.Transform.Rotation);
		Character.Move(velocity);
	}
}
