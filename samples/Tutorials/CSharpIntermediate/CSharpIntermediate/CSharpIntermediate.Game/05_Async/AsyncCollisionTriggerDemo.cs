// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Stride.Engine;
using Stride.Physics;
using Stride.Rendering;

namespace CSharpIntermediate.Code
{
    public class AsyncCollisionTriggerDemo : AsyncScript
    {
        private Material yellowMaterial;
        private Material redMaterial;

        public override async Task Execute()
        {
            // Store the collider component
            var staticCollider = Entity.Get<StaticColliderComponent>();
  
            //Preload some materials
            yellowMaterial = Content.Load<Material>("Materials/Yellow");

            while (Game.IsRunning)
            {
                // Wait for an entity to collide with the trigger
                var collision = await staticCollider.NewCollision();
                var ballCollider = staticCollider == collision.ColliderA ? collision.ColliderB : collision.ColliderA;

                // Store current material
                var modelComponent = ballCollider.Entity.Get<ModelComponent>();
                var originalMaterial = modelComponent.Materials[0];

                // Change the material on the entity
                modelComponent.Materials[0] = yellowMaterial;

                // Wait for the entity to exit the trigger
                await staticCollider.CollisionEnded();

                // Alternative
                // await collision.Ended(); //This checks for the end of any collision on the actual collision object

                // Change the material back to the original one
                modelComponent.Materials[0] = originalMaterial;
            }
        }

        public override void Cancel()
        {
            Content.Unload(yellowMaterial);
            Content.Unload(redMaterial);
        }
    }
}
