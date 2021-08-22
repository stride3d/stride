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


        public bool Equals(ContactPoint other)
        {
            return ((ColliderA == other.ColliderA && ColliderB == other.ColliderB)
                    || (ColliderA == other.ColliderB && ColliderB == other.ColliderA))
                   && Distance == other.Distance
                   && Normal == other.Normal
                   && PositionOnA == other.PositionOnA
                   && PositionOnB == other.PositionOnB;
        }


        public override bool Equals(object obj) => obj is ContactPoint other && Equals(other);


        public override int GetHashCode()
        {
            return HashCode.Combine(ColliderA, ColliderB, Distance, Normal, PositionOnA, PositionOnB);
        }
    }
}