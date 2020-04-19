// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Engine;
using Stride.Physics;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Engine.Events;

namespace PhysicsSample
{
    public class Trigger : AsyncScript
    {
        [DataMemberIgnore]
        public EventKey<bool> TriggerEvent = new EventKey<bool>();

        public override async Task Execute()
        {
            var trigger = Entity.Get<PhysicsComponent>();
            trigger.ProcessCollisions = true;

            while (Game.IsRunning)
            {
                // Wait for the next collision event
                var firstCollision = await trigger.NewCollision();

                // Filter collisions based on collision groups
                var filterAhitB = ((int)firstCollision.ColliderA.CanCollideWith) & ((int)firstCollision.ColliderB.CollisionGroup);
                var filterBhitA = ((int)firstCollision.ColliderB.CanCollideWith) & ((int)firstCollision.ColliderA.CollisionGroup);
                if (filterAhitB == 0 || filterBhitA == 0)
                    continue;

                // Broadcast the collision start event
                TriggerEvent.Broadcast(true);

                // Wait for the collision to end and broadcast that event
                Func<Task> collisionEndTask = async () =>
                {
                    Collision collision;
                    do
                    {
                        collision = await trigger.CollisionEnded();
                    } while (collision != firstCollision);

                    TriggerEvent.Broadcast(false);
                };

                Script.AddTask(collisionEndTask);
            }
        }
    }
}
