// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;
using Xenko.Particles.DebugDraw;

namespace Xenko.Particles.Updaters.FieldShapes
{
    [DataContract("FieldShapeCube")]
    public class Cube : FieldShape
    {
        public override DebugDrawShape GetDebugDrawShape(out Vector3 pos, out Quaternion rot, out Vector3 scl)
        {
            pos = Vector3.Zero;
            rot = Quaternion.Identity;
            scl = new Vector3(halfSideX * 2, halfSideY * 2, halfSideZ * 2);
            return DebugDrawShape.Cube;
        }

        [DataMemberIgnore]
        private Vector3 fieldPosition;

        [DataMemberIgnore]
        private Quaternion fieldRotation;

        [DataMemberIgnore]
        private Quaternion inverseRotation;

        [DataMemberIgnore]
        private Vector3 fieldSize;

        [DataMemberIgnore]
        private Vector3 mainAxis;


        /// <summary>
        /// The maximum distance from the origin along the X axis. The X side is twice as big.
        /// </summary>
        /// <userdoc>
        /// The maximum distance from the origin along the X axis. The X side is twice as big.
        /// </userdoc>
        [DataMember(10)]
        [DataMemberRange(0, 3)]
        [Display("Half X")]
        public float HalfSideX
        {
            get { return halfSideX > MathUtil.ZeroTolerance ? halfSideX : 0; }
            set { halfSideX = value > MathUtil.ZeroTolerance ? value : MathUtil.ZeroTolerance; }
        }
        private float halfSideX = 1f;

        /// <summary>
        /// The maximum distance from the origin along the Y axis. The Y side is twice as big.
        /// </summary>
        /// <userdoc>
        /// The maximum distance from the origin along the Y axis. The Y side is twice as big.
        /// </userdoc>
        [DataMember(20)]
        [DataMemberRange(0, 3)]
        [Display("Half Y")]
        public float HalfSideY
        {
            get { return halfSideY > MathUtil.ZeroTolerance ? halfSideY : 0; }
            set { halfSideY = value > MathUtil.ZeroTolerance ? value : MathUtil.ZeroTolerance; }
        }
        private float halfSideY = 1f;

        /// <summary>
        /// The maximum distance from the origin along the Z axis. The Z side is twice as big.
        /// </summary>
        /// <userdoc>
        /// The maximum distance from the origin along the Z axis. The Z side is twice as big.
        /// </userdoc>
        [DataMember(30)]
        [DataMemberRange(0, 3)]
        [Display("Half Z")]
        public float HalfSideZ
        {
            get { return halfSideZ > MathUtil.ZeroTolerance ? halfSideZ : 0; }
            set { halfSideZ = value > MathUtil.ZeroTolerance ? value : MathUtil.ZeroTolerance; }
        }
        private float halfSideZ = 1f;

        public override void PreUpdateField(Vector3 position, Quaternion rotation, Vector3 size)
        {
            this.fieldSize = size;
            this.fieldPosition = position;
            this.fieldRotation = rotation;
            inverseRotation = new Quaternion(-rotation.X, -rotation.Y, -rotation.Z, rotation.W);

            mainAxis = new Vector3(0, 1, 0);
            rotation.Rotate(ref mainAxis);
        }

        public override float GetDistanceToCenter(
            Vector3 particlePosition, Vector3 particleVelocity,
            out Vector3 alongAxis, out Vector3 aroundAxis, out Vector3 awayAxis)
        {
            // Along - following the main axis
            alongAxis = mainAxis;

            // Toward - tawards the main axis
            awayAxis = particlePosition - fieldPosition;
            awayAxis.Normalize();

            // Around - around the main axis, following the right hand rule
            aroundAxis = Vector3.Cross(alongAxis, awayAxis);

            particlePosition -= fieldPosition;
            var inverseRotation = new Quaternion(-fieldRotation.X, -fieldRotation.Y, -fieldRotation.Z, fieldRotation.W);
            inverseRotation.Rotate(ref particlePosition);
            particlePosition /= fieldSize;

            // Start of code for Cube
            var maxDist = Math.Max(Math.Abs(particlePosition.X) / halfSideX, Math.Abs(particlePosition.Y) / halfSideY);
            maxDist = Math.Max(maxDist, Math.Abs(particlePosition.Z) / halfSideZ);
            // End of code for Cube

            return maxDist;
        }

        public override bool IsPointInside(Vector3 particlePosition, out Vector3 surfacePoint, out Vector3 surfaceNormal)
        {
            particlePosition -= fieldPosition;
            inverseRotation.Rotate(ref particlePosition);
//            particlePosition /= fieldSize;

            var halfSize = fieldSize;
            halfSize.X *= halfSideX;
            halfSize.Y *= halfSideY;
            halfSize.Z *= halfSideZ;

            var isOutside = (Math.Abs(particlePosition.X) > halfSize.X) || (Math.Abs(particlePosition.Y) > halfSize.Y) || (Math.Abs(particlePosition.Z) > halfSize.Z);

            var surfaceX = particlePosition.X >= 0 ? halfSize.X : -halfSize.X;
            var surfaceY = particlePosition.Y >= 0 ? halfSize.Y : -halfSize.Y;
            var surfaceZ = particlePosition.Z >= 0 ? halfSize.Z : -halfSize.Z;

            var distX = Math.Abs(particlePosition.X - surfaceX);
            var distY = Math.Abs(particlePosition.Y - surfaceY);
            var distZ = Math.Abs(particlePosition.Z - surfaceZ);

            surfacePoint = particlePosition;

            if ((distX <= distY) && (distX <= distZ))
            {
                // Biggest distance is on the X axis
                surfacePoint.X = surfaceX;
                surfaceNormal = new Vector3(surfacePoint.X, 0, 0);
            }
            else
            if (distY <= distZ)
            {
                // Biggest distance is on the Y axis
                surfacePoint.Y = surfaceY;
                surfaceNormal = new Vector3(0, surfacePoint.Y, 0);
            }
            else
            {
                // Biggest distance is on the Z axis
                surfacePoint.Z = surfaceZ;
                surfaceNormal = new Vector3(0, 0, surfacePoint.Z);
            }

            if (isOutside)
            {
                surfacePoint.X = Math.Min(surfacePoint.X, halfSize.X);
                surfacePoint.X = Math.Max(surfacePoint.X, -halfSize.X);

                surfacePoint.Y = Math.Min(surfacePoint.Y, halfSize.Y);
                surfacePoint.Y = Math.Max(surfacePoint.Y, -halfSize.Y);

                surfacePoint.Z = Math.Min(surfacePoint.Z, halfSize.Z);
                surfacePoint.Z = Math.Max(surfacePoint.Z, -halfSize.Z);
            }

            // Fix the surface point and normal to world space
            fieldRotation.Rotate(ref surfaceNormal);
            surfaceNormal.Normalize();

            fieldRotation.Rotate(ref surfacePoint);
//            surfacePoint *= fieldSize;
            surfacePoint += fieldPosition;

            return !isOutside;
        }

    }
}
