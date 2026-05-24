// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#include "msdfgen_wrapper.h"

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

int msdfgenGenerateMsdf(
    MsdfgenFont* font,
    uint32_t unicode,
    int32_t width, int32_t height,
    double translateX, double translateY,
    double scaleX, double scaleY,
    double range,
    uint8_t* outRgba)
{
    if (!font || !font->font || !outRgba || width <= 0 || height <= 0)
        return -1;

    Shape shape;
    if (!loadGlyph(shape, font->font, unicode))
        return -2;

    shape.normalize();
    edgeColoringSimple(shape, 3.0);

    Bitmap<float, 3> bitmap(width, height);
    generateMSDF(bitmap, shape, range, Vector2(scaleX, scaleY), Vector2(translateX, translateY));

    // msdfgen stores rows bottom-up (mathematical orientation); flip to top-down
    // so the buffer matches how downstream image libraries (ImageSharp) lay out rows.
    for (int32_t y = 0; y < height; ++y) {
        const float* srcRow = bitmap(0, height - 1 - y);
        uint8_t* dstRow = outRgba + static_cast<size_t>(y) * width * 4;
        for (int32_t x = 0; x < width; ++x) {
            const float* p = srcRow + x * 3;
            dstRow[x * 4 + 0] = pixelFloatToByte(p[0]);
            dstRow[x * 4 + 1] = pixelFloatToByte(p[1]);
            dstRow[x * 4 + 2] = pixelFloatToByte(p[2]);
            dstRow[x * 4 + 3] = 255;
        }
    }
    return 0;
}

} // extern "C"
