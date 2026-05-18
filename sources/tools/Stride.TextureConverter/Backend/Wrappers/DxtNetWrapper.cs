// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Stride.Core;

namespace Stride.TextureConverter.DxtWrapper
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
    internal enum DDS_FLAGS : uint
    {
        DDS_FLAGS_NONE = 0x0,
        DDS_FLAGS_LEGACY_DWORD = 0x1,
        DDS_FLAGS_NO_LEGACY_EXPANSION = 0x2,
        DDS_FLAGS_NO_R10B10G10A2_FIXUP = 0x4,
        DDS_FLAGS_FORCE_RGB = 0x8,
        DDS_FLAGS_NO_16BPP = 0x10,
        DDS_FLAGS_EXPAND_LUMINANCE = 0x20,
        DDS_FLAGS_BAD_DXTN_TAILS = 0x40,
        DDS_FLAGS_PERMISSIVE = 0x80,
        DDS_FLAGS_IGNORE_MIPS = 0x100,
        DDS_FLAGS_FORCE_DX10_EXT = 0x10000,
        DDS_FLAGS_FORCE_DX10_EXT_MISC2 = 0x20000,
        DDS_FLAGS_FORCE_DX9_LEGACY = 0x40000,
        DDS_FLAGS_FORCE_DXT5_RXGB = 0x80000,
        DDS_FLAGS_FORCE_24BPP_RGB = 0x100000,
        DDS_FLAGS_ALLOW_LARGE_FILES = 0x1000000,
    };

    [Flags]
    internal enum TGA_FLAGS : uint
    {
        TGA_FLAGS_NONE = 0x0,
        TGA_FLAGS_BGR = 0x1,
        TGA_FLAGS_ALLOW_ALL_ZERO_ALPHA = 0x2,
        TGA_FLAGS_IGNORE_SRGB = 0x10,
        TGA_FLAGS_FORCE_SRGB = 0x20,
        TGA_FLAGS_FORCE_LINEAR = 0x40,
        TGA_FLAGS_DEFAULT_SRGB = 0x80,
    };

    [Flags]
    internal enum WIC_FLAGS : uint
    {
        WIC_FLAGS_NONE = 0x0,
        WIC_FLAGS_FORCE_RGB = 0x1,
        WIC_FLAGS_NO_X2_BIAS = 0x2,
        WIC_FLAGS_NO_16BPP = 0x4,
        WIC_FLAGS_ALLOW_MONO = 0x8,
        WIC_FLAGS_ALL_FRAMES = 0x10,
        WIC_FLAGS_IGNORE_SRGB = 0x20,
        WIC_FLAGS_FORCE_SRGB = 0x40,
        WIC_FLAGS_FORCE_LINEAR = 0x80,
        WIC_FLAGS_DEFAULT_SRGB = 0x100,
        WIC_FLAGS_DITHER = 0x10000,
        WIC_FLAGS_DITHER_DIFFUSION = 0x20000,
        WIC_FLAGS_FILTER_POINT = 0x100000,
        WIC_FLAGS_FILTER_LINEAR = 0x200000,
        WIC_FLAGS_FILTER_CUBIC = 0x300000,
        WIC_FLAGS_FILTER_FANT = 0x400000,
    };

    [Flags]
    internal enum TEX_COMPRESS_FLAGS : uint
    {
        TEX_COMPRESS_DEFAULT = 0,
        TEX_COMPRESS_RGB_DITHER = 0x10000,
        TEX_COMPRESS_A_DITHER = 0x20000,
        TEX_COMPRESS_DITHER = 0x30000,
        TEX_COMPRESS_UNIFORM = 0x40000,
        TEX_COMPRESS_BC7_USE_3SUBSETS = 0x80000,
        TEX_COMPRESS_BC7_QUICK = 0x100000,
        TEX_COMPRESS_SRGB_IN = 0x1000000,
        TEX_COMPRESS_SRGB_OUT = 0x2000000,
        TEX_COMPRESS_SRGB = (TEX_COMPRESS_SRGB_IN | TEX_COMPRESS_SRGB_OUT),
        TEX_COMPRESS_PARALLEL = 0x10000000,
    };

    [Flags]
    internal enum TEX_PREMULTIPLY_ALPHA_FLAGS : uint
    {
        TEX_PMALPHA_DEFAULT = 0,
        TEX_PMALPHA_IGNORE_SRGB = 0x1,
        TEX_PMALPHA_REVERSE = 0x2,
        TEX_PMALPHA_SRGB_IN = 0x1000000,
        TEX_PMALPHA_SRGB_OUT = 0x2000000,
        TEX_PMALPHA_SRGB = (TEX_PMALPHA_SRGB_IN | TEX_PMALPHA_SRGB_OUT),
    };

    internal enum TEX_DIMENSION
    {
        TEX_DIMENSION_TEXTURE1D = 2,
        TEX_DIMENSION_TEXTURE2D = 3,
        TEX_DIMENSION_TEXTURE3D = 4,
    };

    [Flags]
    internal enum TEX_MISC_FLAG : uint
    {
        TEX_MISC_TEXTURECUBE = 0x4,
    };

    [Flags]
    internal enum TEX_FILTER_FLAGS : uint
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
        TEX_FILTER_SEPARATE_ALPHA = 0x100,
        TEX_FILTER_FLOAT_X2BIAS = 0x200,
        TEX_FILTER_RGB_COPY_RED = 0x1000,
        TEX_FILTER_RGB_COPY_GREEN = 0x2000,
        TEX_FILTER_RGB_COPY_BLUE = 0x4000,
        TEX_FILTER_RGB_COPY_ALPHA = 0x8000,
        TEX_FILTER_DITHER = 0x10000,
        TEX_FILTER_DITHER_DIFFUSION = 0x20000,
        TEX_FILTER_POINT = 0x100000,
        TEX_FILTER_LINEAR = 0x200000,
        TEX_FILTER_CUBIC = 0x300000,
        TEX_FILTER_BOX = 0x400000,
        TEX_FILTER_FANT = 0x400000,
        TEX_FILTER_TRIANGLE = 0x500000,
        TEX_FILTER_SRGB_IN = 0x1000000,
        TEX_FILTER_SRGB_OUT = 0x2000000,
        TEX_FILTER_SRGB = (TEX_FILTER_SRGB_IN | TEX_FILTER_SRGB_OUT),
        TEX_FILTER_FORCE_NON_WIC = 0x10000000,
        TEX_FILTER_FORCE_WIC = 0x20000000,
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
    internal enum CP_FLAGS : uint
    {
        CP_FLAGS_NONE = 0x0,
        CP_FLAGS_LEGACY_DWORD = 0x1,
        CP_FLAGS_PARAGRAPH = 0x2,
        CP_FLAGS_YMM = 0x4,
        CP_FLAGS_ZMM = 0x8,
        CP_FLAGS_PAGE4K = 0x200,
        CP_FLAGS_BAD_DXTN_TAILS = 0x1000,
        CP_FLAGS_24BPP = 0x10000,
        CP_FLAGS_16BPP = 0x20000,
        CP_FLAGS_8BPP = 0x40000,
        CP_FLAGS_LIMIT_4GB = 0x10000000,
    };

    [Flags]
    internal enum CNMAP_FLAGS : uint
    {
        CNMAP_DEFAULT = 0,
        CNMAP_CHANNEL_RED = 0x1,
        CNMAP_CHANNEL_GREEN = 0x2,
        CNMAP_CHANNEL_BLUE = 0x3,
        CNMAP_CHANNEL_ALPHA = 0x4,
        CNMAP_CHANNEL_LUMINANCE = 0x5,
        CNMAP_MIRROR_U = 0x1000,
        CNMAP_MIRROR_V = 0x2000,
        CNMAP_MIRROR = 0x3000,
        CNMAP_INVERT_SIGN = 0x4000,
        CNMAP_COMPUTE_OCCLUSION = 0x8000,
    };
    #endregion

    /// <summary>
    /// POD mirror of DirectX::TexMetadata. Layout must match the C++ struct exactly.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct TexMetadata
    {
        private IntPtr width;       // size_t
        private IntPtr height;      // size_t (1 for 1D)
        private IntPtr depth;       // size_t (1 for 1D/2D)
        private IntPtr arraySize;   // size_t (multiple of 6 for cubemaps)
        private IntPtr mipLevels;   // size_t
        public TEX_MISC_FLAG miscFlags;
        public uint miscFlags2;
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
            this.miscFlags2 = (uint)miscFlags2;
            this.format = format;
            this.dimension = dimension;
        }

        public int Width     { get => (int)width;     set => width     = value; }
        public int Height    { get => (int)height;    set => height    = value; }
        public int Depth     { get => (int)depth;     set => depth     = value; }
        public int ArraySize { get => (int)arraySize; set => arraySize = value; }
        public int MipLevels { get => (int)mipLevels; set => mipLevels = value; }

        public override string ToString()
            => $"width:{Width}\nheight:{Height}\ndepth:{Depth}\narraySize:{ArraySize}\nmipLevels:{MipLevels}\nmiscFlags:{miscFlags}\nformat:{format}\ndimension:{dimension}";
    }

    /// <summary>
    /// POD mirror of DirectX::Image. Layout must match the C++ struct exactly.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct DxtImage
    {
        private IntPtr width;       // size_t
        private IntPtr height;      // size_t
        public DXGI_FORMAT format;
        private IntPtr rowPitch;    // size_t
        private IntPtr slicePitch;  // size_t
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

        public int Width      { get => (int)width;      set => width      = value; }
        public int Height     { get => (int)height;     set => height     = value; }
        public int RowPitch   { get => (int)rowPitch;   set => rowPitch   = value; }
        public int SlicePitch { get => (int)slicePitch; set => slicePitch = value; }

        public override string ToString()
            => $"width:{Width}\nheight:{Height}\nformat:{format}\nrowPitch:{RowPitch}\nslicePitch:{SlicePitch}\npixels:{pixels}";
    }

    /// <summary>
    /// Opaque handle to a DirectX::ScratchImage owned by the native lib.
    /// Lifetime managed via Dispose; calls dxtRelease underneath.
    /// </summary>
    internal sealed class DxtImageSet : IDisposable
    {
        public IntPtr Handle { get; private set; }

        public DxtImageSet(IntPtr handle) { Handle = handle; }

        public void Dispose()
        {
            if (Handle != IntPtr.Zero)
            {
                NativeMethods.dxtRelease(Handle);
                Handle = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }

        ~DxtImageSet() { if (Handle != IntPtr.Zero) NativeMethods.dxtRelease(Handle); }

        public TexMetadata GetMetadata() => Marshal.PtrToStructure<TexMetadata>(NativeMethods.dxtGetMetadata(Handle));

        public int ImageCount => NativeMethods.dxtGetImageCount(Handle);

        public DxtImage[] GetImages()
        {
            int n = ImageCount;
            var arr = new DxtImage[n];
            IntPtr ptr = NativeMethods.dxtGetImages(Handle);
            int stride = Marshal.SizeOf<DxtImage>();
            for (int i = 0; i < n; i++)
                arr[i] = Marshal.PtrToStructure<DxtImage>(ptr + i * stride);
            return arr;
        }

        public IntPtr Pixels     => NativeMethods.dxtGetPixels(Handle);
        public long   PixelsSize => (long)NativeMethods.dxtGetPixelsSize(Handle);

        public bool OverrideFormat(DXGI_FORMAT format) => NativeMethods.dxtOverrideFormat(Handle, format);
    }

    /// <summary>
    /// P/Invoke surface to the stride_directxtex native lib.
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    internal static class NativeMethods
    {
        private const string Lib = "stride_directxtex";
        private const CallingConvention Cdecl = CallingConvention.Cdecl;

        // Lifecycle / queries -----------------------------------------------
        [DllImport(Lib, CallingConvention = Cdecl)]
        public extern static void dxtRelease(IntPtr set);

        [DllImport(Lib, CallingConvention = Cdecl)]
        public extern static IntPtr dxtGetMetadata(IntPtr set);

        [DllImport(Lib, CallingConvention = Cdecl)]
        public extern static IntPtr dxtGetImages(IntPtr set);

        [DllImport(Lib, CallingConvention = Cdecl)]
        public extern static int dxtGetImageCount(IntPtr set);

        [DllImport(Lib, CallingConvention = Cdecl)]
        public extern static IntPtr dxtGetPixels(IntPtr set);

        [DllImport(Lib, CallingConvention = Cdecl)]
        public extern static IntPtr dxtGetPixelsSize(IntPtr set);

        [DllImport(Lib, CallingConvention = Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public extern static bool dxtOverrideFormat(IntPtr set, DXGI_FORMAT format);

        // I/O ----------------------------------------------------------------
        // UTF-8 paths via [MarshalAs(LPUTF8Str)] (.NET Core+).
        [DllImport(Lib, CallingConvention = Cdecl)]
        public extern static uint dxtLoadDDS([MarshalAs(UnmanagedType.LPUTF8Str)] string utf8Path, DDS_FLAGS flags, out IntPtr outSet);

        [DllImport(Lib, CallingConvention = Cdecl)]
        public extern static uint dxtLoadTGA([MarshalAs(UnmanagedType.LPUTF8Str)] string utf8Path, TGA_FLAGS flags, out IntPtr outSet);

        [DllImport(Lib, CallingConvention = Cdecl)]
        public extern static uint dxtLoadHDR([MarshalAs(UnmanagedType.LPUTF8Str)] string utf8Path, out IntPtr outSet);

        [DllImport(Lib, CallingConvention = Cdecl)]
        public extern static uint dxtLoadWIC([MarshalAs(UnmanagedType.LPUTF8Str)] string utf8Path, WIC_FLAGS flags, out IntPtr outSet);

        [DllImport(Lib, CallingConvention = Cdecl)]
        public extern static uint dxtSaveDDS([In] DxtImage[] images, int count, ref TexMetadata metadata, DDS_FLAGS flags, [MarshalAs(UnmanagedType.LPUTF8Str)] string utf8Path);

        [DllImport(Lib, CallingConvention = Cdecl)]
        public extern static uint dxtSaveHDR(ref DxtImage image, [MarshalAs(UnmanagedType.LPUTF8Str)] string utf8Path);

        // Operations ---------------------------------------------------------
        [DllImport(Lib, CallingConvention = Cdecl)]
        public extern static uint dxtCompress([In] DxtImage[] images, int count, ref TexMetadata metadata,
            DXGI_FORMAT format, TEX_COMPRESS_FLAGS flags, float alphaRef, out IntPtr outSet);

        [DllImport(Lib, CallingConvention = Cdecl)]
        public extern static uint dxtDecompress([In] DxtImage[] images, int count, ref TexMetadata metadata,
            DXGI_FORMAT format, out IntPtr outSet);

        [DllImport(Lib, CallingConvention = Cdecl)]
        public extern static uint dxtConvert([In] DxtImage[] images, int count, ref TexMetadata metadata,
            DXGI_FORMAT format, TEX_FILTER_FLAGS filter, float threshold, out IntPtr outSet);

        [DllImport(Lib, CallingConvention = Cdecl)]
        public extern static uint dxtResize([In] DxtImage[] images, int count, ref TexMetadata metadata,
            int width, int height, TEX_FILTER_FLAGS filter, out IntPtr outSet);

        [DllImport(Lib, CallingConvention = Cdecl)]
        public extern static uint dxtGenerateMips([In] DxtImage[] images, int count, ref TexMetadata metadata,
            TEX_FILTER_FLAGS filter, int levels, out IntPtr outSet);

        [DllImport(Lib, CallingConvention = Cdecl)]
        public extern static uint dxtGenerateMips3D([In] DxtImage[] images, int count, ref TexMetadata metadata,
            TEX_FILTER_FLAGS filter, int levels, out IntPtr outSet);

        [DllImport(Lib, CallingConvention = Cdecl)]
        public extern static uint dxtNormalMap([In] DxtImage[] images, int count, ref TexMetadata metadata,
            CNMAP_FLAGS flags, float amplitude, DXGI_FORMAT format, out IntPtr outSet);

        [DllImport(Lib, CallingConvention = Cdecl)]
        public extern static uint dxtPremultiplyAlpha([In] DxtImage[] images, int count, ref TexMetadata metadata,
            TEX_PREMULTIPLY_ALPHA_FLAGS flags, out IntPtr outSet);

        [DllImport(Lib, CallingConvention = Cdecl)]
        public extern static uint dxtScaleMipsAlphaForCoverage(IntPtr set, int item, float alphaRef);

        // Utilities ----------------------------------------------------------
        [DllImport(Lib, CallingConvention = Cdecl)]
        public extern static uint dxtComputePitch(DXGI_FORMAT fmt, int width, int height, CP_FLAGS flags, out IntPtr rowPitch, out IntPtr slicePitch);

        [DllImport(Lib, CallingConvention = Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public extern static bool dxtIsCompressed(DXGI_FORMAT fmt);

        [DllImport(Lib, CallingConvention = Cdecl)]
        public extern static IntPtr dxtBytesPerBlock(DXGI_FORMAT fmt);

        [DllImport(Lib, CallingConvention = Cdecl)]
        public extern static int dxtCalculateMipLevels(int width, int height);

        [DllImport(Lib, CallingConvention = Cdecl)]
        public extern static int dxtCalculateMipLevels3D(int width, int height, int depth);
    }

    /// <summary>
    /// High-level wrappers returning DxtImageSet handles and translating HRESULT codes.
    /// </summary>
    internal static class Utilities
    {
        // I/O
        public static HRESULT LoadDDS(string filePath, DDS_FLAGS flags, out DxtImageSet set)
            => HandleResult(NativeMethods.dxtLoadDDS(filePath, flags, out var p), p, out set);

        public static HRESULT LoadTGA(string filePath, TGA_FLAGS flags, out DxtImageSet set)
            => HandleResult(NativeMethods.dxtLoadTGA(filePath, flags, out var p), p, out set);

        public static HRESULT LoadHDR(string filePath, out DxtImageSet set)
            => HandleResult(NativeMethods.dxtLoadHDR(filePath, out var p), p, out set);

        public static HRESULT LoadWIC(string filePath, WIC_FLAGS flags, out DxtImageSet set)
            => HandleResult(NativeMethods.dxtLoadWIC(filePath, flags, out var p), p, out set);

        public static HRESULT SaveDDS(DxtImage[] images, int nimages, ref TexMetadata metadata, DDS_FLAGS flags, string filePath)
            => HandleResult(NativeMethods.dxtSaveDDS(images, nimages, ref metadata, flags, filePath));

        public static HRESULT SaveHDR(ref DxtImage image, string filePath)
            => HandleResult(NativeMethods.dxtSaveHDR(ref image, filePath));

        // Operations
        public static HRESULT Compress(DxtImage[] images, int nimages, ref TexMetadata metadata,
            DXGI_FORMAT format, TEX_COMPRESS_FLAGS flags, float alphaRef, out DxtImageSet set)
            => HandleResult(NativeMethods.dxtCompress(images, nimages, ref metadata, format, flags, alphaRef, out var p), p, out set);

        public static HRESULT Decompress(DxtImage[] images, int nimages, ref TexMetadata metadata,
            DXGI_FORMAT format, out DxtImageSet set)
            => HandleResult(NativeMethods.dxtDecompress(images, nimages, ref metadata, format, out var p), p, out set);

        public static HRESULT Convert(DxtImage[] images, int nimages, ref TexMetadata metadata,
            DXGI_FORMAT format, TEX_FILTER_FLAGS filter, float threshold, out DxtImageSet set)
            => HandleResult(NativeMethods.dxtConvert(images, nimages, ref metadata, format, filter, threshold, out var p), p, out set);

        public static HRESULT Resize(DxtImage[] images, int nimages, ref TexMetadata metadata,
            int width, int height, TEX_FILTER_FLAGS filter, out DxtImageSet set)
            => HandleResult(NativeMethods.dxtResize(images, nimages, ref metadata, width, height, filter, out var p), p, out set);

        public static HRESULT GenerateMipMaps(DxtImage[] images, int nimages, ref TexMetadata metadata,
            TEX_FILTER_FLAGS filter, int levels, out DxtImageSet set)
            => HandleResult(NativeMethods.dxtGenerateMips(images, nimages, ref metadata, filter, levels, out var p), p, out set);

        public static HRESULT GenerateMipMaps3D(DxtImage[] images, int nimages, ref TexMetadata metadata,
            TEX_FILTER_FLAGS filter, int levels, out DxtImageSet set)
            => HandleResult(NativeMethods.dxtGenerateMips3D(images, nimages, ref metadata, filter, levels, out var p), p, out set);

        public static HRESULT ComputeNormalMap(DxtImage[] images, int nimages, ref TexMetadata metadata,
            CNMAP_FLAGS flags, float amplitude, DXGI_FORMAT format, out DxtImageSet set)
            => HandleResult(NativeMethods.dxtNormalMap(images, nimages, ref metadata, flags, amplitude, format, out var p), p, out set);

        public static HRESULT PremultiplyAlpha(DxtImage[] images, int nimages, ref TexMetadata metadata,
            TEX_PREMULTIPLY_ALPHA_FLAGS flags, out DxtImageSet set)
            => HandleResult(NativeMethods.dxtPremultiplyAlpha(images, nimages, ref metadata, flags, out var p), p, out set);

        public static HRESULT ScaleMipsAlphaForCoverage(DxtImageSet set, int item, float alphaRef)
            => HandleResult(NativeMethods.dxtScaleMipsAlphaForCoverage(set.Handle, item, alphaRef));

        // Utilities
        public static void ComputePitch(DXGI_FORMAT fmt, int width, int height, out int rowPitch, out int slicePitch, CP_FLAGS flags)
        {
            NativeMethods.dxtComputePitch(fmt, width, height, flags, out var r, out var s);
            rowPitch = (int)r;
            slicePitch = (int)s;
        }

        public static bool IsCompressed(DXGI_FORMAT fmt) => NativeMethods.dxtIsCompressed(fmt);
        public static int  BytesPerBlock(DXGI_FORMAT fmt) => (int)NativeMethods.dxtBytesPerBlock(fmt);
        public static int  CalculateMipLevels(int width, int height) => NativeMethods.dxtCalculateMipLevels(width, height);
        public static int  CalculateMipLevels3D(int width, int height, int depth) => NativeMethods.dxtCalculateMipLevels3D(width, height, depth);

        // HRESULT translation: the wrapper passes raw HRESULT through. Convert a few well-known
        // codes to the enum; everything else falls back to E_FAIL.
        public static HRESULT HandleResult(uint hr) => hr switch
        {
            0x00000000 => HRESULT.S_OK,
            0x80004004 => HRESULT.E_ABORT,
            0x80070005 => HRESULT.E_ACCESSDENIED,
            0x80004005 => HRESULT.E_FAIL,
            0x80070006 => HRESULT.E_HANDLE,
            0x80070057 => HRESULT.E_INVALIDARG,
            0x80004002 => HRESULT.E_NOINTERFACE,
            0x80004001 => HRESULT.E_NOTIMPL,
            0x8007000E => HRESULT.E_OUTOFMEMORY,
            0x80004003 => HRESULT.E_POINTER,
            0x8000FFFF => HRESULT.E_UNEXPECTED,
            _ => HRESULT.E_FAIL,
        };

        public static HRESULT HandleResult(uint hr, IntPtr setHandle, out DxtImageSet set)
        {
            var r = HandleResult(hr);
            set = r == HRESULT.S_OK ? new DxtImageSet(setHandle) : null;
            return r;
        }
    }

    internal unsafe class DDSHeader
    {
        enum DDSPfFlags
        {
            DDPF_ALPHAPIXELS = 0x0001,
            DDPF_ALPHA       = 0x0002,
            DDPF_FOURCC      = 0x0004,
            DDPF_RGB         = 0x0040,
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

        private static int GetBitCount(uint bitmask)
        {
            int count = 0;
            for (uint i = 0; i < 32; ++i, bitmask >>= 1)
            {
                if ((bitmask & 1) != 0)
                    count++;
            }
            return count;
        }

        internal static int GetAlphaDepth(string filePath)
        {
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            int headerSize = sizeof(DDSHeaderDX9);
            byte[] buffer = new byte[headerSize];
            DDSHeaderDX9 header;
            int readCount = fileStream.Read(buffer, 0, headerSize);
            if (readCount != headerSize) return -1;
            fixed (byte* ptr = buffer)
            {
                DDSHeaderDX9* headerPtr = &header;
                MemoryUtilities.CopyWithAlignmentFallback(headerPtr, ptr, (uint)headerSize);
            }
            if (header.dwMagic != 0x20534444 || header.dwPfSize != 32) return -1;
            if ((header.dwPfFlags & (uint)DDSPfFlags.DDPF_FOURCC) == 0 && (header.dwPfFlags & (uint)(DDSPfFlags.DDPF_RGB | DDSPfFlags.DDPF_ALPHA)) != 0)
            {
                if ((header.dwPfFlags & (uint)DDSPfFlags.DDPF_ALPHAPIXELS) != 0)
                    return GetBitCount(header.dwRGBAlphaBitMask);
                return 0;
            }
            return -1;
        }
    }
}
