// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;

namespace Stride.Physics
{
    [ContentSerializer(typeof(DataContentSerializer<SphereColliderShapeDesc>))]
    [DataContract("SphereColliderShapeDesc")]
    [Display(50, "Sphere")]
    public class SphereColliderShapeDesc : IInlineColliderShapeDesc
    {
        /// <userdoc>
        /// Select this if this shape will represent a Circle 2D shape
        /// </userdoc>
        [DataMember(10)]
        public bool Is2D;

        /// <userdoc>
        /// The radius of the sphere/circle.
        /// </userdoc>
        [DataMember(20)]
        [DefaultValue(0.5f)]
        public float Radius = 0.5f;

        /// <userdoc>
        /// The offset with the real graphic mesh.
        /// </userdoc>
        [DataMember(30)]
        public Vector3 LocalOffset;

        public bool Match(object obj)
        {
            var other = obj as SphereColliderShapeDesc;
            return other?.Is2D == Is2D && Math.Abs(other.Radius - Radius) < float.Epsilon && other.LocalOffset == LocalOffset;
        }

        public ColliderShape CreateShape()
        {
            return new SphereColliderShape(Is2D, Radius) { LocalOffset = LocalOffset };
        }
    }
}
