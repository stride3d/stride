using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace BepuPhysicIntegrationTest.Integration.Components.Utils;
public class CharacterControllerComponent : SyncScript
{
	public Entity? CameraPivot{ get; set; }
	public BepuCharacterComponent? Character { get; set; }

	public float MinCameraAngle { get; set; }
	public float MaxCameraAngle { get; set; }

	private Vector3 _cameraDirection;

	public override void Start()
	{
		Input.LockMousePosition(true);
		Game.IsMouseVisible = false;

		MaxCameraAngle = MathUtil.DegreesToRadians(MaxCameraAngle);
		MinCameraAngle = MathUtil.DegreesToRadians(MinCameraAngle);
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

		velocity = Vector3.Transform(velocity, Entity.Transform.Rotation);
		Character.Move(velocity);
		Rotate();
	}

	private void Rotate()
	{
		var delta = Input.Mouse.Delta;

		_cameraDirection.X -= delta.Y;
		_cameraDirection.Y -= delta.X;
		_cameraDirection.X = MathUtil.Clamp(_cameraDirection.X, MinCameraAngle, MaxCameraAngle);

		Character.Rotate(Quaternion.RotationY(_cameraDirection.Y));
		CameraPivot.Transform.Rotation = Quaternion.RotationX(_cameraDirection.X);
	}
}
