// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_OPENGL 
using System;
using static Android.Hardware.Camera;

namespace Stride.Graphics
{
    internal static class OpenGLConvertExtensions
    {
        public static PrimitiveTypeGl ToOpenGL(this PrimitiveType primitiveType)
        {
            switch (primitiveType)
            {
                case PrimitiveType.PointList:
                    return PrimitiveTypeGl.Points;
                case PrimitiveType.LineList:
                    return PrimitiveTypeGl.Lines;
                case PrimitiveType.LineStrip:
                    return PrimitiveTypeGl.LineStrip;
                case PrimitiveType.TriangleList:
                    return PrimitiveTypeGl.Triangles;
                case PrimitiveType.TriangleStrip:
                    return PrimitiveTypeGl.TriangleStrip;
                default:
                    // Undefined
                    return PrimitiveTypeGl.Triangles;
            }
        }

        public static MapBufferAccessMask ToOpenGLMask(this MapMode mapMode)
        {
            switch (mapMode)
            {
                case MapMode.Read:
                    return MapBufferAccessMask.MapReadBit;
                case MapMode.Write:
                    return MapBufferAccessMask.MapWriteBit;
                case MapMode.ReadWrite:
                    return MapBufferAccessMask.MapReadBit | MapBufferAccessMask.MapWriteBit;
                case MapMode.WriteDiscard:
                    return MapBufferAccessMask.MapWriteBit | MapBufferAccessMask.MapInvalidateBufferBit;
                case MapMode.WriteNoOverwrite:
                    return MapBufferAccessMask.MapWriteBit | MapBufferAccessMask.MapUnsynchronizedBit;
                default:
                    throw new ArgumentOutOfRangeException("mapMode");
            }
        }

#if STRIDE_GRAPHICS_API_OPENGLES
        public static PrimitiveTypeGl ToOpenGLES(this PrimitiveType primitiveType)
        {
            switch (primitiveType)
            {
                case PrimitiveType.PointList:
                    return PrimitiveTypeGl.Points;
                case PrimitiveType.LineList:
                    return PrimitiveTypeGl.Lines;
                case PrimitiveType.LineStrip:
                    return PrimitiveTypeGl.LineStrip;
                case PrimitiveType.TriangleList:
                    return PrimitiveTypeGl.Triangles;
                case PrimitiveType.TriangleStrip:
                    return PrimitiveTypeGl.TriangleStrip;
                default:
                    throw new NotImplementedException();
            }
        }
#else
        public static BufferAccessARB ToOpenGL(this MapMode mapMode)
        {
            switch (mapMode)
            {
                case MapMode.Read:
                    return BufferAccessARB.ReadOnly;
                case MapMode.Write:
                case MapMode.WriteDiscard:
                case MapMode.WriteNoOverwrite:
                    return BufferAccessARB.WriteOnly;
                case MapMode.ReadWrite:
                    return BufferAccessARB.ReadWrite;
                default:
                    throw new ArgumentOutOfRangeException("mapMode");
            }
        }
#endif

        public static TextureWrapMode ToOpenGL(this TextureAddressMode addressMode)
        {
            switch (addressMode)
            {
                case TextureAddressMode.Border:
                    return TextureWrapMode.ClampToBorder;
                case TextureAddressMode.Clamp:
                    return TextureWrapMode.ClampToEdge;
                case TextureAddressMode.Mirror:
                    return TextureWrapMode.MirroredRepeat;
                case TextureAddressMode.Wrap:
                    return TextureWrapMode.Repeat;
                default:
                    throw new NotImplementedException();
            }
        }

        public static DepthFunction ToOpenGLDepthFunction(this CompareFunction function)
        {
            switch (function)
            {
                case CompareFunction.Always:
                    return DepthFunction.Always;
                case CompareFunction.Equal:
                    return DepthFunction.Equal;
                case CompareFunction.GreaterEqual:
                    return DepthFunction.Gequal;
                case CompareFunction.Greater:
                    return DepthFunction.Greater;
                case CompareFunction.LessEqual:
                    return DepthFunction.Lequal;
                case CompareFunction.Less:
                    return DepthFunction.Less;
                case CompareFunction.Never:
                    return DepthFunction.Never;
                case CompareFunction.NotEqual:
                    return DepthFunction.Notequal;
                default:
                    throw new NotImplementedException();
            }
        }

        public static StencilFunction ToOpenGLStencilFunction(this CompareFunction function)
        {
            switch (function)
            {
                case CompareFunction.Always:
                    return StencilFunction.Always;
                case CompareFunction.Equal:
                    return StencilFunction.Equal;
                case CompareFunction.GreaterEqual:
                    return StencilFunction.Gequal;
                case CompareFunction.Greater:
                    return StencilFunction.Greater;
                case CompareFunction.LessEqual:
                    return StencilFunction.Lequal;
                case CompareFunction.Less:
                    return StencilFunction.Less;
                case CompareFunction.Never:
                    return StencilFunction.Never;
                case CompareFunction.NotEqual:
                    return StencilFunction.Notequal;
                default:
                    throw new NotImplementedException();
            }
        }

        public static StencilOp ToOpenGL(this StencilOperation operation)
        {
            switch (operation)
            {
                case StencilOperation.Keep:
                    return StencilOp.Keep;
                case StencilOperation.Zero:
                    return StencilOp.Zero;
                case StencilOperation.Replace:
                    return StencilOp.Replace;
                case StencilOperation.IncrementSaturation:
                    return StencilOp.Incr;
                case StencilOperation.DecrementSaturation:
                    return StencilOp.Decr;
                case StencilOperation.Invert:
                    return StencilOp.Invert;
                case StencilOperation.Increment:
                    return StencilOp.IncrWrap;
                case StencilOperation.Decrement:
                    return StencilOp.DecrWrap;
                default:
                    throw new ArgumentOutOfRangeException("operation");
            }
        }

        public static void ConvertPixelFormat(GraphicsDevice graphicsDevice, ref PixelFormat inputFormat, out InternalFormat internalFormat, out PixelFormatGl format, out PixelType type,
            out int pixelSize, out bool compressed)
        {
            compressed = false;

            // If the Device doesn't support SRGB, we remap automatically the format to non-srgb
            if (!graphicsDevice.Features.HasSRgb)
            {
                switch (inputFormat)
                {
                    case PixelFormat.ETC2_RGB_SRgb:
                        inputFormat = PixelFormat.ETC2_RGB;
                        break;
                    case PixelFormat.ETC2_RGBA_SRgb:
                        inputFormat = PixelFormat.ETC2_RGBA;
                        break;
                    case PixelFormat.R8G8B8A8_UNorm_SRgb:
                        inputFormat = PixelFormat.R8G8B8A8_UNorm;
                        break;
                    case PixelFormat.B8G8R8A8_UNorm_SRgb:
                        inputFormat = PixelFormat.B8G8R8A8_UNorm;
                        break;
                }
            }

            switch (inputFormat)
            {
                case PixelFormat.A8_UNorm:
                    internalFormat = (InternalFormat)GLEnum.Alpha;
                    format = PixelFormatGl.Alpha;
                    type = PixelType.UnsignedByte;
                    pixelSize = 1;
                    break;
                case PixelFormat.R8_UNorm:
                    internalFormat = InternalFormat.R8;
                    format = PixelFormatGl.Red;
                    type = PixelType.UnsignedByte;
                    pixelSize = 1;
                    break;
                case PixelFormat.R8G8B8A8_UNorm:
                    internalFormat = InternalFormat.Rgba;
                    format = PixelFormatGl.Rgba;
                    type = PixelType.UnsignedByte;
                    pixelSize = 4;
                    break;
                case PixelFormat.B8G8R8A8_UNorm:
                    if (!graphicsDevice.HasExtTextureFormatBGRA8888)
                        throw new NotSupportedException();

                    internalFormat = (InternalFormat)PixelFormatGl.Bgra;
                    format = PixelFormatGl.Bgra;
                    type = PixelType.UnsignedByte;
                    pixelSize = 4;
                    break;
                case PixelFormat.R8G8B8A8_UNorm_SRgb:
                    internalFormat = InternalFormat.Srgb8Alpha8;
                    format = PixelFormatGl.Rgba;
                    type = PixelType.UnsignedByte;
                    pixelSize = 4;
                    break;
#if STRIDE_GRAPHICS_API_OPENGLCORE
                case PixelFormat.B8G8R8A8_UNorm_SRgb:
                    // TODO: Check on iOS/Android and OpenGL 3
                    internalFormat = InternalFormat.Srgb8Alpha8;
                    format = PixelFormatGl.Bgra;
                    type = PixelType.UnsignedByte;
                    pixelSize = 4;
                    break;
                case PixelFormat.BC1_UNorm:
                    if (!graphicsDevice.HasDXT)
                        throw new NotSupportedException();
                    internalFormat = InternalFormat.CompressedRgbaS3TCDxt1Ext;
                    format = PixelFormatGl.Rgb;
                    pixelSize = 4;
                    type = PixelType.UnsignedByte;
                    compressed = true;
                    break;
                case PixelFormat.BC1_UNorm_SRgb:
                    if (!graphicsDevice.HasDXT)
                        throw new NotSupportedException();
                    internalFormat = InternalFormat.CompressedSrgbAlphaS3TCDxt1Ext;
                    format = PixelFormatGl.Rgb;
                    pixelSize = 4;
                    type = PixelType.UnsignedByte;
                    compressed = true;
                    break;
                case PixelFormat.BC2_UNorm:
                    if (!graphicsDevice.HasDXT)
                        throw new NotSupportedException();
                    internalFormat = InternalFormat.CompressedRgbaS3TCDxt3Ext;
                    format = PixelFormatGl.Rgba;
                    pixelSize = 4;
                    type = PixelType.UnsignedByte;
                    compressed = true;
                    break;
                case PixelFormat.BC2_UNorm_SRgb:
                    if (!graphicsDevice.HasDXT)
                        throw new NotSupportedException();
                    internalFormat = InternalFormat.CompressedSrgbAlphaS3TCDxt3Ext;
                    format = PixelFormatGl.Rgba;
                    pixelSize = 4;
                    type = PixelType.UnsignedByte;
                    compressed = true;
                    break;
                case PixelFormat.BC3_UNorm:
                    if (!graphicsDevice.HasDXT)
                        throw new NotSupportedException();
                    internalFormat = InternalFormat.CompressedRgbaS3TCDxt5Ext;
                    format = PixelFormatGl.Rgba;
                    pixelSize = 4;
                    type = PixelType.UnsignedByte;
                    compressed = true;
                    break;
                case PixelFormat.BC3_UNorm_SRgb:
                    if (!graphicsDevice.HasDXT)
                        throw new NotSupportedException();
                    internalFormat = InternalFormat.CompressedSrgbAlphaS3TCDxt5Ext;
                    format = PixelFormatGl.Rgba;
                    pixelSize = 4;
                    type = PixelType.UnsignedByte;
                    compressed = true;
                    break;
#endif
                case PixelFormat.R16_SInt:
                    internalFormat = InternalFormat.R16i;
                    format = PixelFormatGl.RedInteger;
                    type = PixelType.Short;
                    pixelSize = 2;
                    break;
                case PixelFormat.R16_UInt:
                    internalFormat = InternalFormat.R16ui;
                    format = PixelFormatGl.RedInteger;
                    type = PixelType.UnsignedShort;
                    pixelSize = 2;
                    break;
                case PixelFormat.R16_Float:
                    internalFormat = InternalFormat.R16f;
                    format = PixelFormatGl.Red;
                    type = (PixelType)GLEnum.HalfFloat;
                    pixelSize = 2;
                    break;
                case PixelFormat.R16G16_SInt:
                    internalFormat = InternalFormat.RG16i;
                    format = PixelFormatGl.RGInteger;
                    type = PixelType.Short;
                    pixelSize = 4;
                    break;
                case PixelFormat.R16G16_UInt:
                    internalFormat = InternalFormat.RG16ui;
                    format = PixelFormatGl.RGInteger;
                    type = PixelType.UnsignedShort;
                    pixelSize = 4;
                    break;
                case PixelFormat.R16G16_Float:
                    internalFormat = InternalFormat.RG16f;
                    format = PixelFormatGl.RG;
                    type = (PixelType)GLEnum.HalfFloat;
                    pixelSize = 4;
                    break;
                case PixelFormat.R16G16B16A16_SInt:
                    internalFormat = InternalFormat.Rgba16i;
                    format = PixelFormatGl.RgbaInteger;
                    type = PixelType.Short;
                    pixelSize = 8;
                    break;
                case PixelFormat.R16G16B16A16_UInt:
                    internalFormat = InternalFormat.Rgba16ui;
                    format = PixelFormatGl.RgbaInteger;
                    type = PixelType.UnsignedShort;
                    pixelSize = 8;
                    break;
                case PixelFormat.R16G16B16A16_Float:
                    internalFormat = InternalFormat.Rgba16f;
                    format = PixelFormatGl.Rgba;
                    type = (PixelType)GLEnum.HalfFloat;
                    pixelSize = 8;
                    break;
                case PixelFormat.R10G10B10A2_UNorm:
                    internalFormat = InternalFormat.Rgb10A2;
                    format = PixelFormatGl.Rgba;
                    type = PixelType.UnsignedInt1010102;
                    pixelSize = 4;
                    break;
                case PixelFormat.R11G11B10_Float:
                    internalFormat = InternalFormat.R11fG11fB10f;
                    format = PixelFormatGl.Rgb;
                    type = (PixelType)GLEnum.HalfFloat;
                    pixelSize = 4;
                    break;
                case PixelFormat.R32_SInt:
                    internalFormat = InternalFormat.R32i;
                    format = PixelFormatGl.RedInteger;
                    type = PixelType.Int;
                    pixelSize = 4;
                    break;
                case PixelFormat.R32_UInt:
                    internalFormat = InternalFormat.R32ui;
                    format = PixelFormatGl.RedInteger;
                    type = PixelType.UnsignedInt;
                    pixelSize = 4;
                    break;
                case PixelFormat.R32_Float:
                    internalFormat = InternalFormat.R32f;
                    format = PixelFormatGl.Red;
                    type = PixelType.Float;
                    pixelSize = 4;
                    break;
                case PixelFormat.R32G32_SInt:
                    internalFormat = InternalFormat.RG32i;
                    format = PixelFormatGl.RGInteger;
                    type = PixelType.Int;
                    pixelSize = 8;
                    break;
                case PixelFormat.R32G32_UInt:
                    internalFormat = InternalFormat.RG32ui;
                    format = PixelFormatGl.RGInteger;
                    type = PixelType.UnsignedInt;
                    pixelSize = 8;
                    break;
                case PixelFormat.R32G32_Float:
                    internalFormat = InternalFormat.RG32f;
                    format = PixelFormatGl.RG;
                    type = PixelType.Float;
                    pixelSize = 8;
                    break;
                case PixelFormat.R32G32B32_SInt:
                    internalFormat = InternalFormat.Rgb32i;
                    format = PixelFormatGl.RgbInteger;
                    type = PixelType.Int;
                    pixelSize = 12;
                    break;
                case PixelFormat.R32G32B32_UInt:
                    internalFormat = InternalFormat.Rgb32ui;
                    format = PixelFormatGl.RgbInteger;
                    type = PixelType.UnsignedInt;
                    pixelSize = 12;
                    break;
                case PixelFormat.R32G32B32_Float:
                    internalFormat = InternalFormat.Rgb32f;
                    format = PixelFormatGl.Rgb;
                    type = PixelType.Float;
                    pixelSize = 12;
                    break;
                case PixelFormat.R32G32B32A32_SInt:
                    internalFormat = InternalFormat.Rgba32i;
                    format = PixelFormatGl.RgbaInteger;
                    type = PixelType.Int;
                    pixelSize = 16;
                    break;
                case PixelFormat.R32G32B32A32_UInt:
                    internalFormat = InternalFormat.Rgba32ui;
                    format = PixelFormatGl.RgbaInteger;
                    type = PixelType.UnsignedInt;
                    pixelSize = 16;
                    break;
                case PixelFormat.R32G32B32A32_Float:
                    internalFormat = InternalFormat.Rgba32f;
                    format = PixelFormatGl.Rgba;
                    type = PixelType.Float;
                    pixelSize = 16;
                    break;
                case PixelFormat.D16_UNorm:
                    internalFormat = InternalFormat.DepthComponent16;
                    format = PixelFormatGl.DepthComponent;
                    type = PixelType.UnsignedShort;
                    pixelSize = 2;
                    break;
                case PixelFormat.D24_UNorm_S8_UInt:
                    internalFormat = InternalFormat.Depth24Stencil8;
                    format = PixelFormatGl.DepthStencil;
                    type = (PixelType)GLEnum.UnsignedInt248;
                    pixelSize = 4;
                    break;
                // TODO: Temporary depth format (need to decide relation between RenderTarget1D and Texture)
                case PixelFormat.D32_Float:
                    internalFormat = InternalFormat.DepthComponent32f;
                    format = PixelFormatGl.DepthComponent;
                    type = PixelType.Float;
                    pixelSize = 4;
                    break;
#if STRIDE_GRAPHICS_API_OPENGLES
                // Desktop OpenGLES
                case PixelFormat.ETC1:
                    // TODO: Runtime check for extension?
                    internalFormat = InternalFormat.Etc1Rgb8Oes;
                    format = PixelFormatGl.Rgb;
                    compressed = true;
                    pixelSize = 2;
                    type = PixelType.UnsignedByte;
                    break;
                case PixelFormat.ETC2_RGBA:
                    internalFormat = InternalFormat.CompressedRgba8Etc2Eac;
                    format = PixelFormatGl.Rgba;
                    compressed = true;
                    pixelSize = 2;
                    type = PixelType.UnsignedByte;
                    break;
                case PixelFormat.ETC2_RGBA_SRgb:
                    internalFormat = InternalFormat.CompressedSrgb8Alpha8Etc2Eac;
                    format = PixelFormatGl.Rgba;
                    compressed = true;
                    pixelSize = 2;
                    type = PixelType.UnsignedByte;
                    break;
#endif
                case PixelFormat.None: // TODO: remove this - this is only for buffers used in compute shaders
                    internalFormat = InternalFormat.Rgba;
                    format = PixelFormatGl.Red;
                    type = PixelType.UnsignedByte;
                    pixelSize = 1;
                    break;
                default:
                    throw new InvalidOperationException("Unsupported texture format: " + inputFormat);
            }
        }
    }
}
 
#endif
