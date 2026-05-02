// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.InteropServices;

namespace Stride.Graphics.Font
{
    // Minimal FreeType bindings using CLong for correct cross-platform ABI.
    // C FreeType defines FT_Long/FT_Pos/FT_Fixed/FT_F26Dot6 as C `long`, which is
    // 4 bytes on Windows (LLP64) and 8 bytes on Linux/macOS (LP64).
    // .NET's CLong matches this exactly.

    internal enum FreeTypePixelMode : byte
    {
        None = 0,
        Mono = 1,
        Gray = 2,
        Gray2 = 3,
        Gray4 = 4,
        Lcd = 5,
        LcdV = 6,
        Bgra = 7,
    }

    internal enum FreeTypeRenderMode
    {
        Normal = 0,
        Light = 1,
        Mono = 2,
        Lcd = 3,
        LcdV = 4,
    }

    [Flags]
    internal enum FreeTypeLoadFlags
    {
        Default = 0x0,
        NoScale = 0x1,
        NoHinting = 0x2,
        Render = 0x4,
        NoBitmap = 0x8,
        VerticalLayout = 0x10,
        ForceAutohint = 0x20,
        CropBitmap = 0x40,
        Pedantic = 0x80,
        NoRecurse = 0x400,
        IgnoreTransform = 0x800,
        Monochrome = 0x1000,
        LinearDesign = 0x2000,
        NoAutohint = 0x8000,
        Color = 0x100000,
    }

    internal enum FreeTypeLoadTarget
    {
        Normal = 0,
        Light = 0x10000,
        Mono = 0x20000,
        Lcd = 0x30000,
        LcdV = 0x40000,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FT_Generic
    {
        public nint data;
        public nint finalizer;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FT_Vector
    {
        public CLong x;
        public CLong y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FT_BBox
    {
        public CLong xMin;
        public CLong yMin;
        public CLong xMax;
        public CLong yMax;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FT_Glyph_Metrics
    {
        public CLong width;
        public CLong height;
        public CLong horiBearingX;
        public CLong horiBearingY;
        public CLong horiAdvance;
        public CLong vertBearingX;
        public CLong vertBearingY;
        public CLong vertAdvance;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct FT_Bitmap
    {
        public uint rows;
        public uint width;
        public int pitch;
        public byte* buffer;
        public ushort num_grays;
        public byte pixel_mode;
        public byte palette_mode;
        public void* palette;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct FT_Outline
    {
        public ushort n_contours;
        public ushort n_points;
        public FT_Vector* points;
        public byte* tags;
        public ushort* contours;
        public int flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct FT_GlyphSlotRec
    {
        public nint library;
        public nint face;
        public FT_GlyphSlotRec* next;
        public uint glyph_index;
        public FT_Generic generic;
        public FT_Glyph_Metrics metrics;
        public CLong linearHoriAdvance;
        public CLong linearVertAdvance;
        public FT_Vector advance;
        public uint format;
        public FT_Bitmap bitmap;
        public int bitmap_left;
        public int bitmap_top;
        public FT_Outline outline;
        public uint num_subglyphs;
        public nint subglyphs;
        public void* control_data;
        public CLong control_len;
        public CLong lsb_delta;
        public CLong rsb_delta;
        public void* other;
        public nint @internal;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct FT_Bitmap_Size
    {
        public short height;
        public short width;
        public CLong size;
        public CLong x_ppem;
        public CLong y_ppem;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FT_ListRec
    {
        public nint head;
        public nint tail;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct FT_FaceRec
    {
        public CLong num_faces;
        public CLong face_index;
        public CLong face_flags;
        public CLong style_flags;
        public CLong num_glyphs;
        public byte* family_name;
        public byte* style_name;
        public int num_fixed_sizes;
        public FT_Bitmap_Size* available_sizes;
        public int num_charmaps;
        public nint charmaps;
        public FT_Generic generic;
        public FT_BBox bbox;
        public ushort units_per_EM;
        public short ascender;
        public short descender;
        public short height;
        public short max_advance_width;
        public short max_advance_height;
        public short underline_position;
        public short underline_thickness;
        public FT_GlyphSlotRec* glyph;
        public nint size;
        public nint charmap;
        // private fields follow (driver, memory, stream, etc.) — not needed
    }

    // Outline extraction
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int FT_Outline_MoveToFunc(FT_Vector* to, nint user);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int FT_Outline_LineToFunc(FT_Vector* to, nint user);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int FT_Outline_ConicToFunc(FT_Vector* control, FT_Vector* to, nint user);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int FT_Outline_CubicToFunc(FT_Vector* control1, FT_Vector* control2, FT_Vector* to, nint user);

    [StructLayout(LayoutKind.Sequential)]
    internal struct FT_Outline_Funcs
    {
        public nint move_to;
        public nint line_to;
        public nint conic_to;
        public nint cubic_to;
        public int shift;
        public CLong delta;
    }

    internal static unsafe class FreeTypeNative
    {
        private const string FreetypeLib = "freetype";

        [DllImport(FreetypeLib, CallingConvention = CallingConvention.Cdecl)]
        public static extern int FT_Init_FreeType(out nint library);

        [DllImport(FreetypeLib, CallingConvention = CallingConvention.Cdecl)]
        public static extern int FT_Done_FreeType(nint library);

        [DllImport(FreetypeLib, CallingConvention = CallingConvention.Cdecl)]
        public static extern int FT_New_Memory_Face(nint library, byte* file_base, CLong file_size, CLong face_index, out FT_FaceRec* aface);

        [DllImport(FreetypeLib, CallingConvention = CallingConvention.Cdecl)]
        public static extern int FT_Done_Face(FT_FaceRec* face);

        [DllImport(FreetypeLib, CallingConvention = CallingConvention.Cdecl)]
        public static extern int FT_Set_Char_Size(FT_FaceRec* face, CLong char_width, CLong char_height, uint horz_resolution, uint vert_resolution);

        [DllImport(FreetypeLib, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint FT_Get_Char_Index(FT_FaceRec* face, uint charcode);

        [DllImport(FreetypeLib, CallingConvention = CallingConvention.Cdecl)]
        public static extern int FT_Load_Glyph(FT_FaceRec* face, uint glyph_index, int load_flags);

        [DllImport(FreetypeLib, CallingConvention = CallingConvention.Cdecl)]
        public static extern int FT_Render_Glyph(FT_GlyphSlotRec* slot, FreeTypeRenderMode render_mode);

        [DllImport(FreetypeLib, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int FT_Outline_Decompose(
            FT_Outline* outline,
            FT_Outline_Funcs* func_interface,
            nint user);
    }
}
