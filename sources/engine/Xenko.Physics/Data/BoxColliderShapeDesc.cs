// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization.Contents;

namespace Xenko.Physics
{
    [ContentSerializer(typeof(DataContentSerializer<BoxColliderShapeDesc>))]
    [DataContract("BoxColliderShapeDesc")]
    [Display(50, "Box")]
    public class BoxColliderShapeDesc : IInlineColliderShapeDesc
    {
        /// <userdoc>
        /// Select this if this shape will represent a Circle 2D shape
        /// </userdoc>
        [DataMember(5)]
        public bool Is2D;

        /// <userdoc>
        /// The size of one edge of the box.
        /// </userdoc>
        [DataMember(10)]
        public Vector3 Size = Vector3.One;

        /// <userdoc>
        /// The offset with the real graphic mesh.
        /// </userdoc>
        [DataMember(20)]
        public Vector3 LocalOffset;

        /// <userdoc>
        /// The local rotation of the collider shape.
        /// </userdoc>
        [DataMember(30)]
        public Quaternion LocalRotation = Quaternion.Identity;

        public bool Match(object obj)
        {
            var other = obj as BoxColliderShapeDesc;
            return other?.Is2D == Is2D && other.Size == Size && other.LocalOffset == LocalOffset && other.LocalRotation == LocalRotation;
        }

        public ColliderShape CreateShape()
        {
            return new BoxColliderShape(Is2D, Size) { LocalOffset = LocalOffset, LocalRotation = LocalRotation };
        }
    }
}
