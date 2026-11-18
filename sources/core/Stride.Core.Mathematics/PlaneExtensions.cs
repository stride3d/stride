// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// -----------------------------------------------------------------------------
// Original code from SlimMath project. http://code.google.com/p/slimmath/
// Greetings to SlimDX Group. Original code published with the following license:
// -----------------------------------------------------------------------------
/*
* Copyright (c) 2007-2011 SlimDX Group
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
* THE SOFTWARE.
*/

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Numerics;

namespace Stride.Core.Mathematics;

/// <summary>
/// The members of Stride's math API that <see cref="Plane"/> does not have.
/// </summary>
public static class PlaneExtensions
{
    extension(Plane)
    {
        /// <summary>
        /// Projects a point onto a plane.
        /// </summary>
        /// <param name="plane">The plane to project the point to.</param>
        /// <param name="point">The point to project.</param>
        /// <returns>The projected point.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Project(Plane plane, Vector3 point)
        {
            var distance = Plane.DotCoordinate(plane, point);

            // compute: point - distance * plane.Normal
            Vector3 result = plane.Normal * distance;
            result = point - result;
            return result;
        }

        /// <summary>
        /// Transforms an array of normalized planes by a quaternion rotation.
        /// </summary>
        /// <param name="planes">The array of normalized planes to transform.</param>
        /// <param name="rotation">The quaternion rotation.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="planes"/> is <c>null</c>.</exception>
        public static void Transform(Plane[] planes, Quaternion rotation)
        {
            if (planes == null)
                throw new ArgumentNullException(nameof(planes));

            float x2 = rotation.X + rotation.X;
            float y2 = rotation.Y + rotation.Y;
            float z2 = rotation.Z + rotation.Z;
            float wx = rotation.W * x2;
            float wy = rotation.W * y2;
            float wz = rotation.W * z2;
            float xx = rotation.X * x2;
            float xy = rotation.X * y2;
            float xz = rotation.X * z2;
            float yy = rotation.Y * y2;
            float yz = rotation.Y * z2;
            float zz = rotation.Z * z2;

            for (int i = 0; i < planes.Length; ++i)
            {
                float x = planes[i].Normal.X;
                float y = planes[i].Normal.Y;
                float z = planes[i].Normal.Z;

                /*
                 * Note:
                 * Factor common arithmetic out of loop.
                */
                planes[i].Normal.X = ((x * ((1.0f - yy) - zz)) + (y * (xy - wz))) + (z * (xz + wy));
                planes[i].Normal.Y = ((x * (xy + wz)) + (y * ((1.0f - xx) - zz))) + (z * (yz - wx));
                planes[i].Normal.Z = ((x * (xz - wy)) + (y * (yz + wx))) + (z * ((1.0f - xx) - yy));
            }
        }

        /// <summary>
        /// Transforms an array of normalized planes by a matrix.
        /// </summary>
        /// <param name="planes">The array of normalized planes to transform.</param>
        /// <param name="transformation">The transformation matrix.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="planes"/> is <c>null</c>.</exception>
        public static void Transform(Plane[] planes, in Matrix4x4 transformation)
        {
            if (planes == null)
                throw new ArgumentNullException(nameof(planes));

            Matrix4x4.InvertSafe(transformation, out Matrix4x4 inverse);

            for (int i = 0; i < planes.Length; ++i)
            {
                planes[i] = Plane.Transform(planes[i], transformation);
            }
        }

        /// <summary>
        /// Scales a plane by the given value.
        /// </summary>
        /// <param name="scale">The amount by which to scale the plane.</param>
        /// <param name="plane">The plane to scale.</param>
        /// <returns>The scaled plane.</returns>
        public static Plane operator *(float scale, Plane plane)
        {
            return new Plane(plane.Normal.X * scale, plane.Normal.Y * scale, plane.Normal.Z * scale, plane.D * scale);
        }

        /// <summary>
        /// Scales a plane by the given value.
        /// </summary>
        /// <param name="plane">The plane to scale.</param>
        /// <param name="scale">The amount by which to scale the plane.</param>
        /// <returns>The scaled plane.</returns>
        public static Plane operator *(Plane plane, float scale)
        {
            return new Plane(plane.Normal.X * scale, plane.Normal.Y * scale, plane.Normal.Z * scale, plane.D * scale);
        }

        /// <summary>
        /// Negates a plane by negating all its coefficients, which result in a plane in opposite direction.
        /// </summary>
        /// <returns>The negated plane.</returns>
        public static Plane operator -(Plane plane)
        {
            return new Plane(-plane.Normal.X, -plane.Normal.Y, -plane.Normal.Z, -plane.D);
        }

    }

    extension(in Plane self)
    {
        /// <summary>Returns the text System.Numerics gives for a plane, with each number formatted as asked.</summary>
        public string ToString(string format, IFormatProvider formatProvider)
            => $"{{Normal:{self.Normal.ToString(format, formatProvider)} D:{self.D.ToString(format, formatProvider)}}}";

        /// <summary>
        /// Creates an array containing the elements of the plane.
        /// </summary>
        /// <returns>A four-element array containing the components of the plane.</returns>
        public float[] ToArray()
        {
            return [self.Normal.X, self.Normal.Y, self.Normal.Z, self.D];
        }

        /// <summary>
        /// Determines if there is an intersection between the current object and a point.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PlaneIntersectionType Intersects(Vector3 point)
        {
            return CollisionHelper.PlaneIntersectsPoint(in self, in point);
        }

        /// <summary>
        /// Determines if there is an intersection between the current object and a <see cref="Stride.Core.Mathematics.Ray"/>.
        /// </summary>
        /// <param name="ray">The ray to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Intersects(in Ray ray)
        {
            float distance;
            return CollisionHelper.RayIntersectsPlane(in ray, in self, out distance);
        }

        /// <summary>
        /// Determines if there is an intersection between the current object and a <see cref="Stride.Core.Mathematics.Ray"/>.
        /// </summary>
        /// <param name="ray">The ray to test.</param>
        /// <param name="distance">When the method completes, contains the distance of the intersection,
        /// or 0 if there was no intersection.</param>
        /// <returns>Whether the two objects intersected.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Intersects(in Ray ray, out float distance)
        {
            return CollisionHelper.RayIntersectsPlane(in ray, in self, out distance);
        }

        /// <summary>
        /// Determines if there is an intersection between the current object and a <see cref="Stride.Core.Mathematics.Ray"/>.
        /// </summary>
        /// <param name="ray">The ray to test.</param>
        /// <param name="point">When the method completes, contains the point of intersection,
        /// or <see cref="System.Numerics.Vector3.Zero"/> if there was no intersection.</param>
        /// <returns>Whether the two objects intersected.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Intersects(in Ray ray, out Vector3 point)
        {
            return CollisionHelper.RayIntersectsPlane(in ray, in self, out point);
        }

        /// <summary>
        /// Determines if there is an intersection between the current object and a <see cref="System.Numerics.Plane"/>.
        /// </summary>
        /// <param name="plane">The plane to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Intersects(Plane plane)
        {
            return CollisionHelper.PlaneIntersectsPlane(in self, in plane);
        }

        /// <summary>
        /// Determines if there is an intersection between the current object and a <see cref="System.Numerics.Plane"/>.
        /// </summary>
        /// <param name="plane">The plane to test.</param>
        /// <param name="line">When the method completes, contains the line of intersection
        /// as a <see cref="Stride.Core.Mathematics.Ray"/>, or a zero ray if there was no intersection.</param>
        /// <returns>Whether the two objects intersected.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Intersects(in Plane plane, out Ray line)
        {
            return CollisionHelper.PlaneIntersectsPlane(in self, in plane, out line);
        }

        /// <summary>
        /// Determines if there is an intersection between the current object and a triangle.
        /// </summary>
        /// <param name="vertex1">The first vertex of the triangle to test.</param>
        /// <param name="vertex2">The second vertex of the triagnle to test.</param>
        /// <param name="vertex3">The third vertex of the triangle to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PlaneIntersectionType Intersects(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3)
        {
            return CollisionHelper.PlaneIntersectsTriangle(in self, in vertex1, in vertex2, in vertex3);
        }

        /// <summary>
        /// Determines if there is an intersection between the current object and a <see cref="Stride.Core.Mathematics.BoundingBox"/>.
        /// </summary>
        /// <param name="box">The box to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PlaneIntersectionType Intersects(in BoundingBox box)
        {
            return CollisionHelper.PlaneIntersectsBox(in self, in box);
        }

        /// <summary>
        /// Determines if there is an intersection between the current object and a <see cref="Stride.Core.Mathematics.BoundingSphere"/>.
        /// </summary>
        /// <param name="sphere">The sphere to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PlaneIntersectionType Intersects(in BoundingSphere sphere)
        {
            return CollisionHelper.PlaneIntersectsSphere(in self, in sphere);
        }

        /// <summary>Determines whether every component of a plane differs from the same component of this one by less than a tolerance.</summary>
        /// <param name="value">The plane to compare with this instance.</param>
        /// <param name="tolerance">The difference of a component from which the two are not near.</param>
        public bool NearEquals(Plane value, float tolerance = MathUtil.ZeroTolerance)
        {
            return self.Normal.NearEquals(value.Normal, tolerance) && MathF.Abs(self.D - value.D) < tolerance;
        }

    }
}
