// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2012 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;

using static Stride.Graphics.PixelFormat;

namespace Stride.Graphics
{
    /// <summary>
    ///   Provides extensions for <see cref="PixelFormat"/> to help in conversions between formats, querying format
    ///   information, calculating sizes, pitches, etc.
    /// </summary>
    public static class PixelFormatExtensions
    {
        private readonly record struct PixelFormatSizeInfo(byte IsCompressed, byte BlockWidth, byte BlockHeight, byte BlockSize);

        private static readonly PixelFormatSizeInfo[] sizeInfos = new PixelFormatSizeInfo[256];
        private static readonly bool[] srgbFormats = new bool[256];
        private static readonly bool[] hdrFormats = new bool[256];
        private static readonly bool[] alpha32Formats = new bool[256];
        private static readonly bool[] typelessFormats = new bool[256];
        private static readonly Dictionary<PixelFormat, PixelFormat> sRgbConversion;
        
        private static int GetIndex(PixelFormat format)
        {
            // DirectX official pixel formats (0..115 use 0..127 in the arrays)
            // Custom pixel formats (1024..1151? use 128..255 in the array)
            if ((int) format >= 1024)
                return (int) format - 1024 + 128;

            return (int) format;
        }

        public static int BlockSize(this PixelFormat format)
        {
            return sizeInfos[GetIndex(format)].BlockSize;
        }

        public static int BlockWidth(this PixelFormat format)
        {
            return sizeInfos[GetIndex(format)].BlockWidth;
        }

        public static int BlockHeight(this PixelFormat format)
        {
            return sizeInfos[GetIndex(format)].BlockHeight;
        }

        /// <summary>
        /// Calculates the size of a <see cref="PixelFormat"/> in bytes.
        /// </summary>
        /// <param name="format">The dxgi format.</param>
        /// <returns>size of in bytes</returns>
        public static int SizeInBytes(this PixelFormat format)
        {
            var sizeInfo = sizeInfos[GetIndex(format)];
            return sizeInfo.BlockSize / (sizeInfo.BlockWidth * sizeInfo.BlockHeight);
        }

        /// <summary>
        /// Calculates the size of a <see cref="PixelFormat"/> in bits.
        /// </summary>
        /// <param name="format">The pixel format.</param>
        /// <returns>The size in bits</returns>
        public static int SizeInBits(this PixelFormat format)
        {
            var sizeInfo = sizeInfos[GetIndex(format)];
            return sizeInfo.BlockSize * 8 / (sizeInfo.BlockWidth * sizeInfo.BlockHeight);
        }

        /// <summary>
        /// Calculate the size of the alpha channel in bits depending on the pixel format.
        /// </summary>
        /// <param name="format">The pixel format</param>
        /// <returns>The size in bits</returns>
        public static int AlphaSizeInBits(this PixelFormat format)
        {
            switch (format)
            {
                case R32G32B32A32_Typeless:
                case R32G32B32A32_Float:
                case R32G32B32A32_UInt:
                case R32G32B32A32_SInt:
                    return 32;

                case R16G16B16A16_Typeless:
                case R16G16B16A16_Float:
                case R16G16B16A16_UNorm:
                case R16G16B16A16_UInt:
                case R16G16B16A16_SNorm:
                case R16G16B16A16_SInt:
                    return 16;

                case R10G10B10A2_Typeless:
                case R10G10B10A2_UNorm:
                case R10G10B10A2_UInt:
                case R10G10B10_Xr_Bias_A2_UNorm:
                    return 2;

                case R8G8B8A8_Typeless:
                case R8G8B8A8_UNorm:
                case R8G8B8A8_UNorm_SRgb:
                case R8G8B8A8_UInt:
                case R8G8B8A8_SNorm:
                case R8G8B8A8_SInt:
                case B8G8R8A8_UNorm:
                case B8G8R8A8_Typeless:
                case B8G8R8A8_UNorm_SRgb:
                case A8_UNorm:
                    return 8;

                case (PixelFormat) 115: // DXGI_FORMAT_B4G4R4A4_UNORM
                    return 4;

                case B5G5R5A1_UNorm:
                    return 1;

                case BC1_Typeless:
                case BC1_UNorm:
                case BC1_UNorm_SRgb:
                    return 1;  // or 0

                case BC2_Typeless:
                case BC2_UNorm:
                case BC2_UNorm_SRgb:
                    return 4;

                case BC3_Typeless:
                case BC3_UNorm:
                case BC3_UNorm_SRgb:
                    return 8;

                case BC7_Typeless:
                case BC7_UNorm:
                case BC7_UNorm_SRgb:
                    return 8;  // or 0

                case ETC2_RGBA:
                case ETC2_RGBA_SRgb:
                    return 8;

                case ETC2_RGB_A1:
                    return 1;
            }
            return 0;
        }

        /// <summary>
        /// Returns true if the <see cref="PixelFormat"/> is valid.
        /// </summary>
        /// <param name="format">A format to validate</param>
        /// <returns>True if the <see cref="PixelFormat"/> is valid.</returns>
        public static bool IsValid(this PixelFormat format)
        {
            return ((int) format >= 1    && (int) format <= 115)   // DirectX formats
                || ((int) format >= 1088 && (int) format <= 1097); // ETC formats
        }

        /// <summary>
        /// Returns true if the <see cref="PixelFormat"/> is a compressed format.
        /// </summary>
        /// <param name="format">The format to check for compressed.</param>
        /// <returns>True if the <see cref="PixelFormat"/> is a compressed format</returns>
        public static bool IsCompressed(this PixelFormat format)
        {
            return sizeInfos[GetIndex(format)].IsCompressed == 1;
        }

        /// <summary>
        /// Returns true if the <see cref="PixelFormat"/> is an uncompressed 32-bit color with an Alpha channel.
        /// </summary>
        /// <param name="format">The format to check for an uncompressed 32-bit color with an Alpha channel.</param>
        /// <returns>True if the <see cref="PixelFormat"/> is an uncompressed 32-bit color with an Alpha channel</returns>
        public static bool HasAlpha32Bits(this PixelFormat format)
        {
            return alpha32Formats[GetIndex(format)];
        }

        /// <summary>
        /// Returns true if the <see cref="PixelFormat"/> has an Alpha channel.
        /// </summary>
        /// <param name="format">The format to check for an Alpha channel.</param>
        /// <returns>True if the <see cref="PixelFormat"/> has an Alpha channel</returns>
        public static bool HasAlpha(this PixelFormat format)
        {
            return AlphaSizeInBits(format) != 0;
        }

        /// <summary>
        /// Determines whether the specified <see cref="PixelFormat"/> is packed.
        /// </summary>
        /// <param name="format">The DXGI Format.</param>
        /// <returns><c>true</c> if the specified <see cref="PixelFormat"/> is packed; otherwise, <c>false</c>.</returns>
        public static bool IsPacked(this PixelFormat format)
        {
            return format is R8G8_B8G8_UNorm or G8R8_G8B8_UNorm;
        }

        /// <summary>
        /// Determines whether the specified <see cref="PixelFormat"/> is video.
        /// </summary>
        /// <param name="format">The <see cref="PixelFormat"/>.</param>
        /// <returns><c>true</c> if the specified <see cref="PixelFormat"/> is video; otherwise, <c>false</c>.</returns>
        public static bool IsVideo(this PixelFormat format)
        {
#if DIRECTX11_1
            switch (format)
            {
                case PixelFormat.AYUV:
                case PixelFormat.Y410:
                case PixelFormat.Y416:
                case PixelFormat.NV12:
                case PixelFormat.P010:
                case PixelFormat.P016:
                case PixelFormat.YUY2:
                case PixelFormat.Y210:
                case PixelFormat.Y216:
                case PixelFormat.NV11:
                    // These video formats can be used with the 3D pipeline through special view mappings
                    return true;

                case PixelFormat.Opaque420:
                case PixelFormat.AI44:
                case PixelFormat.IA44:
                case PixelFormat.P8:
                case PixelFormat.A8P8:
                    // These are limited use video formats not usable in any way by the 3D pipeline
                    return true;

                default:
                    return false;
                }
#else
            // !DXGI_1_2_FORMATS
            return false;
#endif
        }

        /// <summary>
        /// Determines whether the specified <see cref="PixelFormat"/> is a SRGB format.
        /// </summary>
        /// <param name="format">The <see cref="PixelFormat"/>.</param>
        /// <returns><c>true</c> if the specified <see cref="PixelFormat"/> is a SRGB format; otherwise, <c>false</c>.</returns>
        public static bool IsSRgb(this PixelFormat format)
        {
            return srgbFormats[GetIndex(format)];
        }

        /// <summary>
        /// Determines whether the specified <see cref="PixelFormat"/> is HDR (either 16 or 32bits float)
        /// </summary>
        /// <param name="format">The FMT.</param>
        /// <returns><c>true</c> if the specified pixel format is HDR (floating point); otherwise, <c>false</c>.</returns>
        public static bool IsHDR(this PixelFormat format)
        {
            return hdrFormats[GetIndex(format)];
        }

        /// <summary>
        /// Determines whether the specified <see cref="PixelFormat"/> is typeless.
        /// </summary>
        /// <param name="format">The <see cref="PixelFormat"/>.</param>
        /// <returns><c>true</c> if the specified <see cref="PixelFormat"/> is typeless; otherwise, <c>false</c>.</returns>
        public static bool IsTypeless(this PixelFormat format)
        {
            return typelessFormats[GetIndex(format)];
        }

        public static void ComputePitch(this PixelFormat format, int width, int height, out int rowPitch, out int slicePitch)
        {
            var sizeInfo = sizeInfos[GetIndex(format)];

            rowPitch = ((width + sizeInfo.BlockWidth - 1) / sizeInfo.BlockWidth) * sizeInfo.BlockSize;
            slicePitch = rowPitch * ((height + sizeInfo.BlockHeight - 1) / sizeInfo.BlockHeight);
        }

        /// <summary>
        /// Determine if the format has an equivalent sRGB format.
        /// </summary>
        /// <param name="format">the non-sRGB format</param>
        /// <returns>true if the format has an sRGB equivalent</returns>
        public static bool HasSRgbEquivalent(this PixelFormat format)
        {
            if (format.IsSRgb())
                throw new ArgumentException($"'{format}' is already an sRGB pixel format", nameof(format));

            return sRgbConversion.ContainsKey(format);
        }

        /// <summary>
        /// Determine if the format has an equivalent non-sRGB format.
        /// </summary>
        /// <param name="format">the sRGB format</param>
        /// <returns>true if the format has an non-sRGB equivalent</returns>
        public static bool HasNonSRgbEquivalent(this PixelFormat format)
        {
            if (!format.IsSRgb())
                throw new ArgumentException($"'{format}' is not a sRGB format", nameof(format));

            return sRgbConversion.ContainsKey(format);
        }

        /// <summary>
        /// Find the equivalent sRGB format to the provided format.
        /// </summary>
        /// <param name="format">The non sRGB format.</param>
        /// <returns>
        /// The equivalent sRGB format if any, the provided format else.
        /// </returns>
        public static PixelFormat ToSRgb(this PixelFormat format)
        {
            if (format.IsSRgb() || !sRgbConversion.TryGetValue(format, out var srgbFormat))
                return format;

            return srgbFormat;
        }

        /// <summary>
        /// Find the equivalent non sRGB format to the provided sRGB format.
        /// </summary>
        /// <param name="format">The non sRGB format.</param>
        /// <returns>
        /// The equivalent non sRGB format if any, the provided format else.
        /// </returns>
        public static PixelFormat ToNonSRgb(this PixelFormat format)
        {
            if (!format.IsSRgb() || !sRgbConversion.TryGetValue(format, out var nonSrgbFormat))
                return format;

            return nonSrgbFormat;
        }

        /// <summary>
        /// Determines whether the specified format has components in the RGBA order.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <returns>
        ///   <c>true</c> if the specified format has components in the RGBA order; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsRgbaOrder(this PixelFormat format)
        {
            switch (format)
            {
                case R32G32B32A32_Typeless:
                case R32G32B32A32_Float:
                case R32G32B32A32_UInt:
                case R32G32B32A32_SInt:
                case R32G32B32_Typeless:
                case R32G32B32_Float:
                case R32G32B32_UInt:
                case R32G32B32_SInt:
                case R16G16B16A16_Typeless:
                case R16G16B16A16_Float:
                case R16G16B16A16_UNorm:
                case R16G16B16A16_UInt:
                case R16G16B16A16_SNorm:
                case R16G16B16A16_SInt:
                case R32G32_Typeless:
                case R32G32_Float:
                case R32G32_UInt:
                case R32G32_SInt:
                case R32G8X24_Typeless:
                case R10G10B10A2_Typeless:
                case R10G10B10A2_UNorm:
                case R10G10B10A2_UInt:
                case R11G11B10_Float:
                case R8G8B8A8_Typeless:
                case R8G8B8A8_UNorm:
                case R8G8B8A8_UNorm_SRgb:
                case R8G8B8A8_UInt:
                case R8G8B8A8_SNorm:
                case R8G8B8A8_SInt:
                    return true;
                
                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines whether the specified format has components in the BGRA order.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <returns>
        ///   <c>true</c> if the specified format has components in the BGRA order; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsBgraOrder(this PixelFormat format)
        {
            switch (format)
            {
                case B8G8R8A8_UNorm:
                case B8G8R8X8_UNorm:
                case B8G8R8A8_Typeless:
                case B8G8R8A8_UNorm_SRgb:
                case B8G8R8X8_Typeless:
                case B8G8R8X8_UNorm_SRgb:
                    return true;
                
                default:
                    return false;
            }
        }

        /// <summary>
        /// Static initializer to speed up size calculation (not sure the JIT is enough "smart" for this kind of thing).
        /// </summary>
        static PixelFormatExtensions()
        {
            InitFormat(
            [
                A8_UNorm,
                R8_SInt,
                R8_SNorm,
                R8_Typeless,
                R8_UInt,
                R8_UNorm
            ],
            pixelSize: 1);

            InitFormat(
            [
                B5G5R5A1_UNorm,
                B5G6R5_UNorm,
                D16_UNorm,
                R16_Float,
                R16_SInt,
                R16_SNorm,
                R16_Typeless,
                R16_UInt,
                R16_UNorm,
                R8G8_SInt,
                R8G8_SNorm,
                R8G8_Typeless,
                R8G8_UInt,
                R8G8_UNorm,
#if DIRECTX11_1
                B4G4R4A4_UNorm
#endif
            ],
            pixelSize: 2);

            InitFormat(
            [
                B8G8R8X8_Typeless,
                B8G8R8X8_UNorm,
                B8G8R8X8_UNorm_SRgb,
                D24_UNorm_S8_UInt,
                D32_Float,
                D32_Float_S8X24_UInt,
                R10G10B10_Xr_Bias_A2_UNorm,
                R10G10B10A2_Typeless,
                R10G10B10A2_UInt,
                R10G10B10A2_UNorm,
                R11G11B10_Float,
                R16G16_Float,
                R16G16_SInt,
                R16G16_SNorm,
                R16G16_Typeless,
                R16G16_UInt,
                R16G16_UNorm,
                R24_UNorm_X8_Typeless,
                R24G8_Typeless,
                R32_Float,
                R32_Float_X8X24_Typeless,
                R32_SInt,
                R32_Typeless,
                R32_UInt,
                R8G8B8A8_SInt,
                R8G8B8A8_SNorm,
                R8G8B8A8_Typeless,
                R8G8B8A8_UInt,
                R8G8B8A8_UNorm,
                R8G8B8A8_UNorm_SRgb,
                B8G8R8A8_Typeless,
                B8G8R8A8_UNorm,
                B8G8R8A8_UNorm_SRgb,
                R9G9B9E5_Sharedexp,
                X24_Typeless_G8_UInt,
                X32_Typeless_G8X24_UInt
            ],
            pixelSize: 4);

            InitFormat(
            [
                R16G16B16A16_Float,
                R16G16B16A16_SInt,
                R16G16B16A16_SNorm,
                R16G16B16A16_Typeless,
                R16G16B16A16_UInt,
                R16G16B16A16_UNorm,
                R32G32_Float,
                R32G32_SInt,
                R32G32_Typeless,
                R32G32_UInt,
                R32G8X24_Typeless
            ],
            pixelSize: 8);

            InitFormat(
            [
                R32G32B32_Float,
                R32G32B32_SInt,
                R32G32B32_Typeless,
                R32G32B32_UInt
            ],
            pixelSize: 12);

            InitFormat(
            [
                R32G32B32A32_Float,
                R32G32B32A32_SInt,
                R32G32B32A32_Typeless,
                R32G32B32A32_UInt
            ],
            pixelSize: 16);

            // Compressed formats
            InitBlockFormat(
            [
                BC1_Typeless,
                BC1_UNorm,
                BC1_UNorm_SRgb,
                BC4_SNorm,
                BC4_Typeless,
                BC4_UNorm,
                ETC1,
                ETC2_RGB,
                ETC2_RGB_SRgb,
                ETC2_RGB_A1,
                EAC_R11_Unsigned,
                EAC_R11_Signed
            ],
            blockSize: 8, blockWidth: 4, blockHeight: 4);

            InitBlockFormat(
            [
                BC2_Typeless,
                BC2_UNorm,
                BC2_UNorm_SRgb,
                BC3_Typeless,
                BC3_UNorm,
                BC3_UNorm_SRgb,
                BC5_SNorm,
                BC5_Typeless,
                BC5_UNorm,
                BC6H_Sf16,
                BC6H_Typeless,
                BC6H_Uf16,
                BC7_Typeless,
                BC7_UNorm,
                BC7_UNorm_SRgb,
                ETC2_RGBA,
                EAC_RG11_Unsigned,
                EAC_RG11_Signed,
                ETC2_RGBA_SRgb
            ],
            blockSize: 16, blockWidth: 4, blockHeight: 4);

            InitBlockFormat(
            [
                R8G8_B8G8_UNorm,
                G8R8_G8B8_UNorm
            ],
            blockSize: 4, blockWidth: 2, blockHeight: 1);

            InitBlockFormat(
            [
                R1_UNorm
            ],
            blockSize: 1, blockWidth: 8, blockHeight: 1);

            // sRGB formats
            InitDefaults(
            [
                R8G8B8A8_UNorm_SRgb,
                BC1_UNorm_SRgb,
                BC2_UNorm_SRgb,
                BC3_UNorm_SRgb,
                B8G8R8A8_UNorm_SRgb,
                B8G8R8X8_UNorm_SRgb,
                BC7_UNorm_SRgb,
                ETC2_RGBA_SRgb,
                ETC2_RGB_SRgb
            ], 
            outputArray: srgbFormats);

            // Alpha formats
            InitDefaults(
            [
                R8G8B8A8_UNorm,
                R8G8B8A8_UNorm_SRgb,
                B8G8R8A8_UNorm,
                B8G8R8A8_UNorm_SRgb
            ],
            outputArray: alpha32Formats);

            // HDR formats
            InitDefaults(
            [
                R16G16B16A16_Float,
                R32G32B32A32_Float,
                R16G16B16A16_Float,
                R16G16_Float,
                R16_Float,
                BC6H_Sf16,
                BC6H_Uf16
            ], 
            outputArray: hdrFormats);

            // Typeless formats
            InitDefaults(
            [
                R32G32B32A32_Typeless,
                R32G32B32_Typeless,
                R16G16B16A16_Typeless,
                R32G32_Typeless,
                R32G8X24_Typeless,
                R32_Float_X8X24_Typeless,
                X32_Typeless_G8X24_UInt,
                R10G10B10A2_Typeless,
                R8G8B8A8_Typeless,
                R16G16_Typeless,
                R32_Typeless,
                R24G8_Typeless,
                R24_UNorm_X8_Typeless,
                X24_Typeless_G8_UInt,
                R8G8_Typeless,
                R16_Typeless,
                R8_Typeless,
                BC1_Typeless,
                BC2_Typeless,
                BC3_Typeless,
                BC4_Typeless,
                BC5_Typeless,
                B8G8R8A8_Typeless,
                B8G8R8X8_Typeless,
                BC6H_Typeless,
                BC7_Typeless
            ],
            outputArray: typelessFormats);

            sRgbConversion = new Dictionary<PixelFormat, PixelFormat>
            {
                { R8G8B8A8_UNorm_SRgb,  R8G8B8A8_UNorm },
                { R8G8B8A8_UNorm,       R8G8B8A8_UNorm_SRgb },
                { BC1_UNorm_SRgb,       BC1_UNorm },
                { BC1_UNorm,            BC1_UNorm_SRgb },
                { BC2_UNorm_SRgb,       BC2_UNorm },
                { BC2_UNorm,            BC2_UNorm_SRgb },
                { BC3_UNorm_SRgb,       BC3_UNorm },
                { BC3_UNorm,            BC3_UNorm_SRgb },
                { B8G8R8A8_UNorm_SRgb,  B8G8R8A8_UNorm },
                { B8G8R8A8_UNorm,       B8G8R8A8_UNorm_SRgb },
                { B8G8R8X8_UNorm_SRgb,  B8G8R8X8_UNorm },
                { B8G8R8X8_UNorm,       B8G8R8X8_UNorm_SRgb },
                { BC7_UNorm_SRgb,       BC7_UNorm },
                { BC7_UNorm,            BC7_UNorm_SRgb },
                { ETC2_RGBA_SRgb,       ETC2_RGBA },
                { ETC2_RGBA,            ETC2_RGBA_SRgb },
                { ETC2_RGB_SRgb,        ETC2_RGB },
                { ETC2_RGB,             ETC2_RGB_SRgb }
            };
            return;

            static void InitFormat(ReadOnlySpan<PixelFormat> formats, byte pixelSize)
            {
                foreach (var format in formats)
                    sizeInfos[GetIndex(format)] = new PixelFormatSizeInfo(BlockSize: pixelSize, BlockWidth: 1, BlockHeight: 1, IsCompressed: 0);
            }
            
            static void InitBlockFormat(ReadOnlySpan<PixelFormat> formats, byte blockSize, byte blockWidth, byte blockHeight)
            {
                foreach (var format in formats)
                    sizeInfos[GetIndex(format)] = new PixelFormatSizeInfo(BlockSize: blockSize, BlockWidth: blockWidth, BlockHeight: blockHeight, IsCompressed: 1);
            }
            
            static void InitDefaults(ReadOnlySpan<PixelFormat> formats, bool[] outputArray)
            {
                foreach (var format in formats)
                    outputArray[GetIndex(format)] = true;
            }
        }
    }
}
