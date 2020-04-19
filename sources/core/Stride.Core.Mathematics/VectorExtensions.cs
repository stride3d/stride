// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Mathematics
{
    /// <summary>
    /// Extensions methods of the vector classes.
    /// </summary>
    public static class VectorExtensions
    {
        /// <summary>
        /// Return the Y/X components of the vector in the inverse order.
        /// </summary>
        /// <param name="vector">the input vector</param>
        public static Vector2 YX(this Vector2 vector)
        {
            return new Vector2(vector.Y, vector.X);
        }

        /// <summary>
        /// Return the X/Y components of the vector.
        /// </summary>
        /// <param name="vector">the input vector</param>
        public static Vector2 XY(this Vector3 vector)
        {
            return new Vector2(vector.X, vector.Y);
        }

        /// <summary>
        /// Return the X/Z components of the vector.
        /// </summary>
        /// <param name="vector">the input vector</param>
        public static Vector2 XZ(this Vector3 vector)
        {
            return new Vector2(vector.X, vector.Z);
        }

        /// <summary>
        /// Return the Y/Z components of the vector.
        /// </summary>
        /// <param name="vector">the input vector</param>
        public static Vector2 YZ(this Vector3 vector)
        {
            return new Vector2(vector.Y, vector.Z);
        }

        /// <summary>
        /// Return the X/Y components of the vector.
        /// </summary>
        /// <param name="vector">the input vector</param>
        public static Vector2 XY(this Vector4 vector)
        {
            return new Vector2(vector.X, vector.Y);
        }

        /// <summary>
        /// Return the X/Y/Z components of the vector.
        /// </summary>
        /// <param name="vector">the input vector</param>
        public static Vector3 XYZ(this Vector4 vector)
        {
            return new Vector3(vector.X, vector.Y, vector.Z);
        }
    }
}
