using Sdl = SDL3.SDL;

namespace Stride.Graphics
{
    public static class SdlConvertExtensions
    {
        public static bool TryMapPixelFormat(this Sdl.PixelFormat sdlFormat, out PixelFormat strideFormat)
        {
            strideFormat = PixelFormat.None;

            switch (sdlFormat)
            {
                case Sdl.PixelFormat.Unknown:
                    return false;

                // ---------------------------
                // 1-bit, 2-bit, 4-bit indexed
                // ---------------------------
                case Sdl.PixelFormat.Index1LSB:
                case Sdl.PixelFormat.Index1MSB:
                case Sdl.PixelFormat.Index2LSB:
                case Sdl.PixelFormat.Index2MSB:
                case Sdl.PixelFormat.Index4LSB:
                case Sdl.PixelFormat.Index4MSB:
                    return false; // Stride has no paletted formats

                // ---------------------------
                // 8-bit indexed
                // ---------------------------
                case Sdl.PixelFormat.Index8:
                    strideFormat = PixelFormat.R8_UNorm;
                    return true;

                // ---------------------------
                // Packed 8-bit RGB
                // ---------------------------
                case Sdl.PixelFormat.RGB332:
                    return false; // No 3-3-2 format in Stride

                // ---------------------------
                // 16-bit packed formats
                // ---------------------------
                case Sdl.PixelFormat.XRGB4444:
                case Sdl.PixelFormat.XBGR4444:
                case Sdl.PixelFormat.ARGB4444:
                case Sdl.PixelFormat.RGBA4444:
                case Sdl.PixelFormat.ABGR4444:
                case Sdl.PixelFormat.BGRA4444:
                    return false; // Stride does not support 4-4-4-4

                case Sdl.PixelFormat.XRGB1555:
                case Sdl.PixelFormat.XBGR1555:
                    return false; // Stride does not support 1-5-5-5 or 5-5-5-1 without alpha

                case Sdl.PixelFormat.ARGB1555:
                case Sdl.PixelFormat.ABGR1555:
                case Sdl.PixelFormat.BGRA5551:
                case Sdl.PixelFormat.RGBA5551:
                    strideFormat = PixelFormat.B5G5R5A1_UNorm;
                    return true;

                case Sdl.PixelFormat.RGB565:
                case Sdl.PixelFormat.BGR565:
                    strideFormat = PixelFormat.B5G6R5_UNorm; // Closest match
                    return true;

                // ---------------------------
                // 24-bit formats
                // ---------------------------
                case Sdl.PixelFormat.RGB24:
                case Sdl.PixelFormat.BGR24:
                    return false; // Stride has no 24-bit formats

                // ---------------------------
                // 32-bit packed formats
                // ---------------------------
                case Sdl.PixelFormat.XRGB8888:
                case Sdl.PixelFormat.RGBX8888:
                case Sdl.PixelFormat.XBGR8888:
                case Sdl.PixelFormat.BGRX8888:
                    strideFormat = PixelFormat.B8G8R8X8_UNorm;
                    return true;

                case Sdl.PixelFormat.RGBA8888:
                case Sdl.PixelFormat.ABGR8888:
                    strideFormat = PixelFormat.R8G8B8A8_UNorm; // ABGR → RGBA
                    return true;

                case Sdl.PixelFormat.ARGB8888: // DXGI uses BGRA
                case Sdl.PixelFormat.BGRA8888:
                    strideFormat = PixelFormat.B8G8R8A8_UNorm;
                    return true;

                // ---------------------------
                // 10-10-10-2 formats
                // ---------------------------
                case Sdl.PixelFormat.XRGB2101010:
                case Sdl.PixelFormat.XBGR2101010:
                    return false;

                case Sdl.PixelFormat.ARGB2101010:
                case Sdl.PixelFormat.ABGR2101010:
                    strideFormat = PixelFormat.R10G10B10A2_UNorm;
                    return true;

                // ---------------------------
                // 48-bit (16-bit per channel)
                // ---------------------------
                case Sdl.PixelFormat.RGB48:
                case Sdl.PixelFormat.BGR48:
                    return false;

                // ---------------------------
                // 48-bit (Float 16-bit per channel)
                // ---------------------------
                case Sdl.PixelFormat.RGB48Float:
                case Sdl.PixelFormat.BGR48Float:
                    return false;

                // ---------------------------
                // 64-bit (16-bit per channel)
                // ---------------------------
                case Sdl.PixelFormat.RGBA64:
                case Sdl.PixelFormat.ARGB64:
                case Sdl.PixelFormat.BGRA64:
                case Sdl.PixelFormat.ABGR64:
                    strideFormat = PixelFormat.R16G16B16A16_UNorm;
                    return true;

                // ---------------------------
                // 64-bit (Float 16-bit per channel)
                // ---------------------------
                case Sdl.PixelFormat.RGBA64Float:
                case Sdl.PixelFormat.ARGB64Float:
                case Sdl.PixelFormat.BGRA64Float:
                case Sdl.PixelFormat.ABGR64Float:
                    strideFormat = PixelFormat.R16G16B16A16_Float;
                    return true;

                // ---------------------------
                // 96-bit (Float 32-bit per channel)
                // ---------------------------
                case Sdl.PixelFormat.RGB96Float:
                    strideFormat = PixelFormat.R32G32B32_Float;
                    return true;

                case Sdl.PixelFormat.BGR96Float:
                    return false;

                // ---------------------------
                // 128-bit (Float 32-bit per channel)
                // ---------------------------
                case Sdl.PixelFormat.RGBA128Float:
                case Sdl.PixelFormat.ARGB128Float:
                case Sdl.PixelFormat.BGRA128Float:
                case Sdl.PixelFormat.ABGR128Float:
                    strideFormat = PixelFormat.R32G32B32A32_Float;
                    return true;

                // ---------------------------
                // YUV FOURCC formats
                // ---------------------------
                case Sdl.PixelFormat.YV12:
                case Sdl.PixelFormat.IYUV:
                case Sdl.PixelFormat.YUY2:
                case Sdl.PixelFormat.UYVY:
                case Sdl.PixelFormat.YVYU:
                case Sdl.PixelFormat.NV12:
                case Sdl.PixelFormat.NV21:
                case Sdl.PixelFormat.P010:
                    return false; // Stride has no YUV formats

                // ---------------------------
                // External OES (Android)
                // ---------------------------
                case Sdl.PixelFormat.ExternalOES:
                    return false;

                // ---------------------------
                // MJPEG
                // ---------------------------
                case Sdl.PixelFormat.MJPG:
                    return false; // Stride does not support MJPEG textures

                default:
                    return false;
            }
        }


        public static bool TryGetSrgbEquivalent(this PixelFormat format, out PixelFormat srgbFormat)
        {
            switch (format)
            {
                case PixelFormat.R8G8B8A8_UNorm:
                    srgbFormat = PixelFormat.R8G8B8A8_UNorm_SRgb;
                    return true;

                case PixelFormat.B8G8R8A8_UNorm:
                    srgbFormat = PixelFormat.B8G8R8A8_UNorm_SRgb;
                    return true;

                case PixelFormat.B8G8R8X8_UNorm:
                    srgbFormat = PixelFormat.B8G8R8X8_UNorm_SRgb;
                    return true;

                default:
                    srgbFormat = PixelFormat.None;
                    return false;
            }
        }
    }
}
