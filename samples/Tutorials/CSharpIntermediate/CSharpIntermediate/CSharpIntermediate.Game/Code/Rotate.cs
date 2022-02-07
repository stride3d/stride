using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace CSharpIntermediate.Code
{
    public class Rotate : SyncScript
    {
        private float RotationSpeed = 1.5f;

        public override void Start()
        {
        }

        public override void Update()
        {
            var rotation = 0;
            if (Input.IsKeyDown(Keys.R))
            {
                rotation += 1;
            }
            if (Input.IsKeyDown(Keys.T))
            {
                rotation -= 1;
            }


            var deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;

            Entity.Transform.Rotation *= Quaternion.RotationY(deltaTime * rotation * RotationSpeed);
        }
    }
}
