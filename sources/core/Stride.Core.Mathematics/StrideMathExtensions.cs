// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using NVector2 = System.Numerics.Vector2;
using NVector3 = System.Numerics.Vector3;
using NVector4 = System.Numerics.Vector4;
using NQuaternion = System.Numerics.Quaternion;
using System.Runtime.CompilerServices;

namespace Stride.Core.Mathematics;
/// <summary>
/// Generic extensions for Stride.Core.Mathematics types.
/// </summary>
public static class StrideMathExtensions
{
    /// <summary>
    /// Converts a System.Numerics.Vector2 to a Stride.Core.Mathematics.Vector2.
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public static Vector2 ToStride(this NVector2 v) => Unsafe.As<NVector2, Vector2>(ref v);
    /// <summary>
    /// Converts a System.Numerics.Vector3 to a Stride.Core.Mathematics.Vector3.
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public static Vector3 ToStride(this NVector3 v) => Unsafe.As<NVector3, Vector3>(ref v);
    /// <summary>
    /// Converts a System.Numerics.Vector4 to a Stride.Core.Mathematics.Vector4.
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public static Vector4 ToStride(this NVector4 v) => Unsafe.As<NVector4, Vector4>(ref v);
    /// <summary>
    /// Converts a System.Numerics.Quaternion to a Stride.Core.Mathematics.Quaternion.
    /// </summary>
    /// <param name="q"></param>
    /// <returns></returns>
    public static Quaternion ToStride(this NQuaternion q) => Unsafe.As<NQuaternion, Quaternion>(ref q);

    /// <summary>
    /// Converts a Stride.Core.Mathematics.Vector2 to a System.Numerics.Vector2.
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public static NVector2 ToNumeric(this Vector2 v) => Unsafe.As<Vector2, NVector2>(ref v);
    /// <summary>
    /// Converts a Stride.Core.Mathematics.Vector3 to a System.Numerics.Vector3.
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public static NVector3 ToNumeric(this Vector3 v) => Unsafe.As<Vector3, NVector3>(ref v);
    /// <summary>
    /// Converts a Stride.Core.Mathematics.Vector4 to a System.Numerics.Vector4.
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public static NVector4 ToNumeric(this Vector4 v) => Unsafe.As<Vector4, NVector4>(ref v);
    /// <summary>
    /// Converts a Stride.Core.Mathematics.Quaternion to a System.Numerics.Quaternion.
    /// </summary>
    /// <param name="q"></param>
    /// <returns></returns>
    public static NQuaternion ToNumeric(this Quaternion q) => Unsafe.As<Quaternion, NQuaternion>(ref q);
}
