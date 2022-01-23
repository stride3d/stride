using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace CSharpIntermediate.Code
{
    public class UpDownMovement : SyncScript
    {
        
        private float MoveSpeed = 4.0f;

        public override void Start()
        {
        }

        public override void Update()
        {
            var velocity = new Vector3(0);
            if (Input.IsKeyDown(Keys.W))
            {
                velocity.Y += 1;
            }
            if (Input.IsKeyDown(Keys.S))
            {
                velocity.Y -= 1;
            }

            var delta = (float)Game.UpdateTime.Elapsed.TotalSeconds;
            Entity.Transform.Position += velocity * delta * MoveSpeed;
        }
    }
}
