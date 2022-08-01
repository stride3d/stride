using System;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;
using Xunit.Abstractions;

namespace Stride.Core.Mathematics.Tests;

public class AlignmentTests
{
    private readonly ITestOutputHelper output;
    public AlignmentTests(ITestOutputHelper output) => this.output = output;

    private static readonly MethodInfo unsafeSizeOf = typeof(Unsafe)
        .GetMethod(nameof(Unsafe.SizeOf),
        BindingFlags.Static | BindingFlags.Public);
    private static int SizeOf(Type type)
        => (int)unsafeSizeOf
        .MakeGenericMethod(type)
        .Invoke(null, Array.Empty<object>());

#pragma warning disable IDE1006 // Naming Styles
    private const int float2 = 2 * sizeof(float);
    private const int float3 = 3 * sizeof(float);
    private const int float4 = 4 * sizeof(float);
    private const int plane = float4;
#pragma warning restore IDE1006 // Naming Styles

    public record struct Info(Type Type, int Size, int AlignWanted, int NaturalAlign)
    {
        public static implicit operator Info((Type t, int s, int w, int n) x) => new(x.t, x.s, x.w, x.n);
    }
    public static readonly TheoryData<Info> TypesAndSizes = new()
    { //  type                       size               align   natural (without alignment members)
        { (typeof(AngleSingle      ), sizeof(float)      ,  4,  4) },
        { (typeof(BoundingBox      ), 2 * float3         ,  4,  4) },
        { (typeof(BoundingBoxExt   ), 2 * float3         ,  4,  4) },
        { (typeof(BoundingFrustum  ), 6 * plane          ,  4,  4) },
        { (typeof(BoundingSphere   ), float4             ,  4,  4) },
        { (typeof(Color            ), 4 * sizeof(byte)   ,  1,  1) },
        { (typeof(Color3           ), float3             ,  4,  4) },
        { (typeof(Color4           ), float4             ,  4,  4) },
        { (typeof(ColorBGRA        ), 4 * sizeof(byte)   ,  1,  1) },
        { (typeof(ColorHSV         ), float4             , 16,  4) },
        { (typeof(Double2          ), 16                 ,  8,  8) },
        { (typeof(Double3          ), 24                 ,  8,  8) },
        { (typeof(Double4          ), 32                 ,  8,  8) },
        { (typeof(Half             ), 2                  ,  2,  2) },
        { (typeof(Half2            ), 4                  ,  2,  2) },
        { (typeof(Half3            ), 6                  ,  2,  2) },
        { (typeof(Half4            ), 8                  ,  2,  2) },
        { (typeof(Int2             ), 2 * sizeof(int)    ,  4,  4) },
        { (typeof(Int3             ), 3 * sizeof(int)    ,  4,  4) },
        { (typeof(Int4             ), 4 * sizeof(int)    ,  4,  4) },
        { (typeof(Matrix           ), 4 * float4         ,  4,  4) },
        { (typeof(Plane            ), float4             ,  4,  4) },
        { (typeof(Point            ), 2 * sizeof(int)    ,  4,  4) },
        { (typeof(Quaternion       ), float4             ,  4,  4) },
        { (typeof(Ray              ), 2 * float3         ,  4,  4) },
        { (typeof(Rectangle        ), 4 * sizeof(int)    ,  4,  4) },
        { (typeof(RectangleF       ), float4             ,  4,  4) },
        { (typeof(Size2            ), 2 * sizeof(int)    ,  4,  4) },
        { (typeof(Size2F           ), float2             ,  4,  4) },
        { (typeof(Size3            ), 3 * sizeof(int)    ,  4,  4) },
        { (typeof(UInt4            ), 4 * sizeof(uint)   ,  4,  4) },
        { (typeof(Vector2          ), float2             ,  4,  4) },
        { (typeof(Vector3          ), float3             ,  4,  4) },
        { (typeof(Vector4          ), float4             ,  4,  4) },
    };

    [Theory]
    [MemberData(nameof(TypesAndSizes))]
    public void AssertSizeOf(Info info)
    {
        var actual = SizeOf(info.Type);
        Assert.Equal(info.AlignWanted, actual);
    }
    private static readonly MethodInfo checkAlignmentOnStack = typeof(AlignmentTests)
        .GetMethod(nameof(CheckAlignmentOnStackImpl),
        BindingFlags.NonPublic | BindingFlags.Static);

    private static unsafe void CheckAlignmentOnStackImpl<T>(byte arg0, T arg1, byte arg2, int align)
        where T : unmanaged
    {
        _ = arg0; // needed to 'defeat' x64 stack alignment
        _ = arg2; // maybe does something similar on some other platform
        var ptr = &arg1;
        var zeroes = 1 << BitOperations.TrailingZeroCount((nint)ptr);
        var expected = Math.Max(Unsafe.SizeOf<nint>(), align);
        var actual = Math.Max(Unsafe.SizeOf<nint>(), Math.Min(zeroes, align));
        Assert.Equal(expected, actual);
    }
    private static int GetAlignment(Type type, int align = 0) {
        var size = SizeOf(type);
        if (size > align)
        {
            foreach (var f in type.GetRuntimeFields())
            {
                if (f.IsStatic) continue;

                var ftype = f.FieldType;
                var fsize = SizeOf(ftype);
                if (fsize > align) {
                    if (!ftype.Namespace.Contains("Stride") &&
                        BitOperations.IsPow2(fsize))
                        align = fsize;
                    else
                        return GetAlignment(ftype, align);
                }
            }
        }
        return align;
    }
    [Theory]
    [MemberData(nameof(TypesAndSizes))]
    public void CheckAlignmentOnStack(Info info)
    {
        var align = GetAlignment(info.Type, 0);
        output.WriteLine(
            $"[{info.Type}] Size: {info.Size}, Align: {align}, " +
            $"Expected: {info.AlignWanted}, Natural: {info.NaturalAlign}");
        checkAlignmentOnStack
            .MakeGenericMethod(info.Type)
            .Invoke(null, new object[]
            {
                byte.MaxValue,
                Activator.CreateInstance(info.Type),
                (byte)0xa5,
                info.AlignWanted
            });
        Assert.Equal(info.AlignWanted, align);
    }
}
