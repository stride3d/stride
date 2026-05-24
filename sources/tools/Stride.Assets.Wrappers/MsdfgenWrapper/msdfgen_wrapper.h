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

// Library lifecycle. One context per process is sufficient; the underlying
// FreeType handle is shared by all fonts loaded through it.
MSDF_API MsdfgenContext* msdfgenContextCreate();
MSDF_API void            msdfgenContextDestroy(MsdfgenContext* ctx);

// Font lifecycle. utf8Path is a UTF-8 file path to a TTF/OTF file.
// Returns nullptr on failure (file missing, not a font, etc.).
MSDF_API MsdfgenFont* msdfgenLoadFont(MsdfgenContext* ctx, const char* utf8Path);
MSDF_API void         msdfgenUnloadFont(MsdfgenFont* font);

// Generate MSDF for a glyph.
//
//   font       : font handle from msdfgenLoadFont.
//   unicode    : Unicode code point of the glyph.
//   width,height : output bitmap dimensions in pixels.
//   translateX,translateY : translation applied in shape coordinates.
//   scaleX,scaleY         : per-axis scale applied in shape coordinates.
//   range      : SDF range in shape coordinates (msdfgen CLI default is 4.0).
//   outRgba    : caller-allocated buffer of width*height*4 bytes. The wrapper
//                writes R/G/B from the three MSDF channels (top-down rows,
//                msdfgen's bottom-up output is flipped here) and A = 255.
//
// Returns 0 on success, non-zero on failure (glyph not found, etc.).
MSDF_API int msdfgenGenerateMsdf(
    MsdfgenFont* font,
    uint32_t unicode,
    int32_t width, int32_t height,
    double translateX, double translateY,
    double scaleX, double scaleY,
    double range,
    uint8_t* outRgba);

#ifdef __cplusplus
}
#endif

#endif // STRIDE_MSDFGEN_WRAPPER_H
