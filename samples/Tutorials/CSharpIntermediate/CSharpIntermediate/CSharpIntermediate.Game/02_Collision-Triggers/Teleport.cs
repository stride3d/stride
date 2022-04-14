using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Physics;

namespace CSharpIntermediate.Code
{
    public class Teleport : SyncScript
    {
        public Entity Ball;

        public override void Start() { }

        public override void Update()
        {

            DebugText.Print("Press Space to teleport ball back in the air", new Int2(500, 180));

            if (Input.IsKeyPressed(Keys.Space))
            {
                Entity.Transform.GetWorldTransformation(out Vector3 worldPos, out Quaternion rot, out Vector3 scale);
                Ball.Transform.Position = worldPos;
                Ball.Transform.UpdateWorldMatrix();

                var physicsComponent = Ball.Get<RigidbodyComponent>();
                physicsComponent.Enabled = true;
                physicsComponent.LinearVelocity = new Vector3();
                physicsComponent.AngularVelocity = new Vector3();
                physicsComponent.UpdatePhysicsTransformation();
            }
        }
    }
}
