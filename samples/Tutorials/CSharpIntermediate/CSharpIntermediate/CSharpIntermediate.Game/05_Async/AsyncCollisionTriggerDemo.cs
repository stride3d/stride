using System.Threading.Tasks;
using Stride.Engine;
using Stride.Rendering;

namespace CSharpIntermediate.Code
{
    public class AsyncCollisionTriggerDemo : AsyncScript
    {
        private PhysicsComponent triggerCollider;
        private Material yellowMaterial;
        private Material redMaterial;

        public override async Task Execute()
        {
            // Store the collider component
            triggerCollider = Entity.Get<PhysicsComponent>();
  
            //Preload some materials
            redMaterial = Content.Load<Material>("Materials/Red");
            yellowMaterial = Content.Load<Material>("Materials/Yellow");

            while (Game.IsRunning)
            {
                // Wait for an entity to collide with the trigger
                var collision = await triggerCollider.NewCollision();
                var ballCollider = triggerCollider == collision.ColliderA ? collision.ColliderB : collision.ColliderA;

                // Change the material on the entity
                ballCollider.Entity.Get<ModelComponent>().Materials[0] = yellowMaterial;

                // Wait for the entity to exit the trigger
                await collision.Ended();

                // Change the material back to the original one
                ballCollider.Entity.Get<ModelComponent>().Materials[0] = redMaterial;
            }
        }

        public override void Cancel()
        {
            Content.Unload(yellowMaterial);
            Content.Unload(redMaterial);
        }
    }
}
