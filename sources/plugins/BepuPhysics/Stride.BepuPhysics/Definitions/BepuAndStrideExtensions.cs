// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BRigidPose = BepuPhysics.RigidPose;
using SRigidPose = Stride.BepuPhysics.Definitions.RigidPose;

using BBodyVelocity = BepuPhysics.BodyVelocity;
using SBodyVelocity = Stride.BepuPhysics.Definitions.BodyVelocity;

using System.Numerics;
using System.Runtime.CompilerServices;
using Stride.Core.Mathematics;


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
    public static void ValidateRange<T>(this Vector3 vec, T owner, [CallerMemberName] string? fieldName = null)
    {
        if (Vector3.AllWhereAllBitsSet(Vector3.IsFinite(vec)) == false)
            throw new ArgumentOutOfRangeException($"{owner}'s {fieldName} must be finite");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ValidateRange<T>(this Quaternion quat, T owner, [CallerMemberName] string? fieldName = null)
    {
        if (quat.IsNormalized == false)
            throw new ArgumentOutOfRangeException($"{owner}'s {fieldName} must be normalized");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ValidateGreaterThanZeroFinite<T>(this float fp, T owner, [CallerMemberName] string? fieldName = null)
    {
        if (fp <= 0 || float.IsFinite(fp) == false)
            throw new ArgumentOutOfRangeException($"{owner}'s {fieldName} must be finite and greater than zero");
    }
}
