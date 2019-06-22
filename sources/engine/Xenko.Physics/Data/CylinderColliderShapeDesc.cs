// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization.Contents;

namespace Xenko.Physics
{
    [ContentSerializer(typeof(DataContentSerializer<CylinderColliderShapeDesc>))]
    [DataContract("CylinderColliderShapeDesc")]
    [Display(50, "Cylinder")]
    public class CylinderColliderShapeDesc : IInlineColliderShapeDesc
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
            var other = obj as CylinderColliderShapeDesc;
            if (other == null)
                return false;

            return Math.Abs(other.Height - Height) < float.Epsilon &&
                   Math.Abs(other.Radius - Radius) < float.Epsilon &&
                   other.Orientation == Orientation &&
                   other.LocalOffset == LocalOffset &&
                   other.LocalRotation == LocalRotation;
        }

        public ColliderShape CreateShape()
        {
            return new CylinderColliderShape(Height, Radius, Orientation) { LocalOffset = LocalOffset, LocalRotation = LocalRotation };
        }
    }
}
