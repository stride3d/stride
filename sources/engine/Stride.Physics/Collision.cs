// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.MicroThreading;
using Stride.Core.Threading;
using Stride.Engine;

namespace Stride.Physics
{
    public class Collision
    {
        private static readonly Queue<Channel<ContactPoint>> ChannelsPool = new Queue<Channel<ContactPoint>>();
        private bool destroyed;

        internal Collision()
        {
        }

        public void Initialize(PhysicsComponent colliderA, PhysicsComponent colliderB)
        {
            ColliderA = colliderA;
            ColliderB = colliderB;

            NewContactChannel = ChannelsPool.Count == 0 ? new Channel<ContactPoint> { Preference = ChannelPreference.PreferSender } : ChannelsPool.Dequeue();
            ContactUpdateChannel = ChannelsPool.Count == 0 ? new Channel<ContactPoint> { Preference = ChannelPreference.PreferSender } : ChannelsPool.Dequeue();
            ContactEndedChannel = ChannelsPool.Count == 0 ? new Channel<ContactPoint> { Preference = ChannelPreference.PreferSender } : ChannelsPool.Dequeue();
        }

        internal void Destroy()
        {
            // Because we raise CollisionEnded for removed components right before
            // Simulation.BeginContactTesting() cleans up the ended collisions
            // we need to retain the collider information for an additional frame.
            // The collision has been removed from PhysicsComponent.Collisions
            // and the user should not store the collision object between frames.
            if (!HasEndedFromComponentRemoval)
            {
                ColliderA = null;
                ColliderB = null;
            }
            NewContactChannel.Reset();
            ContactUpdateChannel.Reset();
            ContactEndedChannel.Reset();
            ChannelsPool.Enqueue(NewContactChannel);
            ChannelsPool.Enqueue(ContactUpdateChannel);
            ChannelsPool.Enqueue(ContactEndedChannel);
            Contacts.Clear();

            destroyed = true;
        }

        public PhysicsComponent ColliderA { get; private set; }

        public PhysicsComponent ColliderB { get; private set; }

        public HashSet<ContactPoint> Contacts = new HashSet<ContactPoint>(ContactPointEqualityComparer.Default);

        /// <summary>
        /// True if the collision has ended because one of the colliders has been removed,
        /// either by removing the entity from the scene or by removing physics component
        /// from the entity.
        /// </summary>
        /// <remarks>
        /// If true, it is not safe to invoke further actions on the colliders.
        /// Only use colliders information to identify the entity that has been removed.
        /// </remarks>
        public bool HasEndedFromComponentRemoval { get; internal set; }

        internal Channel<ContactPoint> NewContactChannel;

        public ChannelMicroThreadAwaiter<ContactPoint> NewContact()
        {
            if (destroyed)
                throw new InvalidOperationException("The collision object has been destroyed.");

            return NewContactChannel.Receive();
        }

        internal Channel<ContactPoint> ContactUpdateChannel;

        public ChannelMicroThreadAwaiter<ContactPoint> ContactUpdate()
        {
            if (destroyed)
                throw new InvalidOperationException("The collision object has been destroyed.");

            return ContactUpdateChannel.Receive();
        }

        internal Channel<ContactPoint> ContactEndedChannel;

        public ChannelMicroThreadAwaiter<ContactPoint> ContactEnded()
        {
            if (destroyed)
                throw new InvalidOperationException("The collision object has been destroyed.");

            return ContactEndedChannel.Receive();
        }

        public async Task Ended()
        {
            if (destroyed)
                throw new InvalidOperationException("The collision object has been destroyed.");

            Collision endCollision;
            do
            {
                endCollision = await ColliderA.CollisionEnded();
            }
            while (!endCollision.Equals(this));
        }

        public override bool Equals(object obj)
        {
            var other = (Collision)obj;
            return other != null && ((other.ColliderA == ColliderA && other.ColliderB == ColliderB) || (other.ColliderB == ColliderA && other.ColliderA == ColliderB));
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var result = ColliderA?.GetHashCode() ?? 0;
                result = (result * 397) ^ (ColliderB?.GetHashCode() ?? 0);
                return result;
            }
        }

        internal bool InternalEquals(PhysicsComponent a, PhysicsComponent b)
        {
            return (ColliderA == a && ColliderB == b) || (ColliderB == a && ColliderA == b);
        }
    }
}
