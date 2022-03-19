using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Physics;

namespace CSharpIntermediate.Code
{
    public class CharacterMovement : SyncScript
    {
        public Vector3 MovementMultiplier = new Vector3(2, 0, 3);

        private Entity cameraEntity;
        private CharacterComponent character;

        public override void Start()
        {
            cameraEntity = Entity.FindChild("Camera");
            character = Entity.Get<CharacterComponent>();
        }

        public override void Update()
        {
            var velocity = new Vector3(0);
            if (Input.IsKeyDown(Keys.W))
            {
                velocity.Z++;
            }
            if (Input.IsKeyDown(Keys.S))
            {
                velocity.Z--;
            }

            if (Input.IsKeyDown(Keys.A))
            {
                velocity.X++;
            }
            if (Input.IsKeyDown(Keys.D))
            {
                velocity.X--;
            }

            velocity *= MovementMultiplier;
            velocity = Vector3.Transform(velocity, Entity.Transform.Rotation);

            character.SetVelocity(velocity);
        }
    }
}
