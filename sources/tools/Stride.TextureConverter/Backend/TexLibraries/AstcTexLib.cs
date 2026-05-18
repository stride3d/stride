// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Graphics;
using Stride.TextureConverter.AstcEncWrapper;
using Stride.TextureConverter.Requests;

namespace Stride.TextureConverter.TexLibraries
{
    /// <summary>
    /// Texture library backend wrapping the ARM astc-encoder (astcenc) shared library.
    /// Handles compression to and decompression from the <c>ASTC_*</c> family of <see cref="PixelFormat"/> values.
    /// astcenc operates directly on raw R8G8B8A8 buffers; no persistent native handle is kept.
    /// </summary>
    internal sealed class AstcTexLib : ITexLibrary
    {
        private static readonly Logger Log = GlobalLogger.GetLogger(nameof(AstcTexLib));

        public void Dispose() { }
        public void Dispose(TexImage image) { }
        public void StartLibrary(TexImage image) { }
        public void EndLibrary(TexImage image) { }

        public bool SupportBGRAOrder() => false;

        public bool CanHandleRequest(TexImage image, IRequest request) => CanHandleRequest(image.Format, request);

        public bool CanHandleRequest(PixelFormat format, IRequest request)
        {
            switch (request.Type)
            {
                case RequestType.Compressing:
                    return IsAstcFormat(((CompressingRequest)request).Format) && IsCompressibleSourceFormat(format);
                case RequestType.Decompressing:
                    return IsAstcFormat(format);
                default:
                    return false;
            }
        }

        public void Execute(TexImage image, IRequest request)
        {
            switch (request.Type)
            {
                case RequestType.Compressing:
                    Compress(image, (CompressingRequest)request);
                    break;
                case RequestType.Decompressing:
                    Decompress(image);
                    break;
                default:
                    throw new TextureToolsException($"AstcTexLib can't handle request: {request.Type}");
            }
        }

        private unsafe void Compress(TexImage image, CompressingRequest request)
        {
            if (!IsCompressibleSourceFormat(image.Format))
                throw new TextureToolsException($"AstcTexLib cannot compress from {image.Format}; expected R8G8B8A8_UNorm[/SRgb].");

            GetBlockDimensions(request.Format, out var blockX, out var blockY);
            var profile = request.Format.IsSRgb ? AstcEncNative.AstcEncProfile.LdrSrgb : AstcEncNative.AstcEncProfile.Ldr;
            var quality = MapQuality(request.Quality);

            Log.Verbose($"Compressing to {request.Format} ({blockX}x{blockY}, profile={profile}, quality={quality}) ...");

            // Allocate the config struct on the stack (opaque blob; astcenc fills it in).
            var configBuf = stackalloc byte[AstcEncNative.ConfigStructSize];
            var err = AstcEncNative.ConfigInit(profile, (uint)blockX, (uint)blockY, 1, quality, 0, configBuf);
            if (err != AstcEncNative.AstcEncError.Success)
                throw new TextureToolsException($"astcenc_config_init failed: {AstcEncNative.GetErrorMessage(err)}");

            err = AstcEncNative.ContextAlloc(configBuf, threadCount: 1, out var context);
            if (err != AstcEncNative.AstcEncError.Success)
                throw new TextureToolsException($"astcenc_context_alloc failed: {AstcEncNative.GetErrorMessage(err)}");

            try
            {
                // First pass: compute the size of every output subimage and the total.
                var subCount = image.SubImageArray.Length;
                var outSizes = new int[subCount];
                long totalOut = 0;
                for (int i = 0; i < subCount; i++)
                {
                    var sub = image.SubImageArray[i];
                    int blocksX = (sub.Width + blockX - 1) / blockX;
                    int blocksY = (sub.Height + blockY - 1) / blockY;
                    outSizes[i] = blocksX * blocksY * 16;
                    totalOut += outSizes[i];
                }

                var outBuf = Marshal.AllocHGlobal((nint)totalOut);
                try
                {
                    long writeOffset = 0;
                    var swizzle = AstcEncNative.Swizzle.Rgba;
                    var newSubImages = new TexImage.SubImage[subCount];

                    for (int i = 0; i < subCount; i++)
                    {
                        var sub = image.SubImageArray[i];
                        // astcenc_image.data is a void** of length DimZ; for 2D slices that's a single pointer.
                        void* slicePtr = (void*)sub.Data;
                        void** slicePtrArr = &slicePtr;

                        var astcImage = new AstcEncNative.AstcImage
                        {
                            DimX = (uint)sub.Width,
                            DimY = (uint)sub.Height,
                            DimZ = 1,
                            DataType = AstcEncNative.AstcEncDataType.U8,
                            Data = slicePtrArr,
                        };

                        var dst = (byte*)outBuf + writeOffset;
                        err = AstcEncNative.CompressImage(context, ref astcImage, swizzle, dst, (nuint)outSizes[i], threadIndex: 0);
                        if (err != AstcEncNative.AstcEncError.Success)
                            throw new TextureToolsException($"astcenc_compress_image failed for mip {i}: {AstcEncNative.GetErrorMessage(err)}");

                        AstcEncNative.CompressReset(context);

                        int blocksX = (sub.Width + blockX - 1) / blockX;
                        newSubImages[i] = new TexImage.SubImage
                        {
                            Width = sub.Width,
                            Height = sub.Height,
                            RowPitch = blocksX * 16,
                            SlicePitch = outSizes[i],
                            DataSize = outSizes[i],
                            Data = (IntPtr)dst,
                        };

                        writeOffset += outSizes[i];
                    }

                    // Replace image storage. Free old buffer if we own it.
                    image.DisposingLibrary?.Dispose(image);
                    Marshal.FreeHGlobal(image.Data);

                    image.Data = outBuf;
                    image.DataSize = (int)totalOut;
                    image.Format = request.Format;
                    image.SubImageArray = newSubImages;
                    Tools.ComputePitch(image.Format, image.Width, image.Height, out var rowPitch, out var slicePitch);
                    image.RowPitch = rowPitch;
                    image.SlicePitch = slicePitch;
                    image.DisposingLibrary = this;
                }
                catch
                {
                    Marshal.FreeHGlobal(outBuf);
                    throw;
                }
            }
            finally
            {
                AstcEncNative.ContextFree(context);
            }
        }

        private unsafe void Decompress(TexImage image)
        {
            if (!IsAstcFormat(image.Format))
                throw new TextureToolsException($"AstcTexLib cannot decompress from {image.Format}.");

            GetBlockDimensions(image.Format, out var blockX, out var blockY);
            var profile = image.Format.IsSRgb ? AstcEncNative.AstcEncProfile.LdrSrgb : AstcEncNative.AstcEncProfile.Ldr;
            var outFormat = image.Format.IsSRgb ? PixelFormat.R8G8B8A8_UNorm_SRgb : PixelFormat.R8G8B8A8_UNorm;

            Log.Verbose($"Decompressing {image.Format} ({blockX}x{blockY}) -> {outFormat} ...");

            var configBuf = stackalloc byte[AstcEncNative.ConfigStructSize];
            var err = AstcEncNative.ConfigInit(profile, (uint)blockX, (uint)blockY, 1, AstcEncNative.QualityMedium, AstcEncNative.FlagDecompressOnly, configBuf);
            if (err != AstcEncNative.AstcEncError.Success)
                throw new TextureToolsException($"astcenc_config_init failed: {AstcEncNative.GetErrorMessage(err)}");

            err = AstcEncNative.ContextAlloc(configBuf, threadCount: 1, out var context);
            if (err != AstcEncNative.AstcEncError.Success)
                throw new TextureToolsException($"astcenc_context_alloc failed: {AstcEncNative.GetErrorMessage(err)}");

            try
            {
                var subCount = image.SubImageArray.Length;
                var outSizes = new int[subCount];
                long totalOut = 0;
                for (int i = 0; i < subCount; i++)
                {
                    var sub = image.SubImageArray[i];
                    outSizes[i] = sub.Width * sub.Height * 4;
                    totalOut += outSizes[i];
                }

                var outBuf = Marshal.AllocHGlobal((nint)totalOut);
                try
                {
                    long writeOffset = 0;
                    var swizzle = AstcEncNative.Swizzle.Rgba;
                    var newSubImages = new TexImage.SubImage[subCount];

                    for (int i = 0; i < subCount; i++)
                    {
                        var sub = image.SubImageArray[i];
                        var dst = (byte*)outBuf + writeOffset;
                        void* slicePtr = dst;
                        void** slicePtrArr = &slicePtr;

                        var astcImage = new AstcEncNative.AstcImage
                        {
                            DimX = (uint)sub.Width,
                            DimY = (uint)sub.Height,
                            DimZ = 1,
                            DataType = AstcEncNative.AstcEncDataType.U8,
                            Data = slicePtrArr,
                        };

                        err = AstcEncNative.DecompressImage(context, (byte*)sub.Data, (nuint)sub.DataSize, ref astcImage, swizzle, threadIndex: 0);
                        if (err != AstcEncNative.AstcEncError.Success)
                            throw new TextureToolsException($"astcenc_decompress_image failed for mip {i}: {AstcEncNative.GetErrorMessage(err)}");

                        AstcEncNative.DecompressReset(context);

                        newSubImages[i] = new TexImage.SubImage
                        {
                            Width = sub.Width,
                            Height = sub.Height,
                            RowPitch = sub.Width * 4,
                            SlicePitch = outSizes[i],
                            DataSize = outSizes[i],
                            Data = (IntPtr)dst,
                        };

                        writeOffset += outSizes[i];
                    }

                    image.DisposingLibrary?.Dispose(image);
                    Marshal.FreeHGlobal(image.Data);

                    image.Data = outBuf;
                    image.DataSize = (int)totalOut;
                    image.Format = outFormat;
                    image.SubImageArray = newSubImages;
                    Tools.ComputePitch(image.Format, image.Width, image.Height, out var rowPitch, out var slicePitch);
                    image.RowPitch = rowPitch;
                    image.SlicePitch = slicePitch;
                    image.DisposingLibrary = this;
                }
                catch
                {
                    Marshal.FreeHGlobal(outBuf);
                    throw;
                }
            }
            finally
            {
                AstcEncNative.ContextFree(context);
            }
        }

        private static bool IsCompressibleSourceFormat(PixelFormat format)
            => format == PixelFormat.R8G8B8A8_UNorm || format == PixelFormat.R8G8B8A8_UNorm_SRgb;

        private static bool IsAstcFormat(PixelFormat format)
            => (int)format >= (int)PixelFormat.ASTC_4x4_UNorm && (int)format <= (int)PixelFormat.ASTC_12x12_UNorm_SRgb;

        private static float MapQuality(TextureQuality quality) => quality switch
        {
            TextureQuality.Fast => AstcEncNative.QualityFast,
            TextureQuality.Normal => AstcEncNative.QualityMedium,
            TextureQuality.High => AstcEncNative.QualityThorough,
            _ => AstcEncNative.QualityMedium,
        };

        private static void GetBlockDimensions(PixelFormat format, out int blockX, out int blockY)
        {
            switch (format)
            {
                case PixelFormat.ASTC_4x4_UNorm:   case PixelFormat.ASTC_4x4_UNorm_SRgb:   blockX = 4;  blockY = 4;  break;
                case PixelFormat.ASTC_5x4_UNorm:   case PixelFormat.ASTC_5x4_UNorm_SRgb:   blockX = 5;  blockY = 4;  break;
                case PixelFormat.ASTC_5x5_UNorm:   case PixelFormat.ASTC_5x5_UNorm_SRgb:   blockX = 5;  blockY = 5;  break;
                case PixelFormat.ASTC_6x5_UNorm:   case PixelFormat.ASTC_6x5_UNorm_SRgb:   blockX = 6;  blockY = 5;  break;
                case PixelFormat.ASTC_6x6_UNorm:   case PixelFormat.ASTC_6x6_UNorm_SRgb:   blockX = 6;  blockY = 6;  break;
                case PixelFormat.ASTC_8x5_UNorm:   case PixelFormat.ASTC_8x5_UNorm_SRgb:   blockX = 8;  blockY = 5;  break;
                case PixelFormat.ASTC_8x6_UNorm:   case PixelFormat.ASTC_8x6_UNorm_SRgb:   blockX = 8;  blockY = 6;  break;
                case PixelFormat.ASTC_8x8_UNorm:   case PixelFormat.ASTC_8x8_UNorm_SRgb:   blockX = 8;  blockY = 8;  break;
                case PixelFormat.ASTC_10x5_UNorm:  case PixelFormat.ASTC_10x5_UNorm_SRgb:  blockX = 10; blockY = 5;  break;
                case PixelFormat.ASTC_10x6_UNorm:  case PixelFormat.ASTC_10x6_UNorm_SRgb:  blockX = 10; blockY = 6;  break;
                case PixelFormat.ASTC_10x8_UNorm:  case PixelFormat.ASTC_10x8_UNorm_SRgb:  blockX = 10; blockY = 8;  break;
                case PixelFormat.ASTC_10x10_UNorm: case PixelFormat.ASTC_10x10_UNorm_SRgb: blockX = 10; blockY = 10; break;
                case PixelFormat.ASTC_12x10_UNorm: case PixelFormat.ASTC_12x10_UNorm_SRgb: blockX = 12; blockY = 10; break;
                case PixelFormat.ASTC_12x12_UNorm: case PixelFormat.ASTC_12x12_UNorm_SRgb: blockX = 12; blockY = 12; break;
                default:
                    throw new TextureToolsException($"Not an ASTC PixelFormat: {format}");
            }
        }
    }
}
