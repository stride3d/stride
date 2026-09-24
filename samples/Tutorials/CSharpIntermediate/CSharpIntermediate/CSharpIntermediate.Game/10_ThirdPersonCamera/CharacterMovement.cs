// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.BepuPhysics;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace CSharpIntermediate.Code
{
    public class CharacterMovement : SyncScript
    {
        private CharacterComponent character;

        public override void Start()
        { 
            character = Entity.Get<CharacterComponent>();
        }

        public override void Update()
        {
            var movementDirection = Vector2.Zero;
            if (Input.IsKeyDown(Keys.W))
            {
                movementDirection.Y++;
            }
            if (Input.IsKeyDown(Keys.S))
            {
                movementDirection.Y--;
            }

            if (Input.IsKeyDown(Keys.A))
            {
                movementDirection.X++;
            }
            if (Input.IsKeyDown(Keys.D))
            {
                movementDirection.X--;
            }

            movementDirection.Normalize();
            character.MoveVector = movementDirection;
        }
    }
}
