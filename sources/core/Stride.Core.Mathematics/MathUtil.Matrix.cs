using System.Numerics;

namespace Stride.Core.Mathematics;

public static partial class MathUtil
{
    public static Matrix4x4 Orthonormalize(Matrix4x4 matrix)
    {
        var result = matrix;

        var row1 = new System.Numerics.Vector4(matrix.M11, matrix.M12, matrix.M13, matrix.M14);
        var row2 = new System.Numerics.Vector4(matrix.M21, matrix.M22, matrix.M23, matrix.M24);
        var row3 = new System.Numerics.Vector4(matrix.M31, matrix.M32, matrix.M33, matrix.M34);
        var row4 = new System.Numerics.Vector4(matrix.M41, matrix.M42, matrix.M43, matrix.M44);

        row1 = System.Numerics.Vector4.Normalize(row1);

        row2 = row2 - System.Numerics.Vector4.Dot(row1, row2) * row1;
        row2 = System.Numerics.Vector4.Normalize(row2);

        row3 = row3 - System.Numerics.Vector4.Dot(row1, row3) * row1;
        row3 = row3 - System.Numerics.Vector4.Dot(row2, row3) * row2;
        row3 = System.Numerics.Vector4.Normalize(row3);

        row4 = row4 - System.Numerics.Vector4.Dot(row1, row4) * row1;
        row4 = row4 - System.Numerics.Vector4.Dot(row2, row4) * row2;
        row4 = row4 - System.Numerics.Vector4.Dot(row3, row4) * row3;
        row4 = System.Numerics.Vector4.Normalize(row4);

        result = new Matrix4x4();
        result.M11 = row1.X;
        result.M12 = row1.Y;
        result.M13 = row1.Z;
        result.M14 = row1.W;
        result.M21 = row2.X;
        result.M22 = row2.Y;
        result.M23 = row2.Z;
        result.M24 = row2.W;
        result.M31 = row3.X;
        result.M32 = row3.Y;
        result.M33 = row3.Z;
        result.M34 = row3.W;
        result.M41 = row4.X;
        result.M42 = row4.Y;
        result.M43 = row4.Z;
        result.M44 = row4.W;

        return result;
    }

    public static Matrix4x4 Orthogonalize(Matrix4x4 matrix)
    {
        var result = matrix;

        var row1 = new System.Numerics.Vector4(matrix.M11, matrix.M12, matrix.M13, matrix.M14);
        var row2 = new System.Numerics.Vector4(matrix.M21, matrix.M22, matrix.M23, matrix.M24);
        var row3 = new System.Numerics.Vector4(matrix.M31, matrix.M32, matrix.M33, matrix.M34);
        var row4 = new System.Numerics.Vector4(matrix.M41, matrix.M42, matrix.M43, matrix.M44);

        row2 = row2 - (System.Numerics.Vector4.Dot(row1, row2) / System.Numerics.Vector4.Dot(row1, row1)) * row1;

        row3 = row3 - (System.Numerics.Vector4.Dot(row1, row3) / System.Numerics.Vector4.Dot(row1, row1)) * row1;
        row3 = row3 - (System.Numerics.Vector4.Dot(row2, row3) / System.Numerics.Vector4.Dot(row2, row2)) * row2;

        row4 = row4 - (System.Numerics.Vector4.Dot(row1, row4) / System.Numerics.Vector4.Dot(row1, row1)) * row1;
        row4 = row4 - (System.Numerics.Vector4.Dot(row2, row4) / System.Numerics.Vector4.Dot(row2, row2)) * row2;
        row4 = row4 - (System.Numerics.Vector4.Dot(row3, row4) / System.Numerics.Vector4.Dot(row3, row3)) * row3;

        result.M21 = row2.X;
        result.M22 = row2.Y;
        result.M23 = row2.Z;
        result.M24 = row2.W;
        result.M31 = row3.X;
        result.M32 = row3.Y;
        result.M33 = row3.Z;
        result.M34 = row3.W;
        result.M41 = row4.X;
        result.M42 = row4.Y;
        result.M43 = row4.Z;
        result.M44 = row4.W;

        return result;
    }

    public static Matrix4x4 Invert(Matrix4x4 matrix)
    {
        Matrix4x4.Invert(matrix, out var result);
        return result;
    }

    public static void Decompose(Matrix4x4 matrix, out System.Numerics.Vector3 scale, out System.Numerics.Quaternion rotation, out System.Numerics.Vector3 translation)
    {
        Matrix4x4.Decompose(matrix, out scale, out rotation, out translation);
    }

}
