// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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

using Stride.Core;

namespace Stride.Graphics
{
    /// <summary>
    /// Extensions to <see cref="PixelFormat"/>.
    /// </summary>
    public static class PixelFormatExtensions
    {
        private struct PixelFormatSizeInfo
        {
            public byte IsCompressed;
            public byte BlockWidth;
            public byte BlockHeight;
            public byte BlockSize;
        }

        private static readonly PixelFormatSizeInfo[] sizeInfos = new PixelFormatSizeInfo[256];
        private static readonly bool[] srgbFormats = new bool[256];
        private static readonly bool[] hdrFormats = new bool[256];
        private static readonly bool[] alpha32Formats = new bool[256];
        private static readonly bool[] typelessFormats = new bool[256];
        private static readonly Dictionary<PixelFormat, PixelFormat> sRgbConvertion;
        
        private static int GetIndex(PixelFormat format)
        {
            // DirectX official pixel formats (0..115 use 0..127 in the arrays)
            // Custom pixel formats (1024..1151? use 128..255 in the array)
            if ((int)format >= 1024)
                return (int)format - 1024 + 128;

            return (int)format;
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
            return (int)sizeInfo.BlockSize / ((int)sizeInfo.BlockWidth * (int)sizeInfo.BlockHeight);
        }

        /// <summary>
        /// Calculates the size of a <see cref="PixelFormat"/> in bits.
        /// </summary>
        /// <param name="format">The pixel format.</param>
        /// <returns>The size in bits</returns>
        public static int SizeInBits(this PixelFormat format)
        {
            var sizeInfo = sizeInfos[GetIndex(format)];
            return (int)sizeInfo.BlockSize * 8 / ((int)sizeInfo.BlockWidth * (int)sizeInfo.BlockHeight); ;
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
                case PixelFormat.R32G32B32A32_Typeless:
                case PixelFormat.R32G32B32A32_Float:
                case PixelFormat.R32G32B32A32_UInt:
                case PixelFormat.R32G32B32A32_SInt:
                    return 32;

                case PixelFormat.R16G16B16A16_Typeless:
                case PixelFormat.R16G16B16A16_Float:
                case PixelFormat.R16G16B16A16_UNorm:
                case PixelFormat.R16G16B16A16_UInt:
                case PixelFormat.R16G16B16A16_SNorm:
                case PixelFormat.R16G16B16A16_SInt:
                    return 16;

                case PixelFormat.R10G10B10A2_Typeless:
                case PixelFormat.R10G10B10A2_UNorm:
                case PixelFormat.R10G10B10A2_UInt:
                case PixelFormat.R10G10B10_Xr_Bias_A2_UNorm:
                    return 2;

                case PixelFormat.R8G8B8A8_Typeless:
                case PixelFormat.R8G8B8A8_UNorm:
                case PixelFormat.R8G8B8A8_UNorm_SRgb:
                case PixelFormat.R8G8B8A8_UInt:
                case PixelFormat.R8G8B8A8_SNorm:
                case PixelFormat.R8G8B8A8_SInt:
                case PixelFormat.B8G8R8A8_UNorm:
                case PixelFormat.B8G8R8A8_Typeless:
                case PixelFormat.B8G8R8A8_UNorm_SRgb:
                case PixelFormat.A8_UNorm:
                    return 8;

                case (PixelFormat)115: // DXGI_FORMAT_B4G4R4A4_UNORM
                    return 4;

                case PixelFormat.B5G5R5A1_UNorm:
                    return 1;

                case PixelFormat.BC1_Typeless:
                case PixelFormat.BC1_UNorm:
                case PixelFormat.BC1_UNorm_SRgb:
                    return 1;  // or 0

                case PixelFormat.BC2_Typeless:
                case PixelFormat.BC2_UNorm:
                case PixelFormat.BC2_UNorm_SRgb:
                    return 4;

                case PixelFormat.BC3_Typeless:
                case PixelFormat.BC3_UNorm:
                case PixelFormat.BC3_UNorm_SRgb:
                    return 8;

                case PixelFormat.BC7_Typeless:
                case PixelFormat.BC7_UNorm:
                case PixelFormat.BC7_UNorm_SRgb:
                    return 8;  // or 0

                case PixelFormat.ETC2_RGBA:
                case PixelFormat.ETC2_RGBA_SRgb:
                    return 8;

                case PixelFormat.ETC2_RGB_A1:
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
            return ((int)(format) >= 1 && (int)(format) <= 115) // DirectX formats
                || ((int)(format) >= 1024 && (int)(format) <= 1033) // PVRTC formats
                || ((int)(format) >= 1088 && (int)(format) <= 1097) // ETC formats
                || ((int)(format) >= 1120 && (int)(format) <= 1122); // ATITC formats
        }

        /// <summary>
        /// Returns true if the <see cref="PixelFormat"/> is a compressed format.
        /// </summary>
        /// <param name="fmt">The format to check for compressed.</param>
        /// <returns>True if the <see cref="PixelFormat"/> is a compressed format</returns>
        public static bool IsCompressed(this PixelFormat fmt)
        {
            return sizeInfos[GetIndex(fmt)].IsCompressed == 1;
        }

        /// <summary>
        /// Returns true if the <see cref="PixelFormat"/> is an uncompressed 32 bit color with an Alpha channel.
        /// </summary>
        /// <param name="fmt">The format to check for an uncompressed 32 bit color with an Alpha channel.</param>
        /// <returns>True if the <see cref="PixelFormat"/> is an uncompressed 32 bit color with an Alpha channel</returns>
        public static bool HasAlpha32Bits(this PixelFormat fmt)
        {
            return alpha32Formats[GetIndex(fmt)];
        }

        /// <summary>
        /// Returns true if the <see cref="PixelFormat"/> has an Alpha channel.
        /// </summary>
        /// <param name="fmt">The format to check for an Alpha channel.</param>
        /// <returns>True if the <see cref="PixelFormat"/> has an Alpha channel</returns>
        public static bool HasAlpha(this PixelFormat fmt)
        {
            return AlphaSizeInBits(fmt) != 0;
        }

        /// <summary>
        /// Determines whether the specified <see cref="PixelFormat"/> is packed.
        /// </summary>
        /// <param name="fmt">The DXGI Format.</param>
        /// <returns><c>true</c> if the specified <see cref="PixelFormat"/> is packed; otherwise, <c>false</c>.</returns>
        public static bool IsPacked(this PixelFormat fmt)
        {
            return ((fmt == PixelFormat.R8G8_B8G8_UNorm) || (fmt == PixelFormat.G8R8_G8B8_UNorm));
        }

        /// <summary>
        /// Determines whether the specified <see cref="PixelFormat"/> is video.
        /// </summary>
        /// <param name="fmt">The <see cref="PixelFormat"/>.</param>
        /// <returns><c>true</c> if the specified <see cref="PixelFormat"/> is video; otherwise, <c>false</c>.</returns>
        public static bool IsVideo(this PixelFormat fmt)
        {
#if DIRECTX11_1
            switch ( fmt )
            {
                case Format.AYUV:
                case Format.Y410:
                case Format.Y416:
                case Format.NV12:
                case Format.P010:
                case Format.P016:
                case Format.YUY2:
                case Format.Y210:
                case Format.Y216:
                case Format.NV11:
                    // These video formats can be used with the 3D pipeline through special view mappings
                    return true;

                case Format.Opaque420:
                case Format.AI44:
                case Format.IA44:
                case Format.P8:
                case Format.A8P8:
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
        /// <param name="fmt">The <see cref="PixelFormat"/>.</param>
        /// <returns><c>true</c> if the specified <see cref="PixelFormat"/> is a SRGB format; otherwise, <c>false</c>.</returns>
        public static bool IsSRgb(this PixelFormat fmt)
        {
            return srgbFormats[GetIndex(fmt)];
        }

        /// <summary>
        /// Determines whether the specified <see cref="PixelFormat"/> is HDR (either 16 or 32bits float)
        /// </summary>
        /// <param name="fmt">The FMT.</param>
        /// <returns><c>true</c> if the specified pixel format is HDR (floating point); otherwise, <c>false</c>.</returns>
        public static bool IsHDR(this PixelFormat fmt)
        {
            return hdrFormats[GetIndex(fmt)];
        }

        /// <summary>
        /// Determines whether the specified <see cref="PixelFormat"/> is typeless.
        /// </summary>
        /// <param name="fmt">The <see cref="PixelFormat"/>.</param>
        /// <returns><c>true</c> if the specified <see cref="PixelFormat"/> is typeless; otherwise, <c>false</c>.</returns>
        public static bool IsTypeless(this PixelFormat fmt)
        {
            return typelessFormats[GetIndex(fmt)];
        }

        public static void ComputePitch(this PixelFormat fmt, int width, int height, out int rowPitch, out int slicePitch)
        {
            var sizeInfo = sizeInfos[GetIndex(fmt)];

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
                throw new ArgumentException("The '{0}' format is already an sRGB format".ToFormat(format));

            return sRgbConvertion.ContainsKey(format);
        }

        /// <summary>
        /// Determine if the format has an equivalent non-sRGB format.
        /// </summary>
        /// <param name="format">the sRGB format</param>
        /// <returns>true if the format has an non-sRGB equivalent</returns>
        public static bool HasNonSRgbEquivalent(this PixelFormat format)
        {
            if (!format.IsSRgb())
                throw new ArgumentException("The provided format is not a sRGB format");

            return sRgbConvertion.ContainsKey(format);
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
            if (format.IsSRgb() || !sRgbConvertion.ContainsKey(format))
                return format;

            return sRgbConvertion[format];
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
            if (!format.IsSRgb() || !sRgbConvertion.ContainsKey(format))
                return format;

            return sRgbConvertion[format];
        }

        /// <summary>
        /// Determines whether the specified format is in RGBA order.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <returns>
        ///   <c>true</c> if the specified format is in RGBA order; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsRGBAOrder(this PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.R32G32B32A32_Typeless:
                case PixelFormat.R32G32B32A32_Float:
                case PixelFormat.R32G32B32A32_UInt:
                case PixelFormat.R32G32B32A32_SInt:
                case PixelFormat.R32G32B32_Typeless:
                case PixelFormat.R32G32B32_Float:
                case PixelFormat.R32G32B32_UInt:
                case PixelFormat.R32G32B32_SInt:
                case PixelFormat.R16G16B16A16_Typeless:
                case PixelFormat.R16G16B16A16_Float:
                case PixelFormat.R16G16B16A16_UNorm:
                case PixelFormat.R16G16B16A16_UInt:
                case PixelFormat.R16G16B16A16_SNorm:
                case PixelFormat.R16G16B16A16_SInt:
                case PixelFormat.R32G32_Typeless:
                case PixelFormat.R32G32_Float:
                case PixelFormat.R32G32_UInt:
                case PixelFormat.R32G32_SInt:
                case PixelFormat.R32G8X24_Typeless:
                case PixelFormat.R10G10B10A2_Typeless:
                case PixelFormat.R10G10B10A2_UNorm:
                case PixelFormat.R10G10B10A2_UInt:
                case PixelFormat.R11G11B10_Float:
                case PixelFormat.R8G8B8A8_Typeless:
                case PixelFormat.R8G8B8A8_UNorm:
                case PixelFormat.R8G8B8A8_UNorm_SRgb:
                case PixelFormat.R8G8B8A8_UInt:
                case PixelFormat.R8G8B8A8_SNorm:
                case PixelFormat.R8G8B8A8_SInt:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines whether the specified format is in BGRA order.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <returns>
        ///   <c>true</c> if the specified format is in BGRA order; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsBGRAOrder(this PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.B8G8R8A8_UNorm:
                case PixelFormat.B8G8R8X8_UNorm:
                case PixelFormat.B8G8R8A8_Typeless:
                case PixelFormat.B8G8R8A8_UNorm_SRgb:
                case PixelFormat.B8G8R8X8_Typeless:
                case PixelFormat.B8G8R8X8_UNorm_SRgb:
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
            InitFormat(new[]
            {
                PixelFormat.A8_UNorm,
                PixelFormat.R8_SInt,
                PixelFormat.R8_SNorm,
                PixelFormat.R8_Typeless,
                PixelFormat.R8_UInt,
                PixelFormat.R8_UNorm
            }, 1);

            InitFormat(new[]
            { 
                PixelFormat.B5G5R5A1_UNorm,
                PixelFormat.B5G6R5_UNorm,
                PixelFormat.D16_UNorm,
                PixelFormat.R16_Float,
                PixelFormat.R16_SInt,
                PixelFormat.R16_SNorm,
                PixelFormat.R16_Typeless,
                PixelFormat.R16_UInt,
                PixelFormat.R16_UNorm,
                PixelFormat.R8G8_SInt,
                PixelFormat.R8G8_SNorm,
                PixelFormat.R8G8_Typeless,
                PixelFormat.R8G8_UInt,
                PixelFormat.R8G8_UNorm,
#if DIRECTX11_1
                PixelFormat.B4G4R4A4_UNorm,
#endif
            }, 2);

            InitFormat(new[]
            { 
                PixelFormat.B8G8R8X8_Typeless,
                PixelFormat.B8G8R8X8_UNorm,
                PixelFormat.B8G8R8X8_UNorm_SRgb,
                PixelFormat.D24_UNorm_S8_UInt,
                PixelFormat.D32_Float,
                PixelFormat.D32_Float_S8X24_UInt,
                PixelFormat.R10G10B10_Xr_Bias_A2_UNorm,
                PixelFormat.R10G10B10A2_Typeless,
                PixelFormat.R10G10B10A2_UInt,
                PixelFormat.R10G10B10A2_UNorm,
                PixelFormat.R11G11B10_Float,
                PixelFormat.R16G16_Float,
                PixelFormat.R16G16_SInt,
                PixelFormat.R16G16_SNorm,
                PixelFormat.R16G16_Typeless,
                PixelFormat.R16G16_UInt,
                PixelFormat.R16G16_UNorm,
                PixelFormat.R24_UNorm_X8_Typeless,
                PixelFormat.R24G8_Typeless,
                PixelFormat.R32_Float,
                PixelFormat.R32_Float_X8X24_Typeless,
                PixelFormat.R32_SInt,
                PixelFormat.R32_Typeless,
                PixelFormat.R32_UInt,
                PixelFormat.R8G8B8A8_SInt,
                PixelFormat.R8G8B8A8_SNorm,
                PixelFormat.R8G8B8A8_Typeless,
                PixelFormat.R8G8B8A8_UInt,
                PixelFormat.R8G8B8A8_UNorm,
                PixelFormat.R8G8B8A8_UNorm_SRgb,
                PixelFormat.B8G8R8A8_Typeless,
                PixelFormat.B8G8R8A8_UNorm,
                PixelFormat.B8G8R8A8_UNorm_SRgb,
                PixelFormat.R9G9B9E5_Sharedexp,
                PixelFormat.X24_Typeless_G8_UInt,
                PixelFormat.X32_Typeless_G8X24_UInt,
            }, 4);

            InitFormat(new[]
            { 
                PixelFormat.R16G16B16A16_Float,
                PixelFormat.R16G16B16A16_SInt,
                PixelFormat.R16G16B16A16_SNorm,
                PixelFormat.R16G16B16A16_Typeless,
                PixelFormat.R16G16B16A16_UInt,
                PixelFormat.R16G16B16A16_UNorm,
                PixelFormat.R32G32_Float,
                PixelFormat.R32G32_SInt,
                PixelFormat.R32G32_Typeless,
                PixelFormat.R32G32_UInt,
                PixelFormat.R32G8X24_Typeless,
            }, 8);

            InitFormat(new[]
            { 
                PixelFormat.R32G32B32_Float,
                PixelFormat.R32G32B32_SInt,
                PixelFormat.R32G32B32_Typeless,
                PixelFormat.R32G32B32_UInt,
            }, 12);

            InitFormat(new[]
            { 
                PixelFormat.R32G32B32A32_Float,
                PixelFormat.R32G32B32A32_SInt,
                PixelFormat.R32G32B32A32_Typeless,
                PixelFormat.R32G32B32A32_UInt,
            }, 16);

            // Init compressed formats
            InitBlockFormat(new[]
            { 
                PixelFormat.BC1_Typeless,
                PixelFormat.BC1_UNorm,
                PixelFormat.BC1_UNorm_SRgb,
                PixelFormat.BC4_SNorm,
                PixelFormat.BC4_Typeless,
                PixelFormat.BC4_UNorm,
                PixelFormat.ETC1,
                PixelFormat.ETC2_RGB,
                PixelFormat.ETC2_RGB_SRgb,
                PixelFormat.ETC2_RGB_A1,
                PixelFormat.EAC_R11_Unsigned,
                PixelFormat.EAC_R11_Signed,
            }, 8, 4, 4);

            InitBlockFormat(new[]
            { 
                PixelFormat.BC2_Typeless,
                PixelFormat.BC2_UNorm,
                PixelFormat.BC2_UNorm_SRgb,
                PixelFormat.BC3_Typeless,
                PixelFormat.BC3_UNorm,
                PixelFormat.BC3_UNorm_SRgb,
                PixelFormat.BC5_SNorm,
                PixelFormat.BC5_Typeless,
                PixelFormat.BC5_UNorm,
                PixelFormat.BC6H_Sf16,
                PixelFormat.BC6H_Typeless,
                PixelFormat.BC6H_Uf16,
                PixelFormat.BC7_Typeless,
                PixelFormat.BC7_UNorm,
                PixelFormat.BC7_UNorm_SRgb,
                PixelFormat.ETC2_RGBA,
                PixelFormat.EAC_RG11_Unsigned,
                PixelFormat.EAC_RG11_Signed,
                PixelFormat.ETC2_RGBA_SRgb,
            }, 16, 4, 4);

            InitBlockFormat(new[]
            {
                PixelFormat.R8G8_B8G8_UNorm,
                PixelFormat.G8R8_G8B8_UNorm,
            }, 4, 2, 1);

            InitBlockFormat(new[]
            {
                PixelFormat.R1_UNorm,
            }, 1, 8, 1);

            // Init srgb formats
            InitDefaults(new[]
                {
                    PixelFormat.R8G8B8A8_UNorm_SRgb,
                    PixelFormat.BC1_UNorm_SRgb,
                    PixelFormat.BC2_UNorm_SRgb,
                    PixelFormat.BC3_UNorm_SRgb,
                    PixelFormat.B8G8R8A8_UNorm_SRgb,
                    PixelFormat.B8G8R8X8_UNorm_SRgb,
                    PixelFormat.BC7_UNorm_SRgb,
                    PixelFormat.ETC2_RGBA_SRgb,
                    PixelFormat.ETC2_RGB_SRgb,
                }, srgbFormats);

            // Init srgb formats
            InitDefaults(new[]
                {
                    PixelFormat.R8G8B8A8_UNorm,
                    PixelFormat.R8G8B8A8_UNorm_SRgb,
                    PixelFormat.B8G8R8A8_UNorm,
                    PixelFormat.B8G8R8A8_UNorm_SRgb,
                }, alpha32Formats);

            InitDefaults(new[]
            {
                    PixelFormat.R16G16B16A16_Float,
                    PixelFormat.R32G32B32A32_Float,
                    PixelFormat.R16G16B16A16_Float,
                    PixelFormat.R16G16_Float,
                    PixelFormat.R16_Float,
                    PixelFormat.BC6H_Sf16,
                    PixelFormat.BC6H_Uf16,
            }, hdrFormats);

            // Init typeless formats
            InitDefaults(new[]
                {
                    PixelFormat.R32G32B32A32_Typeless,
                    PixelFormat.R32G32B32_Typeless,
                    PixelFormat.R16G16B16A16_Typeless,
                    PixelFormat.R32G32_Typeless,
                    PixelFormat.R32G8X24_Typeless,
                    PixelFormat.R32_Float_X8X24_Typeless,
                    PixelFormat.X32_Typeless_G8X24_UInt,
                    PixelFormat.R10G10B10A2_Typeless,
                    PixelFormat.R8G8B8A8_Typeless,
                    PixelFormat.R16G16_Typeless,
                    PixelFormat.R32_Typeless,
                    PixelFormat.R24G8_Typeless,
                    PixelFormat.R24_UNorm_X8_Typeless,
                    PixelFormat.X24_Typeless_G8_UInt,
                    PixelFormat.R8G8_Typeless,
                    PixelFormat.R16_Typeless,
                    PixelFormat.R8_Typeless,
                    PixelFormat.BC1_Typeless,
                    PixelFormat.BC2_Typeless,
                    PixelFormat.BC3_Typeless,
                    PixelFormat.BC4_Typeless,
                    PixelFormat.BC5_Typeless,
                    PixelFormat.B8G8R8A8_Typeless,
                    PixelFormat.B8G8R8X8_Typeless,
                    PixelFormat.BC6H_Typeless,
                    PixelFormat.BC7_Typeless,
                }, typelessFormats);

            sRgbConvertion = new Dictionary<PixelFormat, PixelFormat>
            {
                { PixelFormat.R8G8B8A8_UNorm_SRgb, PixelFormat.R8G8B8A8_UNorm },
                { PixelFormat.R8G8B8A8_UNorm, PixelFormat.R8G8B8A8_UNorm_SRgb },
                { PixelFormat.BC1_UNorm_SRgb, PixelFormat.BC1_UNorm },
                { PixelFormat.BC1_UNorm, PixelFormat.BC1_UNorm_SRgb },
                { PixelFormat.BC2_UNorm_SRgb, PixelFormat.BC2_UNorm },
                { PixelFormat.BC2_UNorm, PixelFormat.BC2_UNorm_SRgb },
                { PixelFormat.BC3_UNorm_SRgb, PixelFormat.BC3_UNorm },
                { PixelFormat.BC3_UNorm, PixelFormat.BC3_UNorm_SRgb },
                { PixelFormat.B8G8R8A8_UNorm_SRgb, PixelFormat.B8G8R8A8_UNorm },
                { PixelFormat.B8G8R8A8_UNorm, PixelFormat.B8G8R8A8_UNorm_SRgb },
                { PixelFormat.B8G8R8X8_UNorm_SRgb, PixelFormat.B8G8R8X8_UNorm },
                { PixelFormat.B8G8R8X8_UNorm, PixelFormat.B8G8R8X8_UNorm_SRgb },
                { PixelFormat.BC7_UNorm_SRgb, PixelFormat.BC7_UNorm },
                { PixelFormat.BC7_UNorm, PixelFormat.BC7_UNorm_SRgb },
                { PixelFormat.ETC2_RGBA_SRgb, PixelFormat.ETC2_RGBA },
                { PixelFormat.ETC2_RGBA, PixelFormat.ETC2_RGBA_SRgb },
                { PixelFormat.ETC2_RGB_SRgb, PixelFormat.ETC2_RGB },
                { PixelFormat.ETC2_RGB, PixelFormat.ETC2_RGB_SRgb },
            };
        }

        private static void InitBlockFormat(IEnumerable<PixelFormat> formats, byte blockSize, byte blockWidth, byte blockHeight)
        {
            foreach (var format in formats)
                sizeInfos[GetIndex(format)] = new PixelFormatSizeInfo
                {
                    BlockSize = blockSize,
                    BlockWidth = blockWidth,
                    BlockHeight = blockHeight,
                    IsCompressed = 1,
                };
        }

        private static void InitFormat(IEnumerable<PixelFormat> formats, byte pixelSize)
        {
            foreach (var format in formats)
                sizeInfos[GetIndex(format)] = new PixelFormatSizeInfo
                {
                    BlockSize = pixelSize,
                    BlockWidth = 1,
                    BlockHeight = 1,
                    IsCompressed = 0,
                };
        }

        private static void InitDefaults(IEnumerable<PixelFormat> formats, bool[] outputArray)
        {
            foreach (var format in formats)
                outputArray[GetIndex(format)] = true;
        }
    }
}
