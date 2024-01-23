using NVector3 = System.Numerics.Vector3;
using SVector3 = Stride.Core.Mathematics.Vector3;

using NQuaternion = System.Numerics.Quaternion;
using SQuaternion = Stride.Core.Mathematics.Quaternion;

using NRigidPose = BepuPhysics.RigidPose;
using SRigidPose = Stride.BepuPhysics.Definitions.RigidPose;

using NBodyVelocity = BepuPhysics.BodyVelocity;
using SBodyVelocity = Stride.BepuPhysics.Definitions.BodyVelocity;

using System.Runtime.CompilerServices;


internal static class BepuAndStrideExtensions
{

    public static NRigidPose ToNumericRigidPose(this SRigidPose pose)
    {
        return Unsafe.As<SRigidPose, NRigidPose>(ref pose);
    }
    public static SRigidPose ToStrideRigidPose(this NRigidPose pose)
    {
        return Unsafe.As<NRigidPose, SRigidPose>(ref pose);
    }

    public static NBodyVelocity ToNumericBodyVelocity(this SBodyVelocity pose)
    {
        return Unsafe.As<SBodyVelocity, NBodyVelocity>(ref pose);
    }
    public static SBodyVelocity ToStrideBodyVelocity(this NBodyVelocity pose)
    {
        return Unsafe.As<NBodyVelocity, SBodyVelocity>(ref pose);
    }

    public static NVector3 ToNumericVector(this SVector3 vec)
    {
        return Unsafe.As<SVector3, NVector3>(ref vec);
    }
    public static SVector3 ToStrideVector(this NVector3 vec)
    {
        return Unsafe.As<NVector3, SVector3>(ref vec);
    }

    public static NQuaternion ToNumericQuaternion(this SQuaternion qua)
    {
        return Unsafe.As<SQuaternion, NQuaternion>(ref qua);
    }
    public static SQuaternion ToStrideQuaternion(this NQuaternion qua)
    {
        return Unsafe.As<NQuaternion, SQuaternion>(ref qua);
    }
}