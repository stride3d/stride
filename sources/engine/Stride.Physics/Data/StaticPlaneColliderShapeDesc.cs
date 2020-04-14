// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;

namespace Stride.Physics
{
    [ContentSerializer(typeof(DataContentSerializer<StaticPlaneColliderShapeDesc>))]
    [DataContract("StaticPlaneColliderShapeDesc")]
    [Display(50, "Infinite Plane")]
    public class StaticPlaneColliderShapeDesc : IInlineColliderShapeDesc
    {
        /// <userdoc>
        /// The normal of the infinite plane.
        /// </userdoc>
        [DataMember(10)]
        public Vector3 Normal = Vector3.UnitY;

        /// <userdoc>
        /// The distance offset.
        /// </userdoc>
        [DataMember(20)]
        public float Offset;

        public bool Match(object obj)
        {
            var other = obj as StaticPlaneColliderShapeDesc;
            if (other == null) return false;
            return other.Normal == Normal && Math.Abs(other.Offset - Offset) < float.Epsilon;
        }

        public ColliderShape CreateShape()
        {
            return new StaticPlaneColliderShape(Normal, Offset);
        }
    }
}
