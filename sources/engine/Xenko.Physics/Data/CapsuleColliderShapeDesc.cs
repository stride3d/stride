// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization.Contents;

namespace Xenko.Physics
{
    [ContentSerializer(typeof(DataContentSerializer<CapsuleColliderShapeDesc>))]
    [DataContract("CapsuleColliderShapeDesc")]
    [Display(50, "Capsule")]
    public class CapsuleColliderShapeDesc : IInlineColliderShapeDesc
    {
        /// <userdoc>
        /// Select this if this shape will represent a 2D shape
        /// </userdoc>
        [DataMember(10)]
        [DefaultValue(false)]
        public bool Is2D;

        /// <userdoc>
        /// The length of the capsule (distance between the center of the two sphere centers).
        /// </userdoc>
        [DataMember(20)]
        [DefaultValue(0.5f)]
        public float Length = 0.5f;

        /// <userdoc>
        /// The radius of the capsule.
        /// </userdoc>
        [DataMember(30)]
        [DefaultValue(0.25f)]
        public float Radius = 0.25f;

        /// <userdoc>
        /// The orientation of the capsule.
        /// </userdoc>
        [DataMember(40)]
        [DefaultValue(ShapeOrientation.UpY)]
        public ShapeOrientation Orientation = ShapeOrientation.UpY;

        /// <userdoc>
        /// The offset with the real graphic mesh.
        /// </userdoc>
        [DataMember(50)]
        public Vector3 LocalOffset;

        /// <userdoc>
        /// The local rotation of the collider shape.
        /// </userdoc>
        [DataMember(60)]
        public Quaternion LocalRotation = Quaternion.Identity;

        public bool Match(object obj)
        {
            var other = obj as CapsuleColliderShapeDesc;
            return other?.Is2D == Is2D &&
                   Math.Abs(other.Length - Length) < float.Epsilon &&
                   Math.Abs(other.Radius - Radius) < float.Epsilon &&
                   other.Orientation == Orientation &&
                   other.LocalOffset == LocalOffset &&
                   other.LocalRotation == LocalRotation;
        }
    }
}
