// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using NVector3 = System.Numerics.Vector3;
using SVector3 = Stride.Core.Mathematics.Vector3;

using NQuaternion = System.Numerics.Quaternion;
using SQuaternion = Stride.Core.Mathematics.Quaternion;

using BRigidPose = BepuPhysics.RigidPose;
using SRigidPose = Stride.BepuPhysics.Definitions.RigidPose;

using BBodyVelocity = BepuPhysics.BodyVelocity;
using SBodyVelocity = Stride.BepuPhysics.Definitions.BodyVelocity;

using System.Runtime.CompilerServices;


internal static class BepuAndStrideExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BRigidPose ToBepu(this SRigidPose pose) => Unsafe.As<SRigidPose, BRigidPose>(ref pose);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SRigidPose ToStride(this BRigidPose pose) => Unsafe.As<BRigidPose, SRigidPose>(ref pose);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BBodyVelocity ToBepu(this SBodyVelocity pose) => Unsafe.As<SBodyVelocity, BBodyVelocity>(ref pose);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SBodyVelocity ToStride(this BBodyVelocity pose) => Unsafe.As<BBodyVelocity, SBodyVelocity>(ref pose);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NVector3 ToNumeric(this SVector3 vec) => Unsafe.As<SVector3, NVector3>(ref vec);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SVector3 ToStride(this NVector3 vec) => Unsafe.As<NVector3, SVector3>(ref vec);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NQuaternion ToNumeric(this SQuaternion qua) => Unsafe.As<SQuaternion, NQuaternion>(ref qua);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SQuaternion ToStride(this NQuaternion qua) => Unsafe.As<NQuaternion, SQuaternion>(ref qua);
}
