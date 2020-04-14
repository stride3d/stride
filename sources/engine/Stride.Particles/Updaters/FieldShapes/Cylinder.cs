// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Particles.DebugDraw;

namespace Stride.Particles.Updaters.FieldShapes
{
    [DataContract("FieldShapeCylinder")]
    public class Cylinder : FieldShape
    {
        public override DebugDrawShape GetDebugDrawShape(out Vector3 pos, out Quaternion rot, out Vector3 scl)
        {
            pos = Vector3.Zero;
            rot = Quaternion.Identity;
            scl = new Vector3(radius * 2, halfHeight * 2, radius * 2);
            return DebugDrawShape.Cylinder;
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
        /// The maximum distance from the origin along the Y axis. The height is twice as big.
        /// </summary>
        /// <userdoc>
        /// The maximum distance from the origin along the Y axis. The height is twice as big.
        /// </userdoc>
        [DataMember(10)]
        [Display("Half height")]
        public float HalfHeight
        {
            get { return halfHeight > MathUtil.ZeroTolerance ? halfHeight : 0; }
            set { halfHeight = value > MathUtil.ZeroTolerance ? value : MathUtil.ZeroTolerance; }
        }
        private float halfHeight = 1f;

        /// <summary>
        /// The maximum distance from the central axis.
        /// </summary>
        /// <userdoc>
        /// The maximum distance from the central axis.
        /// </userdoc>
        [DataMember(20)]
        [Display("Radius")]
        public float Radius
        {
            get { return radius > MathUtil.ZeroTolerance ? radius : 0; }
            set { radius = value > MathUtil.ZeroTolerance ? value : MathUtil.ZeroTolerance; }
        }
        private float radius = 1f;


        public override void PreUpdateField(Vector3 position, Quaternion rotation, Vector3 size)
        {
            fieldSize = size;
            fieldPosition = position;
            fieldRotation = rotation;
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
            awayAxis.Y = 0; // In case of cylinder the away vector should be flat (away from the axis rather than just a point)
            awayAxis.Normalize();

            // Around - around the main axis, following the right hand rule
            aroundAxis = Vector3.Cross(alongAxis, awayAxis);

            particlePosition -= fieldPosition;
            inverseRotation.Rotate(ref particlePosition);
            particlePosition /= fieldSize;

            // Start of code for Cylinder
            if (Math.Abs(particlePosition.Y) >= halfHeight)
                return 1;

            particlePosition.Y = 0;

            particlePosition.X /= radius;
            particlePosition.Z /= radius;

            var maxDist = particlePosition.Length();
            // End of code for Cylinder

            return maxDist;
        }

        public override bool IsPointInside(Vector3 particlePosition, out Vector3 surfacePoint, out Vector3 surfaceNormal)
        {
            particlePosition -= fieldPosition;
            inverseRotation.Rotate(ref particlePosition);
//            particlePosition /= fieldSize;

            var maxDist = (float)Math.Sqrt(particlePosition.X * particlePosition.X + particlePosition.Z * particlePosition.Z);

            var fieldX = radius * fieldSize.X;
            var fieldY = halfHeight * fieldSize.Y;
            var fieldZ = radius * fieldSize.Z;

            var roundSurface = particlePosition;
            roundSurface.Y = 0;
            roundSurface.X /= fieldX;
            roundSurface.Z /= fieldZ;
            roundSurface.Normalize();
            roundSurface.X *= fieldX;
            roundSurface.Z *= fieldZ;

            var fieldRadius = roundSurface.Length();

            var isOutside = (maxDist > fieldRadius) || (Math.Abs(particlePosition.Y) > fieldY);

            var surfaceY = particlePosition.Y >= 0 ? fieldY : -fieldY;

            var distR = Math.Abs(maxDist - fieldRadius);
            var distY = Math.Abs(particlePosition.Y - surfaceY);

            if (distR <= distY)
            {
                surfacePoint = roundSurface;

                surfaceNormal = surfacePoint;
                surfaceNormal.X /= fieldX;
                surfaceNormal.Z /= fieldZ;

                surfacePoint.Y = particlePosition.Y;
            }
            else
            {
                // Biggest distance is on the X axis
                surfacePoint.X = particlePosition.X;
                surfacePoint.Y = 0;
                surfacePoint.Z = particlePosition.Z;

                surfacePoint.Y = surfaceY;

                surfaceNormal = surfaceY > 0 ? new Vector3(0, 1, 0) : new Vector3(0, -1, 0);
            }

            if (isOutside)
            {
                surfacePoint.Y = Math.Min(surfacePoint.Y, fieldY);
                surfacePoint.Y = Math.Max(surfacePoint.Y, -fieldY);

                if (Math.Abs(surfacePoint.X) > Math.Abs(roundSurface.X))
                    surfacePoint.X = roundSurface.X;

                if (Math.Abs(surfacePoint.Z) > Math.Abs(roundSurface.Z))
                    surfacePoint.Z = roundSurface.Z;
            }

            // Fix the surface point and normal to world space
            surfaceNormal /= fieldSize;
            fieldRotation.Rotate(ref surfaceNormal);
            surfaceNormal.Normalize();
   
            fieldRotation.Rotate(ref surfacePoint);
//            surfacePoint *= fieldSize;
            surfacePoint += fieldPosition;

            // Is the point inside the cylinder?
            return !isOutside;
        }
    }
}
