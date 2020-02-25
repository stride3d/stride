// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;
using Xenko.Particles.DebugDraw;

namespace Xenko.Particles.Updaters.FieldShapes
{
    [DataContract("FieldShapeSphere")]
    public class Sphere : FieldShape
    {
        public override DebugDrawShape GetDebugDrawShape(out Vector3 pos, out Quaternion rot, out Vector3 scl)
        {
            pos = Vector3.Zero;
            rot = Quaternion.Identity;
            scl = new Vector3(radius * 2, radius * 2, radius * 2);
            return DebugDrawShape.Sphere;
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
        /// The maximum distance from the central point.
        /// </summary>
        /// <userdoc>
        /// The maximum distance from the central point.
        /// </userdoc>
        [DataMember(20)]
        [DataMemberRange(0f, 3)]
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
            awayAxis.Normalize();

            // Around - around the main axis, following the right hand rule
            aroundAxis = Vector3.Cross(alongAxis, awayAxis);

            particlePosition -= fieldPosition;
            inverseRotation.Rotate(ref particlePosition);
            particlePosition /= fieldSize;

            // Start of code for Sphere
            var maxDist = particlePosition.Length() / radius;
            // End of code for Sphere

            return maxDist;
        }

        public override bool IsPointInside(Vector3 particlePosition, out Vector3 surfacePoint, out Vector3 surfaceNormal)
        {
            particlePosition -= fieldPosition;
            inverseRotation.Rotate(ref particlePosition);
            particlePosition /= fieldSize;

            var maxDist = particlePosition.Length() / radius;
            if (maxDist <= MathUtil.ZeroTolerance)
            {
                surfacePoint = fieldPosition;
                surfaceNormal = new Vector3(0, 1, 0);
                return true;
            }

            surfaceNormal = particlePosition / maxDist;

            surfacePoint = surfaceNormal;
            surfacePoint *= fieldSize;
            fieldRotation.Rotate(ref surfacePoint);
            surfacePoint += fieldPosition;

            surfaceNormal /= fieldSize;
            fieldRotation.Rotate(ref surfaceNormal);
            surfaceNormal.Normalize();

            return (maxDist <= 1);
        }
    }
}
