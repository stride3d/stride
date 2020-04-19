// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;

namespace Stride.Physics
{
    [ContentSerializer(typeof(DataContentSerializer<ConeColliderShapeDesc>))]
    [DataContract("ConeColliderShapeDesc")]
    [Display(50, "Cone")]
    public class ConeColliderShapeDesc : IInlineColliderShapeDesc
    {
        /// <userdoc>
        /// The height of the cylinder
        /// </userdoc>
        [DataMember(10)]
        [DefaultValue(1f)]
        public float Height = 1f;

        /// <userdoc>
        /// The radius of the cylinder
        /// </userdoc>
        [DataMember(20)]
        [DefaultValue(0.5f)]
        public float Radius = 0.5f;

        /// <userdoc>
        /// The orientation of the cylinder.
        /// </userdoc>
        [DataMember(30)]
        [DefaultValue(ShapeOrientation.UpY)]
        public ShapeOrientation Orientation = ShapeOrientation.UpY;

        /// <userdoc>
        /// The offset with the real graphic mesh.
        /// </userdoc>
        [DataMember(40)]
        public Vector3 LocalOffset;

        /// <userdoc>
        /// The local rotation of the collider shape.
        /// </userdoc>
        [DataMember(50)]
        public Quaternion LocalRotation = Quaternion.Identity;

        public bool Match(object obj)
        {
            var other = obj as ConeColliderShapeDesc;
            if (other == null) return false;

            return Math.Abs(other.Height - Height) < float.Epsilon &&
                   Math.Abs(other.Radius - Radius) < float.Epsilon &&
                   other.Orientation == Orientation &&
                   other.LocalOffset == LocalOffset &&
                   other.LocalRotation == LocalRotation;
        }

        public ColliderShape CreateShape()
        {
            return new ConeColliderShape(Height, Radius, Orientation) { LocalOffset = LocalOffset, LocalRotation = LocalRotation };
        }
    }
}
