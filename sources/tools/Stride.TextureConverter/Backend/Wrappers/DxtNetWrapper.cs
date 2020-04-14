// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;

namespace Xenko.TextureConverter.DxtWrapper
{
    #region enum
    /// <summary>
    /// Copy of the windows enum of DXGI_FORMAT in the file dxgiformat.h of the includes of Windows kit 
    /// </summary>
    internal enum DXGI_FORMAT
    {
        DXGI_FORMAT_UNKNOWN = 0,
        DXGI_FORMAT_R32G32B32A32_TYPELESS = 1,
        DXGI_FORMAT_R32G32B32A32_FLOAT = 2,
        DXGI_FORMAT_R32G32B32A32_UINT = 3,
        DXGI_FORMAT_R32G32B32A32_SINT = 4,
        DXGI_FORMAT_R32G32B32_TYPELESS = 5,
        DXGI_FORMAT_R32G32B32_FLOAT = 6,
        DXGI_FORMAT_R32G32B32_UINT = 7,
        DXGI_FORMAT_R32G32B32_SINT = 8,
        DXGI_FORMAT_R16G16B16A16_TYPELESS = 9,
        DXGI_FORMAT_R16G16B16A16_FLOAT = 10,
        DXGI_FORMAT_R16G16B16A16_UNORM = 11,
        DXGI_FORMAT_R16G16B16A16_UINT = 12,
        DXGI_FORMAT_R16G16B16A16_SNORM = 13,
        DXGI_FORMAT_R16G16B16A16_SINT = 14,
        DXGI_FORMAT_R32G32_TYPELESS = 15,
        DXGI_FORMAT_R32G32_FLOAT = 16,
        DXGI_FORMAT_R32G32_UINT = 17,
        DXGI_FORMAT_R32G32_SINT = 18,
        DXGI_FORMAT_R32G8X24_TYPELESS = 19,
        DXGI_FORMAT_D32_FLOAT_S8X24_UINT = 20,
        DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS = 21,
        DXGI_FORMAT_X32_TYPELESS_G8X24_UINT = 22,
        DXGI_FORMAT_R10G10B10A2_TYPELESS = 23,
        DXGI_FORMAT_R10G10B10A2_UNORM = 24,
        DXGI_FORMAT_R10G10B10A2_UINT = 25,
        DXGI_FORMAT_R11G11B10_FLOAT = 26,
        DXGI_FORMAT_R8G8B8A8_TYPELESS = 27,
        DXGI_FORMAT_R8G8B8A8_UNORM = 28,
        DXGI_FORMAT_R8G8B8A8_UNORM_SRGB = 29,
        DXGI_FORMAT_R8G8B8A8_UINT = 30,
        DXGI_FORMAT_R8G8B8A8_SNORM = 31,
        DXGI_FORMAT_R8G8B8A8_SINT = 32,
        DXGI_FORMAT_R16G16_TYPELESS = 33,
        DXGI_FORMAT_R16G16_FLOAT = 34,
        DXGI_FORMAT_R16G16_UNORM = 35,
        DXGI_FORMAT_R16G16_UINT = 36,
        DXGI_FORMAT_R16G16_SNORM = 37,
        DXGI_FORMAT_R16G16_SINT = 38,
        DXGI_FORMAT_R32_TYPELESS = 39,
        DXGI_FORMAT_D32_FLOAT = 40,
        DXGI_FORMAT_R32_FLOAT = 41,
        DXGI_FORMAT_R32_UINT = 42,
        DXGI_FORMAT_R32_SINT = 43,
        DXGI_FORMAT_R24G8_TYPELESS = 44,
        DXGI_FORMAT_D24_UNORM_S8_UINT = 45,
        DXGI_FORMAT_R24_UNORM_X8_TYPELESS = 46,
        DXGI_FORMAT_X24_TYPELESS_G8_UINT = 47,
        DXGI_FORMAT_R8G8_TYPELESS = 48,
        DXGI_FORMAT_R8G8_UNORM = 49,
        DXGI_FORMAT_R8G8_UINT = 50,
        DXGI_FORMAT_R8G8_SNORM = 51,
        DXGI_FORMAT_R8G8_SINT = 52,
        DXGI_FORMAT_R16_TYPELESS = 53,
        DXGI_FORMAT_R16_FLOAT = 54,
        DXGI_FORMAT_D16_UNORM = 55,
        DXGI_FORMAT_R16_UNORM = 56,
        DXGI_FORMAT_R16_UINT = 57,
        DXGI_FORMAT_R16_SNORM = 58,
        DXGI_FORMAT_R16_SINT = 59,
        DXGI_FORMAT_R8_TYPELESS = 60,
        DXGI_FORMAT_R8_UNORM = 61,
        DXGI_FORMAT_R8_UINT = 62,
        DXGI_FORMAT_R8_SNORM = 63,
        DXGI_FORMAT_R8_SINT = 64,
        DXGI_FORMAT_A8_UNORM = 65,
        DXGI_FORMAT_R1_UNORM = 66,
        DXGI_FORMAT_R9G9B9E5_SHAREDEXP = 67,
        DXGI_FORMAT_R8G8_B8G8_UNORM = 68,
        DXGI_FORMAT_G8R8_G8B8_UNORM = 69,
        DXGI_FORMAT_BC1_TYPELESS = 70,
        DXGI_FORMAT_BC1_UNORM = 71,
        DXGI_FORMAT_BC1_UNORM_SRGB = 72,
        DXGI_FORMAT_BC2_TYPELESS = 73,
        DXGI_FORMAT_BC2_UNORM = 74,
        DXGI_FORMAT_BC2_UNORM_SRGB = 75,
        DXGI_FORMAT_BC3_TYPELESS = 76,
        DXGI_FORMAT_BC3_UNORM = 77,
        DXGI_FORMAT_BC3_UNORM_SRGB = 78,
        DXGI_FORMAT_BC4_TYPELESS = 79,
        DXGI_FORMAT_BC4_UNORM = 80,
        DXGI_FORMAT_BC4_SNORM = 81,
        DXGI_FORMAT_BC5_TYPELESS = 82,
        DXGI_FORMAT_BC5_UNORM = 83,
        DXGI_FORMAT_BC5_SNORM = 84,
        DXGI_FORMAT_B5G6R5_UNORM = 85,
        DXGI_FORMAT_B5G5R5A1_UNORM = 86,
        DXGI_FORMAT_B8G8R8A8_UNORM = 87,
        DXGI_FORMAT_B8G8R8X8_UNORM = 88,
        DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM = 89,
        DXGI_FORMAT_B8G8R8A8_TYPELESS = 90,
        DXGI_FORMAT_B8G8R8A8_UNORM_SRGB = 91,
        DXGI_FORMAT_B8G8R8X8_TYPELESS = 92,
        DXGI_FORMAT_B8G8R8X8_UNORM_SRGB = 93,
        DXGI_FORMAT_BC6H_TYPELESS = 94,
        DXGI_FORMAT_BC6H_UF16 = 95,
        DXGI_FORMAT_BC6H_SF16 = 96,
        DXGI_FORMAT_BC7_TYPELESS = 97,
        DXGI_FORMAT_BC7_UNORM = 98,
        DXGI_FORMAT_BC7_UNORM_SRGB = 99,
        DXGI_FORMAT_AYUV = 100,
        DXGI_FORMAT_Y410 = 101,
        DXGI_FORMAT_Y416 = 102,
        DXGI_FORMAT_NV12 = 103,
        DXGI_FORMAT_P010 = 104,
        DXGI_FORMAT_P016 = 105,
        DXGI_FORMAT_420_OPAQUE = 106,
        DXGI_FORMAT_YUY2 = 107,
        DXGI_FORMAT_Y210 = 108,
        DXGI_FORMAT_Y216 = 109,
        DXGI_FORMAT_NV11 = 110,
        DXGI_FORMAT_AI44 = 111,
        DXGI_FORMAT_IA44 = 112,
        DXGI_FORMAT_P8 = 113,
        DXGI_FORMAT_A8P8 = 114,
        DXGI_FORMAT_B4G4R4A4_UNORM = 115,
    };

    [Flags]
    internal enum DDS_FLAGS
    {
        DDS_FLAGS_NONE = 0x0,

        /// <summary>
        /// Assume pitch is DWORD aligned instead of BYTE aligned (used by some legacy DDS files)
        /// </summary>
        DDS_FLAGS_LEGACY_DWORD = 0x1,

        /// <summary>
        /// Do not implicitly convert legacy formats that result in larger pixel sizes (24 bpp, 3:3:2, A8L8, A4L4, P8, A8P8) 
        /// </summary>
        DDS_FLAGS_NO_LEGACY_EXPANSION = 0x2,

        /// <summary>
        /// Do not use work-around for long-standing D3DX DDS file format issue which reversed the 10:10:10:2 color order masks
        /// </summary>
        DDS_FLAGS_NO_R10B10G10A2_FIXUP = 0x4,

        /// <summary>
        /// Convert DXGI 1.1 BGR formats to DXGI_FORMAT_R8G8B8A8_UNorm to avoid use of optional WDDM 1.1 formats
        /// </summary>
        DDS_FLAGS_FORCE_RGB = 0x8,

        /// <summary>
        /// Conversions avoid use of 565, 5551, and 4444 formats and instead expand to 8888 to avoid use of optional WDDM 1.2 formats
        /// </summary>
        DDS_FLAGS_NO_16BPP = 0x10,

        /// <summary>
        /// Always use the 'DX10' header extension for DDS writer (i.e. don't try to write DX9 compatible DDS files)
        /// </summary>
        DDS_FLAGS_FORCE_DX10_EXT = 0x10000,
    };


    [Flags]
    internal enum WIC_FLAGS
    {
        WIC_FLAGS_NONE = 0x0,

        WIC_FLAGS_FORCE_RGB = 0x1,
        // Loads DXGI 1.1 BGR formats as DXGI_FORMAT_R8G8B8A8_UNORM to avoid use of optional WDDM 1.1 formats

        WIC_FLAGS_NO_X2_BIAS = 0x2,
        // Loads DXGI 1.1 X2 10:10:10:2 format as DXGI_FORMAT_R10G10B10A2_UNORM

        WIC_FLAGS_NO_16BPP = 0x4,
        // Loads 565, 5551, and 4444 formats as 8888 to avoid use of optional WDDM 1.2 formats

        WIC_FLAGS_ALLOW_MONO = 0x8,
        // Loads 1-bit monochrome (black & white) as R1_UNORM rather than 8-bit grayscale

        WIC_FLAGS_ALL_FRAMES = 0x10,
        // Loads all images in a multi-frame file, converting/resizing to match the first frame as needed, defaults to 0th frame otherwise

        WIC_FLAGS_IGNORE_SRGB = 0x20,
        // Ignores sRGB metadata if present in the file

        WIC_FLAGS_DITHER = 0x10000,
        // Use ordered 4x4 dithering for any required conversions

        WIC_FLAGS_DITHER_DIFFUSION = 0x20000,
        // Use error-diffusion dithering for any required conversions

        WIC_FLAGS_FILTER_POINT = 0x100000,
        WIC_FLAGS_FILTER_LINEAR = 0x200000,
        WIC_FLAGS_FILTER_CUBIC = 0x300000,
        WIC_FLAGS_FILTER_FANT = 0x400000, // Combination of Linear and Box filter
                                          // Filtering mode to use for any required image resizing (only needed when loading arrays of differently sized images; defaults to Fant)
    };


    [Flags]
    internal enum TEX_COMPRESS_FLAGS
    {
        TEX_COMPRESS_DEFAULT = 0,

        /// <summary>
        /// Enables dithering RGB colors for BC1-3 compression
        /// </summary>
        TEX_COMPRESS_RGB_DITHER = 0x10000,

        /// <summary>
        /// Enables dithering alpha for BC1-3 compression
        /// </summary>
        TEX_COMPRESS_A_DITHER = 0x20000,

        /// <summary>
        /// Enables both RGB and alpha dithering for BC1-3 compression
        /// </summary>
        TEX_COMPRESS_DITHER = 0x30000,

        /// <summary>
        /// Uniform color weighting for BC1-3 compression; by default uses perceptual weighting
        /// </summary>
        TEX_COMPRESS_UNIFORM = 0x40000,

        /// <summary>
        /// Compress is free to use multithreading to improve performance (by default it does not use multithreading)
        /// </summary>
        TEX_COMPRESS_PARALLEL = 0x10000000,
    };

    [Flags]
    internal enum TEX_PREMULTIPLY_ALPHA_FLAGS
    {
        TEX_PMALPHA_DEFAULT = 0,

        /// <summary>
        /// Ignores sRGB colorspace conversions
        /// </summary>
        TEX_PMALPHA_IGNORE_SRGB = 0x1,

        TEX_PMALPHA_SRGB_IN = 0x1000000,
        TEX_PMALPHA_SRGB_OUT = 0x2000000,
        TEX_PMALPHA_SRGB = (TEX_PMALPHA_SRGB_IN | TEX_PMALPHA_SRGB_OUT),
        // if the input format type is IsSRGB(), then SRGB_IN is on by default
        // if the output format type is IsSRGB(), then SRGB_OUT is on by default
    };

    internal enum TEX_DIMENSION
    {
        TEX_DIMENSION_TEXTURE1D = 2,
        TEX_DIMENSION_TEXTURE2D = 3,
        TEX_DIMENSION_TEXTURE3D = 4,
    };

    [Flags]
    internal enum TEX_MISC_FLAG
    {
        TEX_MISC_TEXTURECUBE = 0x4,
    };

    [Flags]
    internal enum TEX_FILTER_FLAGS
    {
        TEX_FILTER_DEFAULT = 0,

        TEX_FILTER_WRAP_U = 0x1,
        TEX_FILTER_WRAP_V = 0x2,
        TEX_FILTER_WRAP_W = 0x4,
        TEX_FILTER_WRAP = (TEX_FILTER_WRAP_U | TEX_FILTER_WRAP_V | TEX_FILTER_WRAP_W),
        TEX_FILTER_MIRROR_U = 0x10,
        TEX_FILTER_MIRROR_V = 0x20,
        TEX_FILTER_MIRROR_W = 0x40,
        TEX_FILTER_MIRROR = (TEX_FILTER_MIRROR_U | TEX_FILTER_MIRROR_V | TEX_FILTER_MIRROR_W),
        // Wrap vs. Mirror vs. Clamp filtering options

        TEX_FILTER_SEPARATE_ALPHA = 0x100,
        // Resize color and alpha channel independently

        TEX_FILTER_RGB_COPY_RED = 0x1000,
        TEX_FILTER_RGB_COPY_GREEN = 0x2000,
        TEX_FILTER_RGB_COPY_BLUE = 0x4000,
        // When converting RGB to R, defaults to using grayscale. These flags indicate copying a specific channel instead
        // When converting RGB to RG, defaults to copying RED | GREEN. These flags control which channels are selected instead.

        TEX_FILTER_DITHER = 0x10000,
        // Use ordered 4x4 dithering for any required conversions
        TEX_FILTER_DITHER_DIFFUSION = 0x20000,
        // Use error-diffusion dithering for any required conversions

        TEX_FILTER_POINT = 0x100000,
        TEX_FILTER_LINEAR = 0x200000,
        TEX_FILTER_CUBIC = 0x300000,
        TEX_FILTER_BOX = 0x400000,
        TEX_FILTER_FANT = 0x400000, // Equiv to Box filtering for mipmap generation
        TEX_FILTER_TRIANGLE = 0x500000,
        // Filtering mode to use for any required image resizing

        TEX_FILTER_SRGB_IN = 0x1000000,
        TEX_FILTER_SRGB_OUT = 0x2000000,
        TEX_FILTER_SRGB = (TEX_FILTER_SRGB_IN | TEX_FILTER_SRGB_OUT),
        // sRGB <-> RGB for use in conversion operations
        // if the input format type is IsSRGB(), then SRGB_IN is on by default
        // if the output format type is IsSRGB(), then SRGB_OUT is on by default

        TEX_FILTER_FORCE_NON_WIC = 0x10000000,
        // Forces use of the non-WIC path when both are an option

        TEX_FILTER_FORCE_WIC = 0x20000000,
        // Forces use of the WIC path even when logic would have picked a non-WIC path when both are an option
    };

    internal enum HRESULT
    {
        S_OK,
        E_ABORT,
        E_ACCESSDENIED,
        E_FAIL,
        E_HANDLE,
        E_INVALIDARG,
        E_NOINTERFACE,
        E_NOTIMPL,
        E_OUTOFMEMORY,
        E_POINTER,
        E_UNEXPECTED,
    }

    [Flags]
    internal enum CP_FLAGS
    {
        /// <summary>
        /// Normal operation
        /// </summary>
        CP_FLAGS_NONE = 0x0,

        /// <summary>
        /// Assume pitch is DWORD aligned instead of BYTE aligned
        /// </summary>
        CP_FLAGS_LEGACY_DWORD = 0x1,

        /// <summary>
        /// Override with a legacy 24 bits-per-pixel format size
        /// </summary>
        CP_FLAGS_24BPP = 0x10000,

        /// <summary>
        /// Override with a legacy 16 bits-per-pixel format size
        /// </summary>
        CP_FLAGS_16BPP = 0x20000,

        /// <summary>
        /// Override with a legacy 8 bits-per-pixel format size
        /// </summary>
        CP_FLAGS_8BPP = 0x40000,  
    };

    [Flags]
    internal enum CNMAP_FLAGS
    {
        CNMAP_DEFAULT           = 0,

        CNMAP_CHANNEL_RED       = 0x1,
        CNMAP_CHANNEL_GREEN     = 0x2,
        CNMAP_CHANNEL_BLUE      = 0x3,
        CNMAP_CHANNEL_ALPHA     = 0x4,
        CNMAP_CHANNEL_LUMINANCE = 0x5,
            // Channel selection when evaluting color value for height
            // Luminance is a combination of red, green, and blue

        CNMAP_MIRROR_U          = 0x1000,
        CNMAP_MIRROR_V          = 0x2000,
        CNMAP_MIRROR            = 0x3000,
            // Use mirror semantics for scanline references (defaults to wrap)

        CNMAP_INVERT_SIGN       = 0x4000,
            // Inverts normal sign

        CNMAP_COMPUTE_OCCLUSION = 0x8000,
            // Computes a crude occlusion term stored in the alpha channel
    };
    #endregion


    /// <summary>
    /// C# Equivalent of the DirectXTex structure Metadata
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct TexMetadata
    {
        private IntPtr width;
        private IntPtr height;     // Should be 1 for 1D textures
        private IntPtr depth;      // Should be 1 for 1D or 2D textures
        private IntPtr arraySize;  // For cubemap, this is a multiple of 6
        private IntPtr mipLevels;
        public TEX_MISC_FLAG miscFlags;
        public int miscFlags2;
        public DXGI_FORMAT format;
        public TEX_DIMENSION dimension;

        public TexMetadata(int width, int height, int depth, int arraySize, int mipLevels, TEX_MISC_FLAG miscFlags, int miscFlags2, DXGI_FORMAT format, TEX_DIMENSION dimension)
        {
            this.width = (IntPtr)width;
            this.height = (IntPtr)height;
            this.depth = (IntPtr)depth;
            this.arraySize = (IntPtr)arraySize;
            this.mipLevels = (IntPtr)mipLevels;
            this.miscFlags = miscFlags;
            this.miscFlags2 = miscFlags2;
            this.format = format;
            this.dimension = dimension;
        }

        public int Width
        {
            get { return (int)width; }
            set { width = (IntPtr)value; }
        }

        public int Height
        {
            get { return (int)height; }
            set { height = (IntPtr)value; }
        }

        public int Depth
        {
            get { return (int)depth; }
            set { depth = (IntPtr)value; }
        }

        public int ArraySize
        {
            get { return (int)arraySize; }
            set { arraySize = (IntPtr)value; }
        }

        public int MipLevels
        {
            get { return (int)mipLevels; }
            set { mipLevels = (IntPtr)value; }
        }

        public override String ToString()
        {
            return "width:" + Width + "\nheight:" + Height + "\ndepth:" + Depth + "\narraySize:" + ArraySize + "\nmipLevels:" + MipLevels + "\nmiscFlags:" + miscFlags + "\nformat:" + format + "\ndimension:" + dimension;
        }
    }

    /// <summary>
    /// C# Equivalent of the DirectXTex structure Image
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct DxtImage
    {
        private IntPtr width;
        private IntPtr height;
        public DXGI_FORMAT format;
        private IntPtr rowPitch;
        private IntPtr slicePitch;
        public IntPtr pixels;

        public DxtImage(int width, int height, DXGI_FORMAT format, int rowPitch, int slicePitch, IntPtr pixels)
        {
            this.width = (IntPtr)width;
            this.height = (IntPtr)height;
            this.format = format;
            this.rowPitch = (IntPtr)rowPitch;
            this.slicePitch = (IntPtr)slicePitch;
            this.pixels = pixels;
        }

        public int Width
        {
            get { return (int)width; }
            set { width = (IntPtr)value; }
        }

        public int Height
        {
            get { return (int)height; }
            set { height = (IntPtr)value; }
        }

        public int RowPitch
        {
            get { return (int)rowPitch; }
            set { rowPitch = (IntPtr)value; }
        }

        public int SlicePitch
        {
            get { return (int)slicePitch; }
            set { slicePitch = (IntPtr)value; }
        }

        public override String ToString()
        {
            return "width:" + Width + "\nheight:" + Height + "\nformat:" + format + "\nrowPitch:" + RowPitch + "\nslicePitch:" + SlicePitch + "\npixels:" + pixels;
        }
    }

    /// <summary>
    /// Utility class binding DirectXTex utility methods
    /// </summary>
    internal class Utilities
    {
        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        private extern static void dxtComputePitch(DXGI_FORMAT fmt, int width, int height, out int rowPitch, out int slicePitch, CP_FLAGS flags);

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        private extern static uint dxtLoadDDSFile(String filePath, DDS_FLAGS flags, out TexMetadata metadata, IntPtr image);

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        private extern static uint dxtLoadTGAFile(String filePath, out TexMetadata metadata, IntPtr image);

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        private extern static uint dxtLoadWICFile(String filePath, WIC_FLAGS flags, out TexMetadata metadata, IntPtr image);

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        private extern static bool dxtIsCompressed(DXGI_FORMAT fmt);

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        private extern static uint dxtConvert(ref DxtImage srcImage, DXGI_FORMAT format, TEX_FILTER_FLAGS filter, float threshold, IntPtr cImage);

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        private extern static uint dxtConvertArray(DxtImage[] srcImages, int nimages, ref TexMetadata metadata, DXGI_FORMAT format, TEX_FILTER_FLAGS filter, float threshold, IntPtr cImages);

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        private extern static uint dxtCompress(ref DxtImage srcImage, DXGI_FORMAT format, TEX_COMPRESS_FLAGS compress, float alphaRef, IntPtr cImage);

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        private extern static uint dxtCompressArray(DxtImage[] srcImages, int nimages, ref TexMetadata metadata, DXGI_FORMAT format, TEX_COMPRESS_FLAGS compress, float alphaRef, IntPtr cImages);

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        private extern static uint dxtDecompress(ref DxtImage cImage, DXGI_FORMAT format, IntPtr image);

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        private extern static uint dxtDecompressArray(DxtImage[] cImages, int nimages, ref TexMetadata metadata, DXGI_FORMAT format, IntPtr images);

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        private extern static uint dxtSaveToDDSFile(ref DxtImage dxtImage, DDS_FLAGS flags, string szFile);

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        private extern static uint dxtSaveToDDSFileArray(DxtImage[] dxtImages, int nimages, ref TexMetadata metadata, DDS_FLAGS flags, string szFile);

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        private extern static uint dxtGenerateMipMaps(ref DxtImage baseImage, TEX_FILTER_FLAGS filter, int levels, IntPtr mipChain, bool allow1D);

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        private extern static uint dxtGenerateMipMapsArray(DxtImage[] srcImages, int nimages, ref TexMetadata metadata, TEX_FILTER_FLAGS filter, int levels, IntPtr mipChain);

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        private extern static uint dxtGenerateMipMaps3D(ref DxtImage baseImage, int depth, TEX_FILTER_FLAGS filter, int levels, IntPtr mipChain);

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        private extern static uint dxtGenerateMipMaps3DArray(DxtImage[] srcImages, int nimages, ref TexMetadata metadata, TEX_FILTER_FLAGS filter, int levels, IntPtr mipChain );

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        private extern static uint dxtResize(DxtImage[] srcImages, int nimages, ref TexMetadata metadata, int width, int height, TEX_FILTER_FLAGS filter, IntPtr result);

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        private extern static uint dxtComputeNormalMap(DxtImage[] srcImages, int nimages, ref TexMetadata metadata, CNMAP_FLAGS flags, float amplitude, DXGI_FORMAT format, IntPtr normalMaps );

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        private extern static uint dxtPremultiplyAlpha(DxtImage[] srcImages, int nimages, ref TexMetadata metadata, TEX_PREMULTIPLY_ALPHA_FLAGS flags, IntPtr result);

        public static void ComputePitch(DXGI_FORMAT fmt, int width, int height, out int rowPitch, out int slicePitch, CP_FLAGS flags)
        {
            dxtComputePitch(fmt, width, height, out rowPitch, out slicePitch, flags);
        }

        public static HRESULT LoadDDSFile(String filePath, DDS_FLAGS flags, out TexMetadata metadata, ScratchImage image)
        {
            return HandleHRESULT(dxtLoadDDSFile(filePath, flags, out metadata, image.ptr));
        }

        public static HRESULT LoadTGAFile(String filePath, out TexMetadata metadata, ScratchImage image)
        {
            return HandleHRESULT(dxtLoadTGAFile(filePath, out metadata, image.ptr));
        }

        public static HRESULT LoadWICFile(String filePath, WIC_FLAGS flags, out TexMetadata metadata, ScratchImage image)
        {
            return HandleHRESULT(dxtLoadWICFile(filePath, flags, out metadata, image.ptr));
        }

        public static HRESULT SaveToDDSFile(ref DxtImage dxtImage, DDS_FLAGS flags, string szFile)
        {
            return HandleHRESULT(dxtSaveToDDSFile(ref dxtImage, flags, szFile));
        }

        public static HRESULT SaveToDDSFile(DxtImage[] dxtImages, int nimages, ref TexMetadata metadata, DDS_FLAGS flags, string szFile)
        {
            return HandleHRESULT(dxtSaveToDDSFileArray(dxtImages, nimages, ref metadata, flags, szFile));
        }

        public static bool IsCompressed(DXGI_FORMAT fmt)
        {
            return dxtIsCompressed(fmt);
        }

        public static HRESULT Convert(ref DxtImage srcImage, DXGI_FORMAT format, TEX_FILTER_FLAGS filter, float threshold, ScratchImage cImage)
        {
            return HandleHRESULT(dxtConvert(ref srcImage, format, filter, threshold, cImage.ptr));
        }

        public static HRESULT Convert(DxtImage[] srcImages, int nimages, ref TexMetadata metadata, DXGI_FORMAT format, TEX_FILTER_FLAGS filter, float threshold, ScratchImage cImages)
        {
            return HandleHRESULT(dxtConvertArray(srcImages, nimages, ref metadata, format, filter, threshold, cImages.ptr));
        }

        public static HRESULT Compress(ref DxtImage srcImage, DXGI_FORMAT format, TEX_COMPRESS_FLAGS compress, float alphaRef, ScratchImage cImage)
        {
            return HandleHRESULT(dxtCompress(ref srcImage, format, compress, alphaRef, cImage.ptr));
        }

        public static HRESULT Compress(DxtImage[] srcImages, int nimages, ref TexMetadata metadata, DXGI_FORMAT format, TEX_COMPRESS_FLAGS compress, float alphaRef, ScratchImage cImages)
        {
            return HandleHRESULT(dxtCompressArray(srcImages, nimages, ref metadata, format, compress, alphaRef, cImages.ptr));
        }

        public static HRESULT Decompress(ref DxtImage cImage, DXGI_FORMAT format, ScratchImage image)
        {
            return HandleHRESULT(dxtDecompress(ref cImage, format, image.ptr));
        }

        public static HRESULT Decompress(DxtImage[] cImages, int nimages, ref TexMetadata metadata, DXGI_FORMAT format, ScratchImage images)
        {
            return HandleHRESULT(dxtDecompressArray(cImages, nimages, ref metadata, format, images.ptr));
        }

        public static HRESULT GenerateMipMaps(ref DxtImage baseImage, TEX_FILTER_FLAGS filter, int levels, ScratchImage mipChain, bool allow1D = false)
        {
            return HandleHRESULT(dxtGenerateMipMaps(ref baseImage, filter, levels, mipChain.ptr, allow1D));
        }

        public static HRESULT GenerateMipMaps(DxtImage[] srcImages, int nimages, ref TexMetadata metadata, TEX_FILTER_FLAGS filter, int levels, ScratchImage mipChain)
        {
            return HandleHRESULT(dxtGenerateMipMapsArray(srcImages, nimages, ref metadata, filter, levels, mipChain.ptr));
        }

        public static HRESULT GenerateMipMaps3D(ref DxtImage baseImage, int depth, TEX_FILTER_FLAGS filter, int levels, ScratchImage mipChain)
        {
            return HandleHRESULT(dxtGenerateMipMaps3D(ref baseImage, depth, filter, levels, mipChain.ptr));
        }

        public static HRESULT GenerateMipMaps3D(DxtImage[] srcImages, int nimages, ref TexMetadata metadata, TEX_FILTER_FLAGS filter, int levels, ScratchImage mipChain)
        {
            return HandleHRESULT(dxtGenerateMipMaps3DArray(srcImages, nimages, ref metadata, filter, levels, mipChain.ptr));
        }

        public static HRESULT Resize(DxtImage[] srcImages, int nimages, ref TexMetadata metadata, int width, int height, TEX_FILTER_FLAGS filter, ScratchImage result)
        {
            return HandleHRESULT(dxtResize(srcImages, nimages, ref metadata, width, height, filter, result.ptr));
        }

        public static HRESULT ComputeNormalMap(DxtImage[] srcImages, int nimages, ref TexMetadata metadata, CNMAP_FLAGS flags, float amplitude, DXGI_FORMAT format, ScratchImage normalMaps)
        {
            return HandleHRESULT(dxtComputeNormalMap(srcImages, nimages, ref metadata, flags, amplitude, format, normalMaps.ptr));
        }

        public static HRESULT PremultiplyAlpha(DxtImage[] srcImages, int nimages, ref TexMetadata metadata, TEX_PREMULTIPLY_ALPHA_FLAGS flags, ScratchImage result)
        {
            return HandleHRESULT(dxtPremultiplyAlpha(srcImages, nimages, ref metadata, flags, result.ptr));
        }


        public static HRESULT HandleHRESULT(uint hresult)
        {
            switch (hresult)
            {
                case 0x00000000: return HRESULT.S_OK;
                case 0x80004004: return HRESULT.E_ABORT;
                case 0x80070005: return HRESULT.E_ACCESSDENIED;
                case 0x80004005: return HRESULT.E_FAIL;
                case 0x80070006: return HRESULT.E_HANDLE;
                case 0x80070057: return HRESULT.E_INVALIDARG;
                case 0x80004002: return HRESULT.E_NOINTERFACE;
                case 0x80004001: return HRESULT.E_NOTIMPL;
                case 0x8007000E: return HRESULT.E_OUTOFMEMORY;
                case 0x80004003: return HRESULT.E_POINTER;
                case 0x8000FFFF: return HRESULT.E_UNEXPECTED;
                default: return HRESULT.E_FAIL;
            }
        }

    }


    /// <summary>
    /// Binding of the DirectXTex class ScratchImage
    /// </summary>
    internal class ScratchImage : IDisposable
    {
        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static IntPtr dxtCreateScratchImage();

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static void dxtDeleteScratchImage(IntPtr img);

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static uint dxtInitialize(IntPtr img, out TexMetadata mdata);

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static uint dxtInitialize1D(IntPtr img, DXGI_FORMAT fmt,  int length,  int arraySize,  int mipLevels );

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static uint dxtInitialize2D(IntPtr img, DXGI_FORMAT fmt, int width, int height, int arraySize, int mipLevels);

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static uint dxtInitialize3D(IntPtr img, DXGI_FORMAT fmt,  int width,  int height,  int depth,  int mipLevels );

	    [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static uint dxtInitializeCube(IntPtr img, DXGI_FORMAT fmt,  int width,  int height,  int nCubes,  int mipLevels );

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static uint dxtInitializeFromImage(IntPtr img, out DxtImage srcImage, bool allow1D);

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static uint dxtInitializeArrayFromImages(IntPtr img, DxtImage[] dxtImages, int nImages, bool allow1D );

	    [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static uint dxtInitializeCubeFromImages(IntPtr img, DxtImage[] dxtImages, int nImages);

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static uint dxtInitialize3DFromImages(IntPtr img, DxtImage[] dxtImages, int depth);

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static void dxtRelease(IntPtr img);

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static bool dxtOverrideFormat(IntPtr img, DXGI_FORMAT f);

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static IntPtr dxtGetMetadata(IntPtr img);

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static IntPtr dxtGetImage(IntPtr img, int mip, int item, int slice);

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static IntPtr dxtGetImages(IntPtr img);

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static int dxtGetImageCount(IntPtr img);

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static IntPtr dxtGetPixels(IntPtr img);

        [DllImport("DxtWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static int dxtGetPixelsSize(IntPtr img);


        public IntPtr ptr { private set; get; }

        public ScratchImage()
        {
            ptr = dxtCreateScratchImage();
        }

        public void Dispose()
        {
            dxtDeleteScratchImage(ptr);
        }

        public HRESULT Initialize(out TexMetadata mdata)
        {
            return Utilities.HandleHRESULT(dxtInitialize(ptr, out mdata));
        }

        public HRESULT Initialize1D(DXGI_FORMAT fmt, int length, int arraySize, int mipLevels)
        {
            return Utilities.HandleHRESULT(dxtInitialize1D(ptr, fmt, length, arraySize, mipLevels));
        }

        public HRESULT Initialize2D(DXGI_FORMAT fmt, int width, int height, int arraySize, int mipLevels)
        {
            return Utilities.HandleHRESULT(dxtInitialize2D(ptr, fmt, width, height, arraySize, mipLevels));
        }

        public HRESULT Initialize3D(DXGI_FORMAT fmt, int width, int height, int depth, int mipLevels)
        {
            return Utilities.HandleHRESULT(dxtInitialize3D(ptr, fmt, width, height, depth, mipLevels));
        }

        public HRESULT InitializeCube(DXGI_FORMAT fmt, int width, int height, int nCubes, int mipLevels)
        {
            return Utilities.HandleHRESULT(dxtInitializeCube(ptr, fmt, width, height, nCubes, mipLevels));
        }

        public HRESULT InitializeFromImage(out DxtImage srcImage, bool allow1D = false)
        {
            return Utilities.HandleHRESULT(dxtInitializeFromImage(ptr, out srcImage, allow1D));
        }

        public HRESULT InitializeFromImages(DxtImage[] dxtImages, int nImages, bool allow1D = false)
        {
            return Utilities.HandleHRESULT(dxtInitializeArrayFromImages(ptr, dxtImages, nImages, allow1D));
        }

        public HRESULT InitializeCubeFromImages(DxtImage[] dxtImages, int nImages)
        {
            return Utilities.HandleHRESULT(dxtInitializeCubeFromImages(ptr, dxtImages, nImages));
        }

        public HRESULT Initialize3DFromImages(DxtImage[] dxtImages, int depth)
        {
            return Utilities.HandleHRESULT(dxtInitialize3DFromImages(ptr, dxtImages, depth));
        }

        public void Release()
        {
            dxtRelease(ptr);
        }

        public bool OverrideFormat(TexMetadata mdata, DXGI_FORMAT f)
        {
            return dxtOverrideFormat(ptr, f);
        }
  
        public TexMetadata metadata
        {
            get {return (TexMetadata)Marshal.PtrToStructure(dxtGetMetadata(ptr), typeof(TexMetadata));}
        }

        public IntPtr data
        {
            get { return dxtGetPixels(ptr); } 
        }

        public int pixelSize
        {
            get { return dxtGetPixelsSize(ptr); }
        }

        public int imageCount
        {
            get { return dxtGetImageCount(ptr); }
        }

        public DxtImage GetImage(int mip, int item, int slice)
        {
            return (DxtImage)Marshal.PtrToStructure(dxtGetImage(ptr, mip, item, slice), typeof(DxtImage));
        }

        public DxtImage[] GetImages()
        {
            IntPtr imagesPtr = dxtGetImages(ptr);
            int imagenb =  imageCount;

            DxtImage[] dxtImages = new DxtImage[imagenb];


            for(int i=0;i<imagenb;++i)
            {
                dxtImages[i] = (DxtImage)Marshal.PtrToStructure(imagesPtr + i * Marshal.SizeOf(dxtImages[0]), typeof(DxtImage));
            }

            return dxtImages;
        }

    }

    internal unsafe class DDSHeader
    {
        enum DDSPfFlags {
            DDPF_ALPHAPIXELS    =   0x0001,
            DDPF_ALPHA          =   0x0002,
            DDPF_FOURCC         =   0x0004,
            DDPF_RGB            =   0x0040,
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct DDSHeaderDX9
        {
            public uint dwMagic;
            public uint dwSize;
            public uint dwFlags;
            public uint dwHeight;
            public uint dwWidth;
            public uint dwPitchOrLinearSize;
            public uint dwDepth;
            public uint dwMipMapCount;
            public fixed uint dwReserved[11];
            public uint dwPfSize;
            public uint dwPfFlags;
            public uint dwFourCC;
            public uint dwRGBBitCount;
            public uint dwRBitMask;
            public uint dwGBitMask;
            public uint dwBBitMask;
            public uint dwRGBAlphaBitMask;
            public uint dwCaps;
            public uint dwCaps2;
            public fixed uint dwReservedCaps[2];
            public uint dwReserved2;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct DDSHeaderDX10
        {
            public uint dxgiFormat;
            public uint resourceDimension;
            public uint miscFlag;
            public uint arraySize;
            public uint reserved;
        }

        private static int GetBitCount(uint bitmask)
        {
            int count = 0;
            for (uint i = 0 ; i < 32 ; ++i, bitmask>>=1)
            {
                if ((bitmask&1) != 0)
                    count++;
            }
            return count;
        }

        internal static int GetAlphaDepth(String filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                int headerSize = sizeof(DDSHeaderDX9);  // 128byte
                byte[] buffer = new byte[headerSize];
                DDSHeaderDX9 header;
                fileStream.Read(buffer, 0, headerSize);
                fixed (byte* ptr = buffer)
                {
                    DDSHeaderDX9* headerPtr = &header;
                    Xenko.Core.Utilities.CopyMemory((IntPtr)headerPtr, (IntPtr)ptr, headerSize);
                }
                if (header.dwMagic != 0x20534444 || header.dwPfSize != 32)
                    return -1;
                if ((header.dwPfFlags & (uint)DDSPfFlags.DDPF_FOURCC) == 0 && (header.dwPfFlags & (uint)(DDSPfFlags.DDPF_RGB|DDSPfFlags.DDPF_ALPHA)) != 0)
                {
                    if ((header.dwPfFlags & (uint)DDSPfFlags.DDPF_ALPHAPIXELS) != 0)
                        return GetBitCount(header.dwRGBAlphaBitMask);
                    return 0;
                }
            }
            return -1;
        }

    }

}
