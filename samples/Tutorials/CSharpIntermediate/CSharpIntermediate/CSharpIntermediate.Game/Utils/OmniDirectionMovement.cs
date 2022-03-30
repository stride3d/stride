using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace CSharpIntermediate.Code
{
    public class OmniDirectionMovement : SyncScript
    {
        [DataMember(1)]
        public float MoveSpeed = 3.0f;

        [DataMember(2)]
        public bool Vertical = true;

        [DataMember(3)]
        public bool Horizontal = false;

        [DataMember(4)]
        public bool Forward = false;

        public override void Start()
        {
        }

        public override void Update()
        {
            var movement = new Vector3(0);
            if (Input.IsKeyDown(Keys.Q) && Vertical)
            {
                movement.Y += 1;
            }
            if (Input.IsKeyDown(Keys.E) && Vertical)
            {
                movement.Y -= 1;
            }

            if (Input.IsKeyDown(Keys.W) && Forward)
            {
                movement.Z += 1;
            }
            if (Input.IsKeyDown(Keys.S) && Forward)
            {
                movement.Z -= 1;
            }

            if (Input.IsKeyDown(Keys.A) && Horizontal)
            {
                movement.X += 1;
            }
            if (Input.IsKeyDown(Keys.D) && Horizontal)
            {
                movement.X -= 1;
            }

            var delta = (float)Game.UpdateTime.Elapsed.TotalSeconds;
            Entity.Transform.Position += movement * delta * MoveSpeed;
        }
    }
}
