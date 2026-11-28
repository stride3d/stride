// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Contacts;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace CSharpIntermediate.Code
{
    public class CollisionTriggerDemo : SyncScript, IContactHandler
    {
        StaticComponent staticCollider;
        string collisionStatus = "";
        readonly HashSet<CollidableComponent> collidingComponents = [];

        bool IContactHandler.NoContactResponse => true;

        public override void Start()
        {
            // Retrieve the Physics component of the current entity
            staticCollider = Entity.Get<StaticComponent>();
            staticCollider.ContactEventHandler = this;
        }

        public override void Update()
        {
            // The trigger collider can have 0, 1, or multiple collision going on in a single frame
            int drawX = 500, drawY = 300;
            foreach (var otherComponent in collidingComponents)
            {
                DebugText.Print("ColliderA: " + staticCollider.Entity.Name, new Int2(drawX, drawY += 20));
                DebugText.Print("ColliderB: " + otherComponent.Entity.Name, new Int2(drawX, drawY += 20));
            }

            DebugText.Print(collisionStatus, new Int2(500, 400));
        }

        void IContactHandler.OnStartedTouching<TManifold>(Contacts<TManifold> contacts)
        {
            if (collidingComponents.Add(contacts.Other))
            {
                collisionStatus = contacts.Other.Entity.Name + " entered " + staticCollider.Entity.Name;
            }
        }

        void IContactHandler.OnStoppedTouching<TManifold>(Contacts<TManifold> contacts)
        {
            if (collidingComponents.Remove(contacts.Other))
            {
                collisionStatus = contacts.Other.Entity.Name + " left " + staticCollider.Entity.Name;
            }
        }
    }
}
