// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_UWP

using Stride.Core.Mathematics;

namespace Stride.VirtualReality
{
    internal static class SystemNumericsExtensions
    {
        /// <summary>
        /// Performs a conversion from <see cref="System.Numerics.Matrix4x4"/> to <see cref="Stride.Core.Mathematics.Matrix"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static Matrix ToMatrix(this System.Numerics.Matrix4x4 value)
        {
            return new Matrix()
            {
                M11 = value.M11, M12 = value.M12, M13 = value.M13, M14 = value.M14,
                M21 = value.M21, M22 = value.M22, M23 = value.M23, M24 = value.M24,
                M31 = value.M31, M32 = value.M32, M33 = value.M33, M34 = value.M34,
                M41 = value.M41, M42 = value.M42, M43 = value.M43, M44 = value.M44
            };
        }

        /// <summary>
        /// Performs a conversion from <see cref="System.Numerics.Quaternion"/> to <see cref="Stride.Core.Mathematics.Quaternion"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static Quaternion ToQuaternion(this System.Numerics.Quaternion value)
        {
            return new Quaternion(value.X, value.Y, value.Z, value.W);
        }

        /// <summary>
        /// Performs a conversion from <see cref="System.Numerics.Vector3"/> to <see cref="Stride.Core.Mathematics.Vector3"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static Vector3 ToVector3(this System.Numerics.Vector3 value)
        {
            return new Vector3(value.X, value.Y, value.Z);
        }
    }
}

#endif
