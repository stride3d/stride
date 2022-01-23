using System.Threading.Tasks;
using Stride.Engine;
using Stride.Rendering;

namespace CSharpIntermediate.Code
{
    public class AsyncCollisionTriggerDemo : AsyncScript
    {
        PhysicsComponent triggerCollider;
        private Material material1;
        private Material material2;

        public override async Task Execute()
        {
            triggerCollider = Entity.Get<PhysicsComponent>();
            //triggerCollider.ProcessCollisions = true;

            material1 = Content.Load<Material>("Materials/Yellow");
            material2 = Content.Load<Material>("Materials/Green");

            while (Game.IsRunning)
            {
                // 1. Wait for an entity to collide with the trigger
                var firstCollision = await triggerCollider.NewCollision();
                var ballCollider = triggerCollider == firstCollision.ColliderA
                    ? firstCollision.ColliderB
                    : firstCollision.ColliderA;

                // 2. Change the material on the entity
                ballCollider.Entity.Get<ModelComponent>().Materials[0] = material2;

                // 3. Wait for the entity to exit the trigger
                await firstCollision.Ended();

                // 4. Change the material back to the original one
                ballCollider.Entity.Get<ModelComponent>().Materials[0] = material1;
            }
        }

        public override void Cancel()
        {
            Content.Unload(material1);
            Content.Unload(material2);
        }
    }
}
