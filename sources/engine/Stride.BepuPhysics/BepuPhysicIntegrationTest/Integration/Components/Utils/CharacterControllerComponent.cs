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

	private Vector3 _cameraDirection;

	public override void Start()
	{
		Input.LockMousePosition(true);
		Game.IsMouseVisible = false;
	}

	public override void Update()
	{
		if(Input.IsKeyPressed(Keys.Escape))
		{
			Input.UnlockMousePosition();
			Game.IsMouseVisible = true;
		}

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
		Rotate();
	}

	private void Rotate()
	{
		var delta = Input.Mouse.Delta;

		_cameraDirection.X -= delta.X;
		_cameraDirection.Y -= delta.Y;

		Character.Rotate(Quaternion.RotationY(_cameraDirection.X));
	}
}
