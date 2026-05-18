// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace Stride.TextureConverter.AstcEncWrapper
{
    /// <summary>
    /// P/Invoke surface for the ARM astc-encoder shared library (astcenc).
    /// Only the subset used by <see cref="TexLibraries.AstcTexLib"/> is exposed.
    /// </summary>
    internal static class AstcEncNative
    {
        // On Linux/macOS .NET prepends "lib" and uses the .so/.dylib extension automatically;
        // on Windows it loads astcenc.dll. The deps/astcenc/dotnet/<rid>/ binaries are named accordingly.
        public const string LibraryName = "astcenc";

        public enum AstcEncError
        {
            Success = 0,
            OutOfMem,
            BadCpuFloat,
            BadParam,
            BadBlockSize,
            BadProfile,
            BadQuality,
            BadSwizzle,
            BadFlags,
            BadContext,
            NotImplemented,
            BadDecodeMode,
        }

        public enum AstcEncProfile
        {
            LdrSrgb = 0,
            Ldr = 1,
            HdrRgbLdrA = 2,
            Hdr = 3,
        }

        public enum AstcEncSwizzle : byte
        {
            R = 0, G = 1, B = 2, A = 3, Zero = 4, One = 5, Z = 6,
        }

        public enum AstcEncDataType
        {
            U8 = 0,
            F16 = 1,
            F32 = 2,
        }

        // Quality presets (free-form float 0..100).
        public const float QualityFastest = 0.0f;
        public const float QualityFast = 10.0f;
        public const float QualityMedium = 60.0f;
        public const float QualityThorough = 98.0f;
        public const float QualityVeryThorough = 99.0f;
        public const float QualityExhaustive = 100.0f;

        // Flags
        public const uint FlagDecompressOnly = 1u << 1;

        [StructLayout(LayoutKind.Sequential)]
        public struct Swizzle
        {
            public AstcEncSwizzle R;
            public AstcEncSwizzle G;
            public AstcEncSwizzle B;
            public AstcEncSwizzle A;

            public static readonly Swizzle Rgba = new() { R = AstcEncSwizzle.R, G = AstcEncSwizzle.G, B = AstcEncSwizzle.B, A = AstcEncSwizzle.A };
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct AstcImage
        {
            public uint DimX;
            public uint DimY;
            public uint DimZ;
            public AstcEncDataType DataType;
            // Pointer to an array of (DimZ) slice pointers; each slice is DimY rows × DimX texels × 4 bytes (U8 RGBA).
            public void** Data;
        }

        // Public astcenc_config has many tuning knobs; we only need to construct it via astcenc_config_init
        // and let the encoder fill in defaults. Allocate it as an opaque byte blob sized generously.
        // The struct is ~256 bytes in astcenc 5.x; we reserve 1024 for forward compat.
        public const int ConfigStructSize = 1024;

        [DllImport(LibraryName, EntryPoint = "astcenc_config_init", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe AstcEncError ConfigInit(
            AstcEncProfile profile,
            uint blockX,
            uint blockY,
            uint blockZ,
            float quality,
            uint flags,
            void* config);

        [DllImport(LibraryName, EntryPoint = "astcenc_context_alloc", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe AstcEncError ContextAlloc(
            void* config,
            uint threadCount,
            out IntPtr context);

        [DllImport(LibraryName, EntryPoint = "astcenc_compress_image", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe AstcEncError CompressImage(
            IntPtr context,
            ref AstcImage image,
            in Swizzle swizzle,
            byte* dataOut,
            nuint dataLen,
            uint threadIndex);

        [DllImport(LibraryName, EntryPoint = "astcenc_compress_reset", CallingConvention = CallingConvention.Cdecl)]
        public static extern AstcEncError CompressReset(IntPtr context);

        [DllImport(LibraryName, EntryPoint = "astcenc_decompress_image", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe AstcEncError DecompressImage(
            IntPtr context,
            byte* data,
            nuint dataLen,
            ref AstcImage imageOut,
            in Swizzle swizzle,
            uint threadIndex);

        [DllImport(LibraryName, EntryPoint = "astcenc_decompress_reset", CallingConvention = CallingConvention.Cdecl)]
        public static extern AstcEncError DecompressReset(IntPtr context);

        [DllImport(LibraryName, EntryPoint = "astcenc_context_free", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ContextFree(IntPtr context);

        [DllImport(LibraryName, EntryPoint = "astcenc_get_error_string", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetErrorString(AstcEncError status);

        public static string GetErrorMessage(AstcEncError status)
        {
            var ptr = GetErrorString(status);
            return ptr == IntPtr.Zero ? status.ToString() : Marshal.PtrToStringAnsi(ptr) ?? status.ToString();
        }
    }
}
