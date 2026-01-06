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

namespace Stride.Graphics;

/// <summary>
///   Provides extensions for <see cref="PixelFormat"/> to help in conversions between formats, querying format
///   information, calculating sizes, pitches, etc.
/// </summary>
public static class PixelFormatExtensions
{
    /// <param name="format">The pixel format.</param>
    extension (PixelFormat format)
    {
        /// <summary>
        ///   Gets the block size, in bytes, for the <see cref="PixelFormat"/>.
        /// </summary>
        /// <remarks>
        ///   In compressed formats, pixels are grouped into blocks (e.g., 4x4 pixel blocks).
        ///   This property returns the size of each block in bytes.
        ///   For non-compressed formats, this value corresponds to the size of a single pixel.
        /// </remarks>
        public int BlockSize => sizeInfos[GetIndex(format)].BlockSize;

        /// <summary>
        ///   Gets the width, in pixels, of a single block for the <see cref="PixelFormat"/>.
        /// </summary>
        /// <remarks>
        ///   In compressed formats, pixels are grouped into blocks (e.g., 4x4 pixel blocks).
        ///   This property returns the width of each block in pixels.
        ///   For non-compressed formats, this value is always 1.
        /// </remarks>
        public int BlockWidth => sizeInfos[GetIndex(format)].BlockWidth;
        /// <summary>
        ///   Gets the height, in pixels, of a single block for the <see cref="PixelFormat"/>.
        /// </summary>
        /// <remarks>
        ///   In compressed formats, pixels are grouped into blocks (e.g., 4x4 pixel blocks).
        ///   This property returns the height of each block in pixels.
        ///   For non-compressed formats, this value is always 1.
        /// </remarks>
        public int BlockHeight => sizeInfos[GetIndex(format)].BlockHeight;

        /// <summary>
        ///   Gets the size of the <see cref="PixelFormat"/> in bytes.
        /// </summary>
        public int SizeInBytes
        {
            get
            {
                var sizeInfo = sizeInfos[GetIndex(format)];
                return sizeInfo.BlockSize / (sizeInfo.BlockWidth * sizeInfo.BlockHeight);
            }
        }

        /// <summary>
        ///   Gets the size of the <see cref="PixelFormat"/> in bits.
        /// </summary>
        public int SizeInBits
        {
            get
            {
                var sizeInfo = sizeInfos[GetIndex(format)];
                return sizeInfo.BlockSize * 8 / (sizeInfo.BlockWidth * sizeInfo.BlockHeight);
            }
        }

        /// <summary>
        ///   Gets the size of the <see cref="PixelFormat"/>'s alpha channel in bits.
        /// </summary>
        /// <value>
        ///   The size of the alpha channel in bits.
        ///   If the format does not have an alpha channel, this value is 0.
        /// </value>
        public int AlphaSizeInBits => format switch
        {
            R32G32B32A32_Typeless or R32G32B32A32_Float or R32G32B32A32_UInt or R32G32B32A32_SInt => 32,

            R16G16B16A16_Typeless or R16G16B16A16_Float or R16G16B16A16_UNorm or R16G16B16A16_UInt or R16G16B16A16_SNorm or R16G16B16A16_SInt => 16,

            R10G10B10A2_Typeless or R10G10B10A2_UNorm or R10G10B10A2_UInt or R10G10B10_Xr_Bias_A2_UNorm => 2,

            R8G8B8A8_Typeless or R8G8B8A8_UNorm or R8G8B8A8_UNorm_SRgb or R8G8B8A8_UInt or R8G8B8A8_SNorm or R8G8B8A8_SInt or
            B8G8R8A8_UNorm or B8G8R8A8_Typeless or B8G8R8A8_UNorm_SRgb or
            A8_UNorm => 8,

            (PixelFormat) 115 => 4,  // DXGI_FORMAT_B4G4R4A4_UNORM

            B5G5R5A1_UNorm => 1,

            BC1_Typeless or BC1_UNorm or BC1_UNorm_SRgb => 1, // or 0
            BC2_Typeless or BC2_UNorm or BC2_UNorm_SRgb => 4,
            BC3_Typeless or BC3_UNorm or BC3_UNorm_SRgb => 8,
            BC7_Typeless or BC7_UNorm or BC7_UNorm_SRgb => 8, // or 0

            ETC2_RGBA or ETC2_RGBA_SRgb => 8,
            ETC2_RGB_A1 => 1,

            _ => 0,
        };

        /// <summary>
        ///   Gets a value indicating if the <see cref="PixelFormat"/> is a compressed format.
        /// </summary>
        public bool IsCompressed => sizeInfos[GetIndex(format)].IsCompressed;

        /// <summary>
        ///   Gets a value indicating if the <see cref="PixelFormat"/> is a valid pixel format.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if the format is valid, i.e., recognized by Stride;
        ///   <see langword="false"/> otherwise.
        /// </value>
        public bool IsValid
            => ((int) format >= 1    && (int) format <= 115)   // DirectX formats
            || ((int) format >= 1088 && (int) format <= 1097); // ETC formats

        /// <summary>
        ///   Gets a value indicating if the <see cref="PixelFormat"/> is an
        ///   uncompressed 32-bit-per-pixel (8-bit per channel) color format
        ///   with an Alpha channel.
        /// </summary>
        public bool Is32bppWithAlpha => alpha32Formats[GetIndex(format)];

        /// <summary>
        ///   Gets a value indicating if the <see cref="PixelFormat"/> has an Alpha channel.
        /// </summary>
        public bool HasAlpha => format.AlphaSizeInBits != 0;

        /// <summary>
        ///   Gets a value indicating if the <see cref="PixelFormat"/> uses packed color channels.
        /// </summary>
        /// <remarks>
        ///   A packed format stores multiple color components within a single data element,
        ///   which can affect how pixel data is accessed and processed.
        ///   Use this property to determine if special handling is required for reading or writing pixel values.
        /// </remarks>
        public bool IsPacked => format is R8G8_B8G8_UNorm or G8R8_G8B8_UNorm;

        /// <summary>
        ///   Gets a value indicating if the <see cref="PixelFormat"/> is a video format.
        /// </summary>
        /// <remarks>
        ///   Video formats are typically used for video decoding and processing tasks.
        ///   This property helps identify formats that may have specific requirements or optimizations,
        ///   and may not be suitable for rendering by the 3D pipeline.
        /// </remarks>
        public bool IsVideoFormat
        {
            get
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
        }

        /// <summary>
        ///   Gets a value indicating if the <see cref="PixelFormat"/> is a sRGB format.
        /// </summary>
        /// <remarks>
        ///   A sRGB format uses the standard RGB color space with a gamma curve,
        ///   i.e., it applies gamma correction to color values.
        ///   These kind of formats are typically used for Textures and Images that are displayed on screen,
        ///   but not for Render Targets or Buffers involved in intermediate rendering calculations.
        /// </remarks>
        public bool IsSRgb => srgbFormats[GetIndex(format)];

        /// <summary>
        ///   Gets a value indicating if the <see cref="PixelFormat"/> is an HDR format
        ///   (either a 16-bit or 32-bit floating point format).
        /// </summary>
        /// <remarks>
        ///   HDR formats can represent a wider range of color and brightness values.
        ///   These formats are typically used in high-dynamic-range rendering scenarios,
        ///   such as when rendering scenes with significant contrast between light and dark areas.
        /// </remarks>
        public bool IsHDR => hdrFormats[GetIndex(format)];

        /// <summary>
        ///   Gets a value indicating if the <see cref="PixelFormat"/> is a typeless format.
        /// </summary>
        /// <remarks>
        ///   Typeless formats do not have a specific data type associated with them.
        ///   They are often used in scenarios where the same data may be interpreted
        ///   as different types depending on the context, such as when creating
        ///   a resource that can be viewed in multiple ways.
        /// </remarks>
        public bool IsTypeless => typelessFormats[GetIndex(format)];

        /// <summary>
        ///   Gets a value indicating if the <see cref="PixelFormat"/> has an equivalent sRGB format.
        /// </summary>
        /// <returns>
        ///   <see langword="true"/> if the pixel format has an sRGB equivalent;
        ///   <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   The provided pixel format is already an sRGB format.
        /// </exception>
        public bool HasSRgbEquivalent
            => format.IsSRgb
                ? throw new ArgumentException($"'{format}' is already an sRGB pixel format", nameof(format))
                : sRgbConversion.ContainsKey(format);

        /// <summary>
        ///   Gets a value indicating if the <see cref="PixelFormat"/> has an equivalent non-sRGB format.
        /// </summary>
        /// <returns>
        ///   <see langword="true"/> if the pixel format has an non-sRGB equivalent;
        ///   <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   The provided pixel format is not an sRGB format.
        /// </exception>
        public bool HasNonSRgbEquivalent
            => !format.IsSRgb
                ? throw new ArgumentException($"'{format}' is not a sRGB format", nameof(format))
                : sRgbConversion.ContainsKey(format);

        /// <summary>
        ///   Gets the equivalent sRGB format to the <see cref="PixelFormat"/>.
        /// </summary>
        /// <returns>
        ///   The equivalent sRGB format if it exists, or the provided pixel format otherwise.
        /// </returns>
        public PixelFormat ToSRgb()
            => format.IsSRgb || !sRgbConversion.TryGetValue(format, out var srgbFormat)
                ? format
                : srgbFormat;

        /// <summary>
        ///   Gets the equivalent non-sRGB format to the provided <see cref="PixelFormat"/>.
        /// </summary>
        /// <returns>
        ///   The equivalent non-sRGB format if it exists, or the provided pixel format otherwise.
        /// </returns>
        public PixelFormat ToNonSRgb()
            => !format.IsSRgb || !sRgbConversion.TryGetValue(format, out var nonSrgbFormat)
                ? format
                : nonSrgbFormat;

        /// <summary>
        ///   Calculates the row and slice pitch values for a Texture based on the specified width and height.
        /// </summary>
        /// <param name="width">The width of the Texture, in pixels. Must be greater than zero.</param>
        /// <param name="height">The height of the Texture, in pixels. Must be greater than zero.</param>
        /// <param name="rowPitch">When this method returns, contains the number of bytes per row of the Texture.</param>
        /// <param name="slicePitch">When this method returns, contains the number of bytes per slice of the Texture.</param>
        /// <remarks>
        ///   The computed pitch values are determined by the Texture format and block size.
        ///   These values are commonly used when allocating memory for Textures
        ///   or performing low-level graphics operations.
        /// </remarks>
        public void ComputePitch(int width, int height, out int rowPitch, out int slicePitch)
        {
            var sizeInfo = sizeInfos[GetIndex(format)];

            rowPitch = ((width + sizeInfo.BlockWidth - 1) / sizeInfo.BlockWidth) * sizeInfo.BlockSize;
            slicePitch = rowPitch * ((height + sizeInfo.BlockHeight - 1) / sizeInfo.BlockHeight);
        }

        /// <summary>
        ///   Gets a value indicating if the <see cref="PixelFormat"/> has its components in the RGBA order.
        /// </summary>
        public bool IsRgbaOrder => format switch
        {
            R32G32B32A32_Typeless or R32G32B32A32_Float or R32G32B32A32_UInt or R32G32B32A32_SInt or
            R32G32B32_Typeless or R32G32B32_Float or R32G32B32_UInt or R32G32B32_SInt or
            R16G16B16A16_Typeless or R16G16B16A16_Float or R16G16B16A16_UNorm or R16G16B16A16_UInt or R16G16B16A16_SNorm or R16G16B16A16_SInt or
            R32G32_Typeless or R32G32_Float or R32G32_UInt or R32G32_SInt or
            R32G8X24_Typeless or
            R10G10B10A2_Typeless or R10G10B10A2_UNorm or R10G10B10A2_UInt or
            R11G11B10_Float or
            R8G8B8A8_Typeless or R8G8B8A8_UNorm or R8G8B8A8_UNorm_SRgb or R8G8B8A8_UInt or R8G8B8A8_SNorm or R8G8B8A8_SInt => true,

            _ => false
        };

        /// <summary>
        ///   Gets a value indicating if the <see cref="PixelFormat"/> has its components in the BGRA order.
        /// </summary>
        public bool IsBgraOrder => format switch
        {
            B8G8R8A8_UNorm or B8G8R8X8_UNorm or B8G8R8A8_Typeless or B8G8R8A8_UNorm_SRgb or B8G8R8X8_Typeless or B8G8R8X8_UNorm_SRgb => true,
            _ => false,
        };
    }

    #region Lookup tables and pre-computed information

    private static readonly PixelFormatSizeInfo[] sizeInfos = new PixelFormatSizeInfo[256];
    private static readonly bool[] srgbFormats = new bool[256];
    private static readonly bool[] hdrFormats = new bool[256];
    private static readonly bool[] alpha32Formats = new bool[256];
    private static readonly bool[] typelessFormats = new bool[256];
    private static readonly Dictionary<PixelFormat, PixelFormat> sRgbConversion;


    /// <summary>
    ///   Calculates an index corresponding to the specified pixel format so it can be
    ///   located in the information arrays.
    /// </summary>
    /// <param name="format">The pixel format.</param>
    /// <returns>An integer representing the array index for the given pixel format.</returns>
    private static int GetIndex(PixelFormat format)
    {
        int value = (int) format;

        return value < 1024
            // DirectX official pixel formats (0..115 use 0..127 in the arrays)
            ? value
            // Custom pixel formats (1024..1151? use 128..255 in the array)
            : value - 1024 + 128;
    }


    //
    // Static initializer to speed up size calculation (not sure the JIT is enough "smart" for this kind of thing).
    //
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
                sizeInfos[GetIndex(format)] = new PixelFormatSizeInfo(BlockSize: pixelSize, BlockWidth: 1, BlockHeight: 1, IsCompressed: false);
        }

        static void InitBlockFormat(ReadOnlySpan<PixelFormat> formats, byte blockSize, byte blockWidth, byte blockHeight)
        {
            foreach (var format in formats)
                sizeInfos[GetIndex(format)] = new PixelFormatSizeInfo(BlockSize: blockSize, BlockWidth: blockWidth, BlockHeight: blockHeight, IsCompressed: true);
        }

        static void InitDefaults(ReadOnlySpan<PixelFormat> formats, bool[] outputArray)
        {
            foreach (var format in formats)
                outputArray[GetIndex(format)] = true;
        }
    }

    #region Helper type: PixelFormatSizeInfo

    private readonly record struct PixelFormatSizeInfo(bool IsCompressed, byte BlockWidth, byte BlockHeight, byte BlockSize);

    #endregion

    #endregion
}
