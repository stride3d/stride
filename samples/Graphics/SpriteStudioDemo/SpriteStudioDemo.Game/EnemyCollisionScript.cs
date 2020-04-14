// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Physics;

namespace SpriteStudioDemo
{
    public class EnemyCollisionScript : AsyncScript
    {
        public override async Task Execute()
        {
            var rigidbodyElement = Entity.Get<RigidbodyComponent>();
            var enemyScript = Entity.Get<EnemyScript>();

            while (Game.IsRunning)
            {
                var collision = await rigidbodyElement.NewCollision();

                if (collision.ColliderA.Entity.Name == "bullet" && !rigidbodyElement.IsTrigger) //if we are trigger we should ignore the bullet
                {
                    var script = collision.ColliderA.Entity.Get<BeamScript>();
                    script.Die();
                    enemyScript.Explode();
                }
                else if (collision.ColliderB.Entity.Name == "bullet" && !rigidbodyElement.IsTrigger)
                {
                    var script = collision.ColliderB.Entity.Get<BeamScript>();
                    script.Die();
                    enemyScript.Explode();
                }
            }
        }
    }
}
