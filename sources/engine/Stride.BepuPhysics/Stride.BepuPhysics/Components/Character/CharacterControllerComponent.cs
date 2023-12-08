using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace Stride.BepuPhysics.Components.Character;
[ComponentCategory("Bepu - Character")]
public class CharacterControllerComponent : SyncScript
{
    public Entity? CameraPivot { get; set; }
    public CharacterComponent? Character { get; set; }

    public float MinCameraAngle { get; set; } = -90;
    public float MaxCameraAngle { get; set; } = 90;

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
        if (Input.IsKeyPressed(Keys.Tab))
        {
            if (Game.IsMouseVisible)
                Input.UnlockMousePosition();
            else
                Input.LockMousePosition(true);
            Game.IsMouseVisible = !Game.IsMouseVisible;
        }

        Move();
        Rotate();

        if (Input.IsKeyPressed(Keys.Space))
            Character?.Jump();
    }

    private void Move()
    {
        // Keyboard movement
        var moveDirection = Vector2.Zero;
        if (Input.IsKeyDown(Keys.W) || Input.IsKeyDown(Keys.Z))
            moveDirection.Y = 1;
        if (Input.IsKeyDown(Keys.S))
            moveDirection.Y -= 1;
        if (Input.IsKeyDown(Keys.A) || Input.IsKeyDown(Keys.Q))
            moveDirection.X -= 1;
        if (Input.IsKeyDown(Keys.D))
            moveDirection.X = 1;

        var velocity = new Vector3(moveDirection.X, 0, -moveDirection.Y);
        velocity.Normalize();

        velocity = Vector3.Transform(velocity, Entity.Transform.Rotation);

        if (Input.IsKeyDown(Keys.LeftShift))
            velocity *= 2f;

        Character?.Move(velocity);
    }

    private void Rotate()
    {
        var delta = Input.Mouse.Delta;

        _cameraDirection.X -= delta.Y;
        _cameraDirection.Y -= delta.X;
        _cameraDirection.X = MathUtil.Clamp(_cameraDirection.X, MinCameraAngle, MaxCameraAngle);

        Character?.Rotate(Quaternion.RotationY(_cameraDirection.Y));
        if (CameraPivot != null)
            CameraPivot.Transform.Rotation = Quaternion.RotationX(_cameraDirection.X);
    }
}
