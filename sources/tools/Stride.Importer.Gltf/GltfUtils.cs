using System;
using System.Collections.Generic;
using System.Text;
using Stride.Core.Mathematics;

namespace Stride.Importer.Gltf
{
    class GltfUtils
    {
        public static Matrix ConvertNumerics(System.Numerics.Matrix4x4 mat)
        {
            return new Matrix(
                    mat.M11, mat.M12, mat.M13, mat.M14,
                    mat.M21, mat.M22, mat.M23, mat.M24,
                    mat.M31, mat.M32, mat.M33, mat.M34,
                    mat.M41, mat.M42, mat.M43, mat.M44
                );
        }

        public static Quaternion ConvertNumerics(System.Numerics.Quaternion v) => new Quaternion(v.X, v.Y, v.Z, v.W);
        public static Vector4 ConvertNumerics(System.Numerics.Vector4 v) => new Vector4(v.X, v.Y, v.Z, v.W);
        public static Vector3 ConvertNumerics(System.Numerics.Vector3 v) => new Vector3(v.X, v.Y, v.Z);
        public static Vector2 ConvertNumerics(System.Numerics.Vector2 v) => new Vector2(v.X, v.Y);
    }
}
