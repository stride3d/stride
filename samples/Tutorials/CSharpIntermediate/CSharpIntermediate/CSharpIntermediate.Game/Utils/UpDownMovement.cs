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
            var movement = new Vector3(0);
            if (Input.IsKeyDown(Keys.Q))
            {
                movement.Y -= 1;
            }
            if (Input.IsKeyDown(Keys.E))
            {
                movement.Y += 1;
            }

            var delta = (float)Game.UpdateTime.Elapsed.TotalSeconds;
            Entity.Transform.Position += movement * delta * MoveSpeed;
        }
    }
}
