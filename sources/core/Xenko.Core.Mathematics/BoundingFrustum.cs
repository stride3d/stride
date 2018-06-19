// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;

namespace Xenko.Core.Mathematics
{
    /// <summary>
    /// A bounding frustum.
    /// </summary>
    public struct BoundingFrustum
    {
        /// <summary>
        /// The left plane of this frustum.
        /// </summary>
        public Plane LeftPlane;

        /// <summary>
        /// The right plane of this frustum.
        /// </summary>
        public Plane RightPlane;

        /// <summary>
        /// The top  plane of this frustum.
        /// </summary>
        public Plane TopPlane;

        /// <summary>
        /// The bottom plane of this frustum.
        /// </summary>
        public Plane BottomPlane;

        /// <summary>
        /// The near plane of this frustum.
        /// </summary>
        public Plane NearPlane;

        /// <summary>
        /// The far plane of this frustum.
        /// </summary>
        public Plane FarPlane;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoundingFrustum"/> struct from a matrix view-projection.
        /// </summary>
        /// <param name="matrix">The matrix view projection.</param>
        public BoundingFrustum(ref Matrix matrix)
        {
            // Left
            LeftPlane = Plane.Normalize(new Plane(
                matrix.M14 + matrix.M11,
                matrix.M24 + matrix.M21,
                matrix.M34 + matrix.M31,
                matrix.M44 + matrix.M41));

            // Right
            RightPlane = Plane.Normalize(new Plane(
                matrix.M14 - matrix.M11,
                matrix.M24 - matrix.M21,
                matrix.M34 - matrix.M31,
                matrix.M44 - matrix.M41));

            // Top
            TopPlane = Plane.Normalize(new Plane(
                matrix.M14 - matrix.M12,
                matrix.M24 - matrix.M22,
                matrix.M34 - matrix.M32,
                matrix.M44 - matrix.M42));

            // Bottom
            BottomPlane = Plane.Normalize(new Plane(
                matrix.M14 + matrix.M12,
                matrix.M24 + matrix.M22,
                matrix.M34 + matrix.M32,
                matrix.M44 + matrix.M42));

            // Near
            NearPlane = Plane.Normalize(new Plane(
                matrix.M13,
                matrix.M23,
                matrix.M33,
                matrix.M43));

            // Far
            FarPlane = Plane.Normalize(new Plane(
                matrix.M14 - matrix.M13,
                matrix.M24 - matrix.M23,
                matrix.M34 - matrix.M33,
                matrix.M44 - matrix.M43));
        }

        /// <summary>
        /// Check whether this frustum contains the specified <see cref="BoundingBoxExt"/>.
        /// </summary>
        /// <param name="boundingBoxExt">The bounding box.</param>
        /// <returns><c>true</c> if this frustum contains the specified bounding box.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(ref BoundingBoxExt boundingBoxExt)
        {
            return CollisionHelper.FrustumContainsBox(ref this, ref boundingBoxExt);
        }
    }
}
