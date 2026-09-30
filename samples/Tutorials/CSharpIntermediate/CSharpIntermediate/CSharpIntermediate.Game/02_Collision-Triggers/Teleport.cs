// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.BepuPhysics;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

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
                var physicsComponent = Ball.Get<BodyComponent>();
                physicsComponent.Awake = true;
                physicsComponent.LinearVelocity = Vector3.Zero;
                physicsComponent.AngularVelocity = Vector3.Zero;
                physicsComponent.Teleport(Entity.Transform.WorldMatrix.TranslationVector, Quaternion.Identity);
            }
        }
    }
}
