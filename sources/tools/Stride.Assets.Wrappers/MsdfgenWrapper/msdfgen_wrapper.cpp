// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#include "msdfgen_wrapper.h"

#include <cmath>
#include <cstdlib>

#include <msdfgen.h>
#include <msdfgen-ext.h>

using namespace msdfgen;

struct MsdfgenContext {
    FreetypeHandle* ft;
};

struct MsdfgenFont {
    FontHandle* font;
};

extern "C" {

MsdfgenContext* msdfgenContextCreate() {
    FreetypeHandle* ft = initializeFreetype();
    if (!ft) return nullptr;
    auto* ctx = new (std::nothrow) MsdfgenContext{ft};
    if (!ctx) {
        deinitializeFreetype(ft);
        return nullptr;
    }
    return ctx;
}

void msdfgenContextDestroy(MsdfgenContext* ctx) {
    if (!ctx) return;
    if (ctx->ft) deinitializeFreetype(ctx->ft);
    delete ctx;
}

MsdfgenFont* msdfgenLoadFont(MsdfgenContext* ctx, const char* utf8Path) {
    if (!ctx || !ctx->ft || !utf8Path) return nullptr;
    FontHandle* fh = loadFont(ctx->ft, utf8Path);
    if (!fh) return nullptr;
    auto* wrapped = new (std::nothrow) MsdfgenFont{fh};
    if (!wrapped) {
        destroyFont(fh);
        return nullptr;
    }
    return wrapped;
}

void msdfgenUnloadFont(MsdfgenFont* font) {
    if (!font) return;
    if (font->font) destroyFont(font->font);
    delete font;
}

int msdfgenGenerateGlyph(
    MsdfgenFont* font,
    uint32_t unicode,
    double emSize,
    double pxRange,
    int32_t margin,
    MsdfgenGlyphInfo* info,
    uint8_t** outRgba)
{
    if (!font || !font->font || !info || !outRgba || emSize <= 0)
        return -1;

    *outRgba = nullptr;
    info->width = 0;
    info->height = 0;
    info->offsetX = 0;
    info->offsetY = 0;
    info->advance = 0;

    // Load the outline in EM-normalized coordinates: one em maps to 1.0 unit, so the pixel
    // scale is simply emSize. msdfgen also reports the advance in the same coordinate system.
    Shape shape;
    double advanceEm = 0;
    if (!loadGlyph(shape, font->font, unicode, FONT_SCALING_EM_NORMALIZED, &advanceEm))
        return -2;

    info->advance = advanceEm * emSize;

    shape.normalize();

    // Whitespace / outline-less glyph: advance only, no bitmap.
    if (shape.contours.empty())
        return 0;

    Shape::Bounds bounds = shape.getBounds();
    if (!(bounds.r > bounds.l) || !(bounds.t > bounds.b))
        return 0;

    const double scale = emSize; // pixels per em

    // The signed-distance field spreads pxRange/2 beyond the outline on each side; the border
    // must be at least that wide so the field isn't clipped at the bitmap edge.
    int border = margin;
    int rangePad = static_cast<int>(std::ceil(pxRange * 0.5));
    if (rangePad > border)
        border = rangePad;

    int width = static_cast<int>(std::ceil((bounds.r - bounds.l) * scale)) + 2 * border;
    int height = static_cast<int>(std::ceil((bounds.t - bounds.b) * scale)) + 2 * border;
    if (width <= 0 || height <= 0)
        return 0;

    // msdfgen projects shape coordinates as scale * (coord + translate). Placing the glyph's
    // bottom-left bound at the inner border fits the outline (plus its range) in the bitmap.
    Vector2 translate(border / scale - bounds.l, border / scale - bounds.b);
    double rangeShape = pxRange / scale;

    edgeColoringSimple(shape, 3.0);

    Bitmap<float, 3> bitmap(width, height);
    generateMSDF(bitmap, shape, rangeShape, Vector2(scale, scale), translate);

    size_t byteCount = static_cast<size_t>(width) * height * 4;
    uint8_t* buffer = static_cast<uint8_t*>(std::malloc(byteCount));
    if (!buffer)
        return -3;

    // msdfgen stores rows bottom-up (mathematical orientation); flip to top-down
    // so the buffer matches how downstream image libraries (ImageSharp) lay out rows.
    for (int y = 0; y < height; ++y) {
        const float* srcRow = bitmap(0, height - 1 - y);
        uint8_t* dstRow = buffer + static_cast<size_t>(y) * width * 4;
        for (int x = 0; x < width; ++x) {
            const float* p = srcRow + x * 3;
            dstRow[x * 4 + 0] = pixelFloatToByte(p[0]);
            dstRow[x * 4 + 1] = pixelFloatToByte(p[1]);
            dstRow[x * 4 + 2] = pixelFloatToByte(p[2]);
            dstRow[x * 4 + 3] = 255;
        }
    }

    info->width = width;
    info->height = height;
    // Placement in pixels relative to the pen origin / baseline (screen y-down). The bitmap's
    // top-left sits one border outside the glyph's top-left bound.
    info->offsetX = bounds.l * scale - border;
    info->offsetY = -(bounds.t * scale) - border;
    *outRgba = buffer;
    return 0;
}

void msdfgenFreeBitmap(uint8_t* rgba) {
    std::free(rgba);
}

} // extern "C"
