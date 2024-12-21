// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
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
                Ball.Transform.Position = Entity.Transform.WorldMatrix.TranslationVector;
                Ball.Transform.UpdateWorldMatrix();
                
                // We have to update the physics transform since we manually positioned the ball's position 
                var physicsComponent = Ball.Get<RigidbodyComponent>();
                physicsComponent.LinearVelocity = new Vector3();
                physicsComponent.UpdatePhysicsTransformation();
            }
        }
    }
}
