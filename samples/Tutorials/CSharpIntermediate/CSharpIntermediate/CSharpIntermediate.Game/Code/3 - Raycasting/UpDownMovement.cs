using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace CSharpIntermediate.Code
{
    public class UpDownMovement : SyncScript
    {
        private float MoveSpeed = 3.0f;

        public override void Start()
        {
        }

        public override void Update()
        {
            var velocity = new Vector3(0);
            if (Input.IsKeyDown(Keys.Q))
            {
                velocity.Y += 1;
            }
            if (Input.IsKeyDown(Keys.E))
            {
                velocity.Y -= 1;
            }

            if (Input.IsKeyDown(Keys.W))
            {
                velocity.Z += 1;
            }
            if (Input.IsKeyDown(Keys.S))
            {
                velocity.Z -= 1;
            }

            if (Input.IsKeyDown(Keys.A))
            {
                velocity.X -= 1;
            }
            if (Input.IsKeyDown(Keys.D))
            {
                velocity.X += 1;
            }

            var delta = (float)Game.UpdateTime.Elapsed.TotalSeconds;
            Entity.Transform.Position += velocity * delta * MoveSpeed;
        }
    }
}
