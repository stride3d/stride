// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.BuildEngine;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Graphics;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.TextureConverter;
using Half = System.Half;
using Vector2 = System.Numerics.Vector2;

namespace Stride.BepuPhysics.Definitions.Heightfield.Assets;

/// <summary>
/// Heightfield built from a texture
/// </summary>
[DataContract]
public record HeightfieldTexture : HeightfieldTextureSharedData, IHeightfieldLayerBuilder
{
    /// <inheritdoc/>
    public IEnumerable<ObjectUrl> GetInputFiles()
    {
        if (!string.IsNullOrEmpty(Texture?.Url))
        {
            yield return new ObjectUrl(UrlType.Content, Texture.Url);
        }
    }

    /// <inheritdoc/>
    public void BuildRuntimeRepresentation(float size, int subdivision, ICommandContext commandContext, IAssetFinder assetFinder, out IHeightfieldRuntimeLayer runtimeLayer, out float minHeight, out float maxHeight)
    {
        if (Texture == null! || string.IsNullOrEmpty(Texture.Url))
        {
            runtimeLayer = new RuntimeLayer
            {
                BufferResolution = new Int2(32, 32),
                Texture = new(),
                Tiling = Tiling,
                BilinearSampling = BilinearSampling,
                HeightMultiplier = HeightMultiplier,
                Heights = new byte[32 * 32],
                HeightsFormat = PixelFormat.R8_UNorm,
                HeightfieldSubdivision = subdivision
            };
            minHeight = 0;
            maxHeight = 0;
            commandContext.Logger.Warning("No texture provided");
            return;
        }

        using (var context = new Stride.Assets.Skyboxes.SkyboxGeneratorContext(new(), MicrothreadLocalDatabases.ProviderService))
        {
            var loadedTexture = context.Content.Load<Texture>(Texture.Url, ContentManagerLoaderSettings.StreamingDisabled);
            var baseImage = loadedTexture.GetDataAsImage(context.RenderDrawContext.CommandList);

            using (var textureTool = new TextureTool())
            using (var texImage = textureTool.Load(baseImage, baseImage.Description.Format.IsSRgb))
            {
                switch (texImage.Format)
                {
                    case PixelFormat.R32_Float:
                    case PixelFormat.R16_UNorm:
                    case PixelFormat.R16_SNorm:
                    case PixelFormat.R16_Float:
                    case PixelFormat.R8_UNorm:
                        break;

                    case PixelFormat.R32G32B32A32_Float:
                    case PixelFormat.R16G16B16A16_Float:
                        textureTool.Convert(texImage, PixelFormat.R32_Float);
                        break;

                    case PixelFormat.R16G16B16A16_UNorm:
                    case PixelFormat.R16G16_UNorm:
                        textureTool.Convert(texImage, PixelFormat.R16_UNorm);
                        break;

                    case PixelFormat.R16G16B16A16_SNorm:
                    case PixelFormat.R16G16_SNorm:
                        textureTool.Convert(texImage, PixelFormat.R16_SNorm);
                        break;

                    case PixelFormat.R8_SNorm:
                    case PixelFormat.B8G8R8A8_UNorm:
                    case PixelFormat.B8G8R8X8_UNorm:
                    case PixelFormat.R8G8B8A8_UNorm:
                    case PixelFormat.R8G8_UNorm:

                    case PixelFormat.R8G8B8A8_SNorm:
                    case PixelFormat.R8G8_SNorm:

                    case PixelFormat.B8G8R8A8_UNorm_SRgb:
                    case PixelFormat.B8G8R8X8_UNorm_SRgb:
                    case PixelFormat.R8G8B8A8_UNorm_SRgb:
                        textureTool.Convert(texImage, PixelFormat.R8_UNorm);
                        break;

                    default:
                        commandContext.Logger.Warning($"{texImage.Format} does not have a specific implementation. Falling back to float conversion");
                        textureTool.Convert(texImage, PixelFormat.R32_Float);
                        break;
                }

                using (var image = textureTool.ConvertToStrideImage(texImage))
                {
                    var pixelBuffer = image.GetPixelBuffer(0, 0, 0);

                    ReadOnlySpan<byte> heights;

                    minHeight = float.PositiveInfinity;
                    maxHeight = float.NegativeInfinity;
                    switch (image.Description.Format)
                    {
                        case PixelFormat.R32_Float:
                            var floats = pixelBuffer.GetPixels<float>();
                            MathUtil.FindMinMax(floats, out minHeight, out maxHeight);

                            // See RuntimeLayer.Heights
                            foreach (ref var f in floats.AsSpan())
                                f *= HeightMultiplier;

                            heights = MemoryMarshal.Cast<float, byte>(floats.AsSpan());
                            break;

                        case PixelFormat.R16_Float:
                            var halfs = pixelBuffer.GetPixels<Half>();
                            foreach (var v in halfs)
                            {
                                minHeight = MathF.Min(minHeight, (float)v);
                                maxHeight = MathF.Max(maxHeight, (float)v);
                            }

                            // See RuntimeLayer.Heights
                            foreach (ref var f in halfs.AsSpan())
                                f = (Half)((float)f * HeightMultiplier);

                            heights = MemoryMarshal.Cast<Half, byte>(halfs.AsSpan());
                            break;

                        case PixelFormat.R16_SNorm:
                            var shorts = pixelBuffer.GetPixels<short>();
                            MathUtil.FindMinMax(shorts, out var minShort, out var maxShort);
                            minHeight = minShort;
                            maxHeight = maxShort;

                            heights = MemoryMarshal.Cast<short, byte>(shorts);
                            break;

                        case PixelFormat.R16_UNorm:
                            var ushorts = pixelBuffer.GetPixels<ushort>();
                            MathUtil.FindMinMax(ushorts, out var minUShort, out var maxUShort);
                            minHeight = minUShort;
                            maxHeight = maxUShort;

                            heights = MemoryMarshal.Cast<ushort, byte>(ushorts);
                            break;

                        case PixelFormat.R8_UNorm:
                            var bytes = pixelBuffer.GetPixels<byte>();
                            MathUtil.FindMinMax(bytes, out var minByte, out var maxByte);
                            minHeight = minByte;
                            maxHeight = maxByte;

                            heights = bytes;
                            break;

                        default:
                            throw new InvalidOperationException();
                    }
                    minHeight *= HeightMultiplier;
                    maxHeight *= HeightMultiplier;

                    var description = image.Description;

                    runtimeLayer = new RuntimeLayer
                    {
                        BufferResolution = new Int2(description.Width, description.Height),
                        Texture = Texture,
                        Tiling = Tiling,
                        BilinearSampling = BilinearSampling,
                        HeightMultiplier = HeightMultiplier,
                        Heights = heights.ToArray(),
                        HeightsFormat = image.Description.Format,
                        HeightfieldSubdivision = subdivision
                    };
                }
            }
        }
    }

    /// <summary>
    /// Post-compilation representation of a <see cref="HeightfieldTexture"/>  
    /// </summary>
    [DataContract]
    public record RuntimeLayer : HeightfieldTextureSharedData, IHeightfieldRuntimeLayer
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
                (PixelFormat.R32_Float, 1f) => Build<float, FloatToFloat, AndWrap/*NoWrap*/>(),
                (PixelFormat.R16_Float, 1f) => Build<Half, HalfToFloat, AndWrap/*NoWrap*/>(),
                (PixelFormat.R16_SNorm, 1f) => Build<short, ShortToFloat, AndWrap/*NoWrap*/>(),
                (PixelFormat.R16_UNorm, 1f) => Build<ushort, UShortToFloat, AndWrap/*NoWrap*/>(),
                (PixelFormat.R8_UNorm, 1f) => Build<byte, ByteToFloat, AndWrap/*NoWrap*/>(),

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
            var rezToBuff = ((Vector2)BufferResolution) / HeightfieldSubdivision * Tiling;
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
            if (Texture == null! || string.IsNullOrEmpty(Texture.Url))
                return new ComputeFloat(HeightMultiplier);

            // - We need the texture here to feed the ComputeScalar.
            // - We can't hold a direct reference to the texture: loading this record through Content.Load<HeightfieldAsset> would require a graphics context
            //   to load the reference. Which it doesn't have when building a game and setting up the associated material through 'HeightfieldDisplacementFeature'
            // - Instead we'll create a proxy, like the serializer does, and let the engine bind the texture from its url/id
            var texture = AttachedReferenceManager.CreateProxyObject<Texture>(Texture.Id, Texture.Url);

            var textureScalar = new ComputeTextureScalar(texture, TextureCoordinate.Texcoord0, new Stride.Core.Mathematics.Vector2(Tiling), default)
            {
                Filtering = BilinearSampling ? TextureFilter.Linear : TextureFilter.Point,
                AddressModeU = TextureAddressMode.Wrap,
                AddressModeV = TextureAddressMode.Wrap,
            };
            return new ComputeBinaryScalar(textureScalar, new ComputeFloat(HeightMultiplier), BinaryOperator.Multiply);
        }
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

                    Debug.Assert(index < Heights.Length);

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
                    int x1 = TWrapper.Wrap(x0 + 1, Mask.X);
                    int y1 = TWrapper.Wrap(y0 + 1, Mask.Y);

                    Debug.Assert(x0 + y0 < Heights.Length && x0 + y1 < Heights.Length && x1 + y0 < Heights.Length && x1 + y1 < Heights.Length);

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
