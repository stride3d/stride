// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#ifndef STRIDE_MSDFGEN_WRAPPER_H
#define STRIDE_MSDFGEN_WRAPPER_H

#ifdef _MSC_VER
#define MSDF_API __declspec(dllexport)
#else
#if __GNUC__ >= 4
#define MSDF_API __attribute__((visibility("default")))
#else
#define MSDF_API
#endif
#endif

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

// Opaque handles. C# side treats both as IntPtr.
typedef struct MsdfgenContext MsdfgenContext;
typedef struct MsdfgenFont    MsdfgenFont;

// Placement of a generated glyph, all in pixels. Filled by msdfgenGenerateGlyph.
typedef struct MsdfgenGlyphInfo {
    int32_t width;     // generated bitmap width  (0 for whitespace / outline-less glyphs)
    int32_t height;    // generated bitmap height (0 for whitespace / outline-less glyphs)
    double  offsetX;   // bitmap left edge relative to the pen origin (x to the right)
    double  offsetY;   // bitmap top edge relative to the baseline (screen y-down)
    double  advance;   // horizontal advance
} MsdfgenGlyphInfo;

// Library lifecycle. One context per process is sufficient; the underlying
// FreeType handle is shared by all fonts loaded through it.
MSDF_API MsdfgenContext* msdfgenContextCreate();
MSDF_API void            msdfgenContextDestroy(MsdfgenContext* ctx);

// Font lifecycle. utf8Path is a UTF-8 file path to a TTF/OTF file.
// Returns nullptr on failure (file missing, not a font, etc.).
MSDF_API MsdfgenFont* msdfgenLoadFont(MsdfgenContext* ctx, const char* utf8Path);
MSDF_API void         msdfgenUnloadFont(MsdfgenFont* font);

// Generate the MSDF for a glyph, framing it from the glyph's own outline bounds.
// The caller supplies only the target em size and SDF range in pixels; msdfgen
// computes the bitmap size, scale and translation from the shape itself, so the
// caller never converts between font/shape/pixel coordinate systems.
//
//   font     : font handle from msdfgenLoadFont.
//   unicode  : Unicode code point of the glyph.
//   emSize   : target glyph size in pixels (pixels per em).
//   pxRange  : width of the signed-distance range, in pixels.
//   margin   : minimum transparent border around the glyph, in pixels.
//   info     : receives the generated bitmap size and pixel-space placement.
//   outRgba  : receives a newly allocated width*height*4 RGBA buffer (top-down
//              rows, A = 255), owned by the caller and freed via msdfgenFreeBitmap.
//              Set to NULL for whitespace / outline-less glyphs (info.advance is
//              still valid).
//
// Returns 0 on success (including whitespace), non-zero on failure (glyph not
// found, allocation failure, etc.).
MSDF_API int msdfgenGenerateGlyph(
    MsdfgenFont* font,
    uint32_t unicode,
    double emSize,
    double pxRange,
    int32_t margin,
    MsdfgenGlyphInfo* info,
    uint8_t** outRgba);

// Frees a buffer returned through msdfgenGenerateGlyph's outRgba.
MSDF_API void msdfgenFreeBitmap(uint8_t* rgba);

#ifdef __cplusplus
}
#endif

#endif // STRIDE_MSDFGEN_WRAPPER_H
