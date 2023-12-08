using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using Stride.Engine;

namespace Stride.BepuPhysics.Extensions
{
    public static class BepuAndStrideExtensions
    {
        public const int LIST_SIZE = 50000;
        public const int X_DEBUG_TEXT_POS = 2000; //1200

        public static Core.Mathematics.Vector3 GetWorldPos(this TransformComponent tr)
        {
            tr.WorldMatrix.Decompose(out var _1, out Core.Mathematics.Quaternion _2, out var _3);
            return _3;
        }

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

        public static RigidPose ToBepuPose(this TransformComponent transform)
        {
            return new RigidPose(transform.Position.ToNumericVector(), transform.Rotation.ToNumericQuaternion());
        }
    }

}
