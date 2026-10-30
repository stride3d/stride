// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.BuildEngine;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Graphics;
using Stride.Graphics.Data;
using Stride.TextureConverter;
using Half = System.Half;

namespace Stride.Heightfield.Assets;

/// <summary>
/// Heightfield built from a texture
/// </summary>
[DataContract]
[Display("Heightfield Texture")]
public record HeightfieldTexture : RuntimeLayer.HeightfieldTextureSharedData, IHeightfieldLayerBuilder
{
    /// <inheritdoc/>
    public IEnumerable<ObjectUrl> GetInputFiles()
    {
        if (Texture is not null)
        {
            yield return new ObjectUrl(UrlType.Content, AttachedReferenceManager.GetUrl(Texture) ?? throw new NullReferenceException());
        }
    }

    /// <inheritdoc/>
    public void BuildRuntimeRepresentation(float size, int subdivision, ICommandContext commandContext, IAssetFinder assetFinder, out IHeightfieldRuntimeLayer runtimeLayer, out float minHeight, out float maxHeight)
    {
        if (Texture is null)
        {
            runtimeLayer = new RuntimeLayer.TextureRuntimeLayer
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

        var content = new ContentManager(MicrothreadLocalDatabases.ProviderService);
        var loadedTexture = content.Load<Texture>(AttachedReferenceManager.GetUrl(Texture) ?? throw new NullReferenceException(), ContentManagerLoaderSettings.StreamingDisabled);
        var baseImage = loadedTexture.GetSerializationData().Image;

        using (var textureTool = new TextureTool())
        using (var texImage = textureTool.Load(baseImage, baseImage.Description.Format.IsSRgb))
        {
            if (texImage.Format.IsCompressed)
                textureTool.Decompress(texImage, texImage.Format.IsSRgb);

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

                        // See TextureRuntimeLayer.Heights
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

                        // See TextureRuntimeLayer.Heights
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

                runtimeLayer = new RuntimeLayer.TextureRuntimeLayer
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
