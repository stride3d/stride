// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.Physics
{
    public struct ContactPoint : IEquatable<ContactPoint>
    {
        public PhysicsComponent ColliderA;
        public PhysicsComponent ColliderB;
        public float Distance;
        public Vector3 Normal;
        public Vector3 PositionOnA;
        public Vector3 PositionOnB;

        /// <summary>
        /// The normal impulse applied to resolve a collision between two bodies
        /// </summary>
        public float AppliedImpulse;

        /// <summary>
        /// Tangential impulse to contact point of <see cref="AppliedImpulse"/>
        /// </summary>
        public float AppliedImpulseLateral1;

        /// <summary>
        /// Perpendicular impulse to both <see cref="AppliedImpulse"/> and <see cref="AppliedImpulseLateral1"/>
        /// </summary>
        public float AppliedImpulseLateral2;


        public bool Equals(ContactPoint other)
        {
            return ((ColliderA == other.ColliderA && ColliderB == other.ColliderB)
                    || (ColliderA == other.ColliderB && ColliderB == other.ColliderA))
                   && Distance == other.Distance
                   && Normal == other.Normal
                   && PositionOnA == other.PositionOnA
                   && PositionOnB == other.PositionOnB
                   && AppliedImpulse == other.AppliedImpulse
                   && AppliedImpulseLateral1 == other.AppliedImpulseLateral1
                   && AppliedImpulseLateral2 == other.AppliedImpulseLateral2;
        }


        public override bool Equals(object obj) => obj is ContactPoint other && Equals(other);


        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(ColliderA);
            hash.Add(ColliderB);
            hash.Add(Distance);
            hash.Add(Normal);
            hash.Add(PositionOnA);
            hash.Add(PositionOnB);
            hash.Add(AppliedImpulse);
            hash.Add(AppliedImpulseLateral1);
            hash.Add(AppliedImpulseLateral2);
            return hash.ToHashCode();
        }
    }
}
