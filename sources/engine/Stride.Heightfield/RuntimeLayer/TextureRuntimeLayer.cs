// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Graphics;
using Stride.Graphics.Data;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Half = System.Half;
using Vector2 = System.Numerics.Vector2;

namespace Stride.Heightfield.RuntimeLayer;

/// <summary>
/// Heightfield built from a texture
/// </summary>
[DataContract]
public record TextureRuntimeLayer : HeightfieldTextureSharedData, IHeightfieldRuntimeLayer
{
    /// <summary>
    /// Format <see cref="Heights"/> is in
    /// </summary>
    /// <remarks>
    /// May or may not match <see cref="Texture"/>'s own format
    /// </remarks>
    public required PixelFormat HeightsFormat { get; init; }

    /// <summary>
    /// Heights laid out in [x + y * <see cref="BufferResolution"/>.X]
    /// </summary>
    /// <remarks>
    /// For scalar types; halfs and floats, we assume these values to already be scaled by HeightMultiplier,
    /// others will be scaled on sample instead to retain precision
    /// </remarks>
    public required byte[] Heights { get; init; }

    /// <summary>
    /// The dimensions of <see cref="Heights"/>
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Each dimension is required to be a power of two for performance purposes
    /// </exception>
    public required Int2 BufferResolution
    {
        get;
        init
        {
            if (MathUtil.IsPow2(value.X) == false)
                throw new ArgumentException($"Must be a power of two ({value.X})");
            if (MathUtil.IsPow2(value.Y) == false)
                throw new ArgumentException($"Must be a power of two ({value.Y})");

            field = value;
        }
    }

    /// <summary>
    /// Matches the <see cref="IHeightfieldSource.Subdivision"/> this layer runs on
    /// </summary>
    public required int HeightfieldSubdivision { get; init; }

    /// <inheritdoc/>
    public IHeightfieldFunction BuildHeightfieldFunction()
    {
        return (HeightsFormat, Tiling) switch
        {
            (PixelFormat.R32_Float, 1f) => Build<float, FloatToFloat, NoWrap>(),
            (PixelFormat.R16_Float, 1f) => Build<Half, HalfToFloat, NoWrap>(),
            (PixelFormat.R16_SNorm, 1f) => Build<short, ShortToFloat, NoWrap>(),
            (PixelFormat.R16_UNorm, 1f) => Build<ushort, UShortToFloat, NoWrap>(),
            (PixelFormat.R8_UNorm, 1f) => Build<byte, ByteToFloat, NoWrap>(),

            (PixelFormat.R32_Float, _) => Build<float, FloatToFloat, AndWrap>(),
            (PixelFormat.R16_Float, _) => Build<Half, HalfToFloat, AndWrap>(),
            (PixelFormat.R16_SNorm, _) => Build<short, ShortToFloat, AndWrap>(),
            (PixelFormat.R16_UNorm, _) => Build<ushort, UShortToFloat, AndWrap>(),
            (PixelFormat.R8_UNorm, _) => Build<byte, ByteToFloat, AndWrap>(),
            _ => throw new InvalidOperationException()
        };
    }

    private IHeightfieldFunction Build<T, TConv, TWrap>() where T : unmanaged where TConv : IFloatConverter<T> where TWrap : IWrapper
    {
        // Coords are [0,field-1] we want to remap them into [0,rez-1]
        var rezToBuff = ((Vector2)(BufferResolution - Int2.One)) / (HeightfieldSubdivision - 1) * Tiling;
        var mask = BufferResolution - new Int2(1);

        bool oneCellPerPixel = MathUtil.NearEqual(rezToBuff.X, 1f) && MathUtil.NearEqual(rezToBuff.Y, 1f);

        // No reason to bilinear if each pixel has a corresponding field cell
        var bilinear = BilinearSampling && !oneCellPerPixel;

        // floats and halfs are pre-multiplied to improve throughput
        if (HeightMultiplier is 1f || HeightsFormat is PixelFormat.R32_Float or PixelFormat.R16_Float)
            return bilinear ? new FunctionBilinear<T, TConv, TWrap>(mask, Heights, rezToBuff) : new FunctionNearest<T, TConv, TWrap>(mask, Heights, rezToBuff);

        // integer buffer's values are not pre-multiplied to improve accuracy, wrap sampler to post multiply them after every sample
        return bilinear ?
            new Multiply<FunctionBilinear<T, TConv, TWrap>>(new FunctionBilinear<T, TConv, TWrap>(mask, Heights, rezToBuff), HeightMultiplier) :
            new Multiply<FunctionNearest<T, TConv, TWrap>>(new FunctionNearest<T, TConv, TWrap>(mask, Heights, rezToBuff), HeightMultiplier);
    }

    /// <inheritdoc/>
    public IComputeScalar BuildGpuSideSampler(MaterialGeneratorContext context)
    {
        if (Texture is null)
            return new ComputeFloat(HeightMultiplier);

        var url = AttachedReferenceManager.GetUrl(Texture) ?? throw new NullReferenceException();
        var proxyObject = context.Content.Load<Texture>(url, ContentManagerLoaderSettings.StreamingDisabled);

        var size = new Vector2(proxyObject.Width, proxyObject.Height);
        if (size.X == 0) // might not be allocated/uploaded yet, in which case we'll fetch the backing image
        {
            var img = proxyObject.GetSerializationData().Image;
            size = new Vector2(img.Description.Width, img.Description.Height);
        }

        var textureScalar = new ComputeTextureScalar(Texture, TextureCoordinate.Texcoord0, default, default)
        {
            Filtering = BilinearSampling ? TextureFilter.Linear : TextureFilter.Point,
            AddressModeU = TextureAddressMode.Wrap,
            AddressModeV = TextureAddressMode.Wrap,
            // uv{0,0} is right in the middle of px{0,0}, px{0,^1}, px{^1,0} and px{^1,^1};
            // In other words, px{0,0}'s center is at half a texel from uv{0,0}.
            // It is more accurate to sample from the center than the middle of two pixels
            // regardless of the filtering method.
            Offset = new Vector2(0.5f) / size / Tiling,
            // Same as above, we want the last pixel to be centered on uv{^1,^1}
            // Note: Scale happens first, then offset adds to the UV
            Scale = (size - Vector2.One) / size * Tiling
        };
        return new ComputeBinaryScalar(textureScalar, new ComputeFloat(HeightMultiplier), BinaryOperator.Multiply);
    }

    private readonly record struct FunctionNearest<T, TConverter, TWrapper>(Int2 Mask, byte[] Heights, Vector2 RezToBuff) : IHeightfieldFunction
        where T : unmanaged
        where TConverter : IFloatConverter<T>
        where TWrapper : IWrapper
    {
        public unsafe void FillSamples(Span<Sample> samples)
        {
            fixed (byte* ptr = Heights)
            {
                var tPtr = (T*)ptr;
                foreach (ref var sample in samples)
                {
                    var cellCoord = (Vector2)sample.SampleCoord * RezToBuff;
                    cellCoord = Vector2.Round(cellCoord, MidpointRounding.ToNegativeInfinity);

                    int x0 = TWrapper.Wrap((int)cellCoord.X, Mask.X);
                    int y0 = TWrapper.Wrap((int)cellCoord.Y, Mask.Y);
                    var index = x0 + y0 * (Mask.X + 1);

                    Debug.Assert(index < Heights.Length / sizeof(T));
                    Debug.Assert(index >= 0);

                    sample.Height = TConverter.ToFloat(tPtr[index]);
                }
            }
        }

        public void AppendTo(IFunctionCollector solution) => solution.Append(this);
    }

    private readonly record struct FunctionBilinear<T, TConverter, TWrapper>(Int2 Mask, byte[] Heights, Vector2 RezToBuff) : IHeightfieldFunction
        where T : unmanaged
        where TConverter : IFloatConverter<T>
        where TWrapper : IWrapper
    {
        public unsafe void FillSamples(Span<Sample> samples)
        {
            fixed (byte* ptr = Heights)
            {
                var tPtr = (T*)ptr;
                foreach (ref var sample in samples)
                {
                    var cellCoord = (Vector2)sample.SampleCoord * RezToBuff;
                    var cellCorner = Vector2.Round(cellCoord, MidpointRounding.ToNegativeInfinity);

                    var cellFrac = cellCoord - cellCorner;
                    var cellInvFrac = new Vector2(1) - cellFrac;

                    int x0 = TWrapper.Wrap((int)cellCorner.X, Mask.X);
                    int y0 = TWrapper.Wrap((int)cellCorner.Y, Mask.Y);
                    int x1 = (x0 + 1) & Mask.X;
                    int y1 = (y0 + 1) & Mask.Y;

                    Debug.Assert(x0 + y0 < Heights.Length / sizeof(T));
                    Debug.Assert(x0 + y1 < Heights.Length / sizeof(T));
                    Debug.Assert(x1 + y0 < Heights.Length / sizeof(T));
                    Debug.Assert(x1 + y1 < Heights.Length / sizeof(T));

                    Debug.Assert(x0 + y0 >= 0);
                    Debug.Assert(x0 + y1 >= 0);
                    Debug.Assert(x1 + y0 >= 0);
                    Debug.Assert(x1 + y1 >= 0);

                    var r = Mask.X + 1;
                    y0 *= r;
                    y1 *= r;

                    // DETERMINISM: result of MAE is architecture dependant, bepu does not offer cross-platform determinism right now, but this may change
                    var w0 = float.MultiplyAddEstimate(TConverter.ToFloat(tPtr[x0 + y0]), cellInvFrac.Y, TConverter.ToFloat(tPtr[x0 + y1]) * cellFrac.Y);
                    var w1 = float.MultiplyAddEstimate(TConverter.ToFloat(tPtr[x1 + y0]), cellInvFrac.Y, TConverter.ToFloat(tPtr[x1 + y1]) * cellFrac.Y);
                    sample.Height = float.MultiplyAddEstimate(w0, cellInvFrac.X, w1 * cellFrac.X);
                }
            }
        }

        public void AppendTo(IFunctionCollector solution) => solution.Append(this);
    }

    private readonly record struct Multiply<T>(T InnerFunction, float Multiplier) : IHeightfieldFunction where T : IHeightfieldFunction
    {
        public void FillSamples(Span<Sample> samples)
        {
            InnerFunction.FillSamples(samples);
            foreach (ref var sample in samples)
                sample.Height *= Multiplier;
        }

        public void AppendTo(IFunctionCollector solution) => solution.Append(this);
    }

    private interface IFloatConverter<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static abstract float ToFloat(in T v);
    }

    private struct FloatToFloat : IFloatConverter<float>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToFloat(in float v) => v;
    }

    private struct HalfToFloat : IFloatConverter<Half>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToFloat(in Half v) => (float)v;
    }

    private struct UShortToFloat : IFloatConverter<ushort>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToFloat(in ushort v) => (float)v / ushort.MaxValue;
    }

    private struct ShortToFloat : IFloatConverter<short>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToFloat(in short v) => (float)v / short.MaxValue;
    }

    private struct ByteToFloat : IFloatConverter<byte>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToFloat(in byte v) => (float)v / byte.MaxValue;
    }

    private interface IWrapper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static abstract int Wrap(int value, int wrap);
    }

    private struct NoWrap : IWrapper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Wrap(int value, int wrap) => value;
    }

    private struct AndWrap : IWrapper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Wrap(int value, int wrap) => value & wrap;
    }
}
