// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization.Contents;
using Xenko.Graphics;

namespace Xenko.Physics
{
    [ContentSerializer(typeof(DataContentSerializer<HeightfieldColliderShapeDesc>))]
    [DataContract("HeightfieldColliderShapeDesc")]
    [Display(500, "Heightfield")]
    public class HeightfieldColliderShapeDesc : IAssetColliderShapeDesc
    {
        [Display(Browsable = false)]
        [DataMember(10)]
        public List<float> FloatHeights;

        [Display(Browsable = false)]
        [DataMember(20)]
        public List<short> ShortHeights;

        [Display(Browsable = false)]
        [DataMember(30)]
        public List<byte> ByteHeights;

        [DataMember(40)]
        public Texture Texture;

        /// <userdoc>
        /// The offset with the real graphic mesh.
        /// </userdoc>
        [DataMember(41)]
        public Vector3 LocalOffset;

        /// <userdoc>
        /// The local rotation of the collider shape.
        /// </userdoc>
        [DataMember(42)]
        public Quaternion LocalRotation = Quaternion.Identity;

        [DataMember(50)]
        public int HeightStickWidth = 33;

        [DataMember(60)]
        public int HeightStickLength = 33;

        [DataMember(70)]
        public HeightfieldTypes HeightType = HeightfieldTypes.Float;

        [Display(Browsable = false)]
        [DataMember(80)]
        public float HeightScale = 1f;

        [Display(Browsable = false)]
        [DataMember(90)]
        public float MinHeight = -5f;

        [Display(Browsable = false)]
        [DataMember(100)]
        public float MaxHeight = 5f;

        [DataMember(110)]
        public bool FlipQuadEdges = false;

        [DataMember(105)]
        public float HeightRange = 10f;

        public bool Match(object obj)
        {
            var other = obj as HeightfieldColliderShapeDesc;

            if (other == null)
            {
                return false;
            }

            if (other.LocalOffset != LocalOffset || other.LocalRotation != LocalRotation)
                return false;

            return other.Texture == Texture
                && other.HeightStickWidth == HeightStickWidth
                && other.HeightStickLength == HeightStickLength
                && other.HeightType == HeightType
                && Math.Abs(other.HeightScale - HeightScale) < float.Epsilon
                && Math.Abs(other.MinHeight - MinHeight) < float.Epsilon
                && Math.Abs(other.MaxHeight - MaxHeight) < float.Epsilon
                && other.FlipQuadEdges == FlipQuadEdges
                && Math.Abs(other.HeightRange - HeightRange) < float.Epsilon;
        }

        public bool IsValid()
        {
            return HeightStickWidth > 1
                && HeightStickLength > 1
                && HeightRange > 0
                && MaxHeight > MinHeight
                && Math.Abs((MaxHeight - MinHeight) - HeightRange) < float.Epsilon;
        }
    }
}
