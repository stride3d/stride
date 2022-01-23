using System.Collections.Specialized;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;

namespace CSharpIntermediate.Code
{
    public class CollisionTriggerDemo : SyncScript
    {
        PhysicsComponent triggerCollider;
        string enterStatus = "";
        string exitStatus = "";

        public override void Start()
        {
            // Retrieve the Physics component of the current entity
            triggerCollider = Entity.Get<PhysicsComponent>();

            // When the 'CollectionChanged' event occurs, execute the CollisionsChanged method
            triggerCollider.Collisions.CollectionChanged += CollisionsChanged;
        }

        private void CollisionsChanged(object sender, TrackingCollectionChangedEventArgs args)
        {
            // Cast the argument 'item' to a collision object
            var collision = (Collision)args.Item;

            // We need to make sure which collision object is not the Trigger collider
            // We perform a little check to find the ballCollider 
            var ballCollider = triggerCollider == collision.ColliderA ? collision.ColliderB : collision.ColliderA;

            if (args.Action == NotifyCollectionChangedAction.Add)
            {
                // When a collision has been added to the collision collection, we know an object 'entered' our trigger
                enterStatus = ballCollider.Entity.Name + " entered " + triggerCollider.Entity.Name;
                exitStatus = "";
            }
            else if (args.Action == NotifyCollectionChangedAction.Remove)
            {
                // When a collision has been removed fromthe collision collection, we know an object 'left' our trigger
                enterStatus = "";
                exitStatus = ballCollider.Entity.Name + " left " + triggerCollider.Entity.Name;
            }
        }

        public override void Update()
        {
            // the trigger collider can have 0, 1 or multiple collision going on in a single frame
            foreach (var collision in triggerCollider.Collisions)
            {
                DebugText.Print("ColliderA: " + collision.ColliderA.Entity.Name, new Int2(500, 300));
                DebugText.Print("ColliderB: " + collision.ColliderB.Entity.Name, new Int2(500, 320));
            }

            DebugText.Print(enterStatus, new Int2(200, 400));
            DebugText.Print(exitStatus, new Int2(700, 400));
        }
    }
}
