// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Specialized;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;

namespace CSharpIntermediate.Code
{
    public class CollisionTriggerDemo : SyncScript
    {
        StaticColliderComponent staticCollider;
        string collisionStatus = "";

        public override void Start()
        {
            staticCollider = Entity.Get<StaticColliderComponent>();
            staticCollider.Collisions.CollectionChanged += CollisionsChanged;
        }

        private void CollisionsChanged(object sender, TrackingCollectionChangedEventArgs args)
        {
            var collision = (Collision)args.Item;
            var ballCollider = staticCollider == collision.ColliderA ? collision.ColliderB : collision.ColliderA;

            if (args.Action == NotifyCollectionChangedAction.Add)
            {
                collisionStatus = ballCollider.Entity.Name + " entered " + staticCollider.Entity.Name;
            }
            else if (args.Action == NotifyCollectionChangedAction.Remove)
            {
                collisionStatus = ballCollider.Entity.Name + " left " + staticCollider.Entity.Name;
            }
        }

        public override void Update()
        {
            foreach (var collision in staticCollider.Collisions)
            {
                DebugText.Print("ColliderA: " + collision.ColliderA.Entity.Name, new Int2(500, 300));
                DebugText.Print("ColliderB: " + collision.ColliderB.Entity.Name, new Int2(500, 320));
            }

            DebugText.Print(collisionStatus, new Int2(500, 400));
        }
    }
}
