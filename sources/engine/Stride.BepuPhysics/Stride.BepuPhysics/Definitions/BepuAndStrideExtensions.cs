using System.Numerics;
using System.Runtime.CompilerServices;

namespace Stride.BepuPhysics.Definitions;

internal static class BepuAndStrideExtensions
{
    public static Vector3 ToNumericVector(this Core.Mathematics.Vector3 vec)
    {
        return Unsafe.As<Core.Mathematics.Vector3, Vector3>(ref vec);
        //return new Vector3(vec.X, vec.Y, vec.Z);
    }
    public static Core.Mathematics.Vector3 ToStrideVector(this Vector3 vec)
    {
        return Unsafe.As<Vector3, Core.Mathematics.Vector3>(ref vec);
        //return new Stride.Core.Mathematics.Vector3(vec.X, vec.Y, vec.Z);
    }

    public static Quaternion ToNumericQuaternion(this Core.Mathematics.Quaternion qua)
    {
        return Unsafe.As<Core.Mathematics.Quaternion, Quaternion>(ref qua);
        //return new Quaternion(qua.X, qua.Y, qua.Z, qua.W);
    }
    public static Core.Mathematics.Quaternion ToStrideQuaternion(this Quaternion qua)
    {
        return Unsafe.As<Quaternion, Core.Mathematics.Quaternion>(ref qua);
        //return new Stride.Core.Mathematics.Quaternion(qua.X, qua.Y, qua.Z, qua.W);
    }
}