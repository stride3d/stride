// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.MicroThreading;
using Stride.Engine;

namespace Stride.Physics
{
    /// <summary>
    /// A pair of component colliding with each other.
    /// Pair of [b,a] is considered equal to [a,b].
    /// </summary>
    public readonly struct Collision : IEquatable<Collision>
    {
        public readonly PhysicsComponent ColliderA;

        public readonly PhysicsComponent ColliderB;

        /// <summary>
        /// True if the collision has ended because one of the colliders has been removed,
        /// either by removing the entity from the scene or by removing physics component
        /// from the entity.
        /// </summary>
        /// <remarks>
        /// If true, it is not safe to invoke further actions on the colliders.
        /// Only use colliders information to identify the entity that has been removed.
        /// </remarks>
        public readonly bool HasEndedFromComponentRemoval => ColliderA.Simulation.EndedFromComponentRemoval.Contains(this);

        public readonly bool AreColliding => ColliderA.Simulation.CurrentCollisions.Contains(this);

        public readonly HashSet<ContactPoint> Contacts => ColliderA.Simulation.LatestContactPointsFor(this);

        internal Collision(PhysicsComponent a, PhysicsComponent b)
        {
            ColliderA = a;
            ColliderB = b;
        }

        /// <summary>
        /// The returned collection contains the previous contacts, new contacts are under <see cref="Contacts"/>
        /// </summary>
        public ChannelMicroThreadAwaiter<HashSet<ContactPoint>> ContactChanged()
        {
            return ColliderA.Simulation.ContactChanged(this);
        }

        public async Task Ended()
        {
            if (ColliderA.Simulation.CurrentCollisions.Contains(this) == false)
                throw new InvalidOperationException("The collision object has been destroyed.");

            Collision endCollision;
            do
            {
                endCollision = await ColliderA.CollisionEnded();
            } while (!endCollision.Equals(this));
        }

        public static bool operator ==(in Collision a, in Collision b)
        {
            return (Equals(a.ColliderA, b.ColliderA) && Equals(a.ColliderB, b.ColliderB))
                   || (Equals(a.ColliderB, b.ColliderA) && Equals(a.ColliderA, b.ColliderB));
        }

        public static bool operator !=(in Collision a, in Collision b) => (a == b) == false;

        public override bool Equals(object obj)
        {
            return obj is Collision other && Equals(other);
        }

        public bool Equals(Collision other) => this == other;

        public override int GetHashCode()
        {
            int aH = ColliderA.GetHashCode();
            int bH = ColliderB.GetHashCode();
            // This ensures that a pair of components will return the same hash regardless
            // of if they are setup as [b,a] or [a,b]
            return aH > bH ? HashCode.Combine(aH, bH) : HashCode.Combine(bH, aH);
        }
    }
}