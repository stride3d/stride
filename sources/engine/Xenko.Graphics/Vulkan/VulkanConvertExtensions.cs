// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_GRAPHICS_API_VULKAN
using System;
using SharpVulkan;
using Xenko.Shaders;

namespace Xenko.Graphics
{
    internal static class VulkanConvertExtensions
    {
        public static PolygonMode ConvertFillMode(FillMode fillMode)
        {
            // NOTE: Vulkan's PolygonMode.Point is not exposed

            switch (fillMode)
            {
                case FillMode.Solid:
                    return PolygonMode.Fill;
                case FillMode.Wireframe:
                    return PolygonMode.Line;
                default:
                    throw new ArgumentOutOfRangeException(nameof(fillMode));
            }
        }

        public static CullModeFlags ConvertCullMode(CullMode cullMode)
        {
            // NOTE: Vulkan's CullModeFlags.FrontAndBack is not exposed

            switch (cullMode)
            {
                case CullMode.Back:
                    return CullModeFlags.Back;
                case CullMode.Front:
                    return CullModeFlags.Front;
                case CullMode.None:
                    return CullModeFlags.None;
                default:
                    throw new ArgumentOutOfRangeException(nameof(cullMode));
            }
        }

        public static PrimitiveTopology ConvertPrimitiveType(PrimitiveType primitiveType)
        {
            switch (primitiveType)
            {
                case PrimitiveType.PointList:
                    return PrimitiveTopology.PointList;
                case PrimitiveType.LineList:
                    return PrimitiveTopology.LineList;
                case PrimitiveType.LineStrip:
                    return PrimitiveTopology.LineStrip;
                case PrimitiveType.TriangleList:
                    return PrimitiveTopology.TriangleList;
                case PrimitiveType.TriangleStrip:
                    return PrimitiveTopology.TriangleStrip;
                case PrimitiveType.LineListWithAdjacency:
                    return PrimitiveTopology.LineListWithAdjacency;
                case PrimitiveType.LineStripWithAdjacency:
                    return PrimitiveTopology.LineStripWithAdjacency;
                case PrimitiveType.TriangleListWithAdjacency:
                    return PrimitiveTopology.TriangleListWithAdjacency;
                case PrimitiveType.TriangleStripWithAdjacency:
                    return PrimitiveTopology.TriangleStripWithAdjacency;
                default:
                    throw new ArgumentOutOfRangeException(nameof(primitiveType));
            }
        }

        public static ShaderStageFlags Convert(ShaderStage stage)
        {
            switch (stage)
            {
                case ShaderStage.Vertex:
                    return ShaderStageFlags.Vertex;
                case ShaderStage.Hull:
                    return ShaderStageFlags.TessellationControl;
                case ShaderStage.Domain:
                    return ShaderStageFlags.TessellationEvaluation;
                case ShaderStage.Geometry:
                    return ShaderStageFlags.Geometry;
                case ShaderStage.Pixel:
                    return ShaderStageFlags.Fragment;
                case ShaderStage.Compute:
                    return ShaderStageFlags.Compute;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static CompareOperation ConvertComparisonFunction(CompareFunction comparison)
        {
            switch (comparison)
            {
                case CompareFunction.Always:
                    return CompareOperation.Always;
                case CompareFunction.Never:
                    return CompareOperation.Never;
                case CompareFunction.Equal:
                    return CompareOperation.Equal;
                case CompareFunction.Greater:
                    return CompareOperation.Greater;
                case CompareFunction.GreaterEqual:
                    return CompareOperation.GreaterOrEqual;
                case CompareFunction.Less:
                    return CompareOperation.Less;
                case CompareFunction.LessEqual:
                    return CompareOperation.LessOrEqual;
                case CompareFunction.NotEqual:
                    return CompareOperation.NotEqual;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static SharpVulkan.StencilOperation ConvertStencilOperation(StencilOperation operation)
        {
            switch (operation)
            {
                case StencilOperation.Decrement:
                    return SharpVulkan.StencilOperation.DecrementAndWrap;
                case StencilOperation.DecrementSaturation:
                    return SharpVulkan.StencilOperation.DecrementAndClamp;
                case StencilOperation.Increment:
                    return SharpVulkan.StencilOperation.IncrementAndWrap;
                case StencilOperation.IncrementSaturation:
                    return SharpVulkan.StencilOperation.IncrementAndClamp;
                case StencilOperation.Invert:
                    return SharpVulkan.StencilOperation.Invert;
                case StencilOperation.Keep:
                    return SharpVulkan.StencilOperation.Keep;
                case StencilOperation.Replace:
                    return SharpVulkan.StencilOperation.Replace;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static BlendOperation ConvertBlendFunction(BlendFunction blendFunction)
        {
            // TODO: Binary compatible
            switch (blendFunction)
            {
                case BlendFunction.Add:
                    return BlendOperation.Add;
                case BlendFunction.Subtract:
                    return BlendOperation.Subtract;
                case BlendFunction.ReverseSubtract:
                    return BlendOperation.ReverseSubtract;
                case BlendFunction.Max:
                    return BlendOperation.Max;
                case BlendFunction.Min:
                    return BlendOperation.Min;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static BlendFactor ConvertBlend(Blend blend)
        {
            switch (blend)
            {
                case Blend.BlendFactor:
                    return BlendFactor.ConstantColor;
                case Blend.DestinationAlpha:
                    return BlendFactor.DestinationAlpha;
                case Blend.DestinationColor:
                    return BlendFactor.DestinationColor;
                case Blend.InverseBlendFactor:
                    return BlendFactor.OneMinusConstantColor;
                case Blend.InverseDestinationAlpha:
                    return BlendFactor.OneMinusDestinationAlpha;
                case Blend.InverseDestinationColor:
                    return BlendFactor.OneMinusDestinationColor;
                case Blend.InverseSecondarySourceAlpha:
                    return BlendFactor.OneMinusSource1Alpha;
                case Blend.InverseSecondarySourceColor:
                    return BlendFactor.OneMinusSource1Color;
                case Blend.InverseSourceAlpha:
                    return BlendFactor.OneMinusSourceAlpha;
                case Blend.InverseSourceColor:
                    return BlendFactor.OneMinusSourceColor;
                case Blend.One:
                    return BlendFactor.One;
                case Blend.SecondarySourceAlpha:
                    return BlendFactor.Source1Alpha;
                case Blend.SecondarySourceColor:
                    return BlendFactor.Source1Color;
                case Blend.SourceAlpha:
                    return BlendFactor.SourceAlpha;
                case Blend.SourceAlphaSaturate:
                    return BlendFactor.SourceAlphaSaturate;
                case Blend.SourceColor:
                    return BlendFactor.SourceColor;
                case Blend.Zero:
                    return BlendFactor.Zero;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static Format ConvertPixelFormat(PixelFormat inputFormat)
        {
            Format format;
            int pixelSize;
            bool compressed;

            ConvertPixelFormat(inputFormat, out format, out pixelSize, out compressed);
            return format;
        }

        public static void ConvertPixelFormat(PixelFormat inputFormat, out Format format, out int pixelSize, out bool compressed)
        {
            compressed = false;

            // TODO VULKAN: Complete supported formats
            switch (inputFormat)
            {
                //case PixelFormat.A8_UNorm:
                //    format = Format.;
                //    pixelSize = 1;
                //    break;
                case PixelFormat.R8_UNorm:
                    format = Format.R8UNorm;
                    pixelSize = 1;
                    break;
                case PixelFormat.R8_SNorm:
                    format = Format.R8SNorm;
                    pixelSize = 1;
                    break;
                case PixelFormat.R8_UInt:
                    format = Format.R8UInt;
                    pixelSize = 1;
                    break;
                case PixelFormat.R8_SInt:
                    format = Format.R8SInt;
                    pixelSize = 1;
                    break;

                case PixelFormat.R8G8B8A8_UNorm:
                    format = Format.R8G8B8A8UNorm;
                    pixelSize = 4;
                    break;
                case PixelFormat.R8G8B8A8_UInt:
                    format = Format.R8G8B8A8UInt;
                    pixelSize = 4;
                    break;
                case PixelFormat.R8G8B8A8_SInt:
                    format = Format.R8G8B8A8SInt;
                    pixelSize = 4;
                    break;
                case PixelFormat.B8G8R8A8_UNorm:
                    format = Format.B8G8R8A8UNorm;
                    pixelSize = 4;
                    break;
                case PixelFormat.R8G8B8A8_UNorm_SRgb:
                    format = Format.R8G8B8A8SRgb;
                    pixelSize = 4;
                    break;
                case PixelFormat.B8G8R8A8_UNorm_SRgb:
                    format = Format.B8G8R8A8SRgb;
                    pixelSize = 4;
                    break;

                case PixelFormat.R16_Float:
                    format = Format.R16SFloat;
                    pixelSize = 2;
                    break;
                case PixelFormat.R16_UNorm:
                    format = Format.R16UNorm;
                    pixelSize = 2;
                    break;
                case PixelFormat.R16_UInt:
                    format = Format.R16UInt;
                    pixelSize = 2;
                    break;
                case PixelFormat.R16_SInt:
                    format = Format.R16SInt;
                    pixelSize = 2;
                    break;

                case PixelFormat.R16G16_Float:
                    format = Format.R16G16SFloat;
                    pixelSize = 4;
                    break;
                case PixelFormat.R16G16_SNorm:
                    format = Format.R16G16SNorm;
                    pixelSize = 4;
                    break;
                case PixelFormat.R16G16_UNorm:
                    format = Format.R16G16UNorm;
                    pixelSize = 4;
                    break;
                case PixelFormat.R16G16_SInt:
                    format = Format.R16G16SNorm;
                    pixelSize = 4;
                    break;
                case PixelFormat.R16G16_UInt:
                    format = Format.R16G16UNorm;
                    pixelSize = 4;
                    break;

                case PixelFormat.R16G16B16A16_Float:
                    format = Format.R16G16B16A16SFloat;
                    pixelSize = 8;
                    break;
                case PixelFormat.R16G16B16A16_UNorm:
                    format = Format.R16G16B16A16UNorm;
                    pixelSize = 8;
                    break;
                case PixelFormat.R16G16B16A16_SNorm:
                    format = Format.R16G16B16A16SNorm;
                    pixelSize = 8;
                    break;
                case PixelFormat.R16G16B16A16_UInt:
                    format = Format.R16G16B16A16UInt;
                    pixelSize = 8;
                    break;
                case PixelFormat.R16G16B16A16_SInt:
                    format = Format.R16G16B16A16SInt;
                    pixelSize = 8;
                    break;

                case PixelFormat.R32_UInt:
                    format = Format.R32UInt;
                    pixelSize = 4;
                    break;
                case PixelFormat.R32_Float:
                    format = Format.R32SFloat;
                    pixelSize = 4;
                    break;

                case PixelFormat.R32G32_Float:
                    format = Format.R32G32SFloat;
                    pixelSize = 8;
                    break;
                case PixelFormat.R32G32_UInt:
                    format = Format.R32G32UInt;
                    pixelSize = 8;
                    break;
                case PixelFormat.R32G32_SInt:
                    format = Format.R32G32SInt;
                    pixelSize = 8;
                    break;

                case PixelFormat.R32G32B32_Float:
                    format = Format.R32G32B32SFloat;
                    pixelSize = 12;
                    break;
                case PixelFormat.R32G32B32_SInt:
                    format = Format.R32G32B32SInt;
                    pixelSize = 12;
                    break;
                case PixelFormat.R32G32B32_UInt:
                    format = Format.R32G32B32UInt;
                    pixelSize = 12;
                    break;

                case PixelFormat.R32G32B32A32_Float:
                    format = Format.R32G32B32A32SFloat;
                    pixelSize = 16;
                    break;
                case PixelFormat.R32G32B32A32_SInt:
                    format = Format.R32G32B32A32SInt;
                    pixelSize = 16;
                    break;
                case PixelFormat.R32G32B32A32_UInt:
                    format = Format.R32G32B32A32UInt;
                    pixelSize = 16;
                    break;

                case PixelFormat.D16_UNorm:
                    format = Format.D16UNorm;
                    pixelSize = 2;
                    break;
                case PixelFormat.D24_UNorm_S8_UInt:
                    format = Format.D24UNormS8UInt;
                    pixelSize = 4;
                    break;
                // TODO: Temporary depth format (need to decide relation between RenderTarget1D and Texture)
                case PixelFormat.D32_Float:
                    format = Format.D32SFloat;
                    pixelSize = 4;
                    break;

                case PixelFormat.ETC1:
                case PixelFormat.ETC2_RGB: // ETC1 upper compatible
                    format = Format.Etc2R8G8B8UNormBlock;
                    compressed = true;
                    pixelSize = 1; // 4bpp
                    break;
                case PixelFormat.ETC2_RGB_SRgb:
                    format = Format.Etc2R8G8B8SRgbBlock;
                    compressed = true;
                    pixelSize = 1;
                    break;
                case PixelFormat.ETC2_RGB_A1:
                    format = Format.Etc2R8G8B8A1UNormBlock;
                    compressed = true;
                    pixelSize = 1; // 4bpp
                    break;
                case PixelFormat.ETC2_RGBA: // ETC2 + EAC
                    format = Format.Etc2R8G8B8A8UNormBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.ETC2_RGBA_SRgb: // ETC2 + EAC
                    format = Format.Etc2R8G8B8A8SRgbBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.EAC_R11_Unsigned:
                    format = Format.EacR11UNormBlock;
                    compressed = true;
                    pixelSize = 1; // 4bpp
                    break;
                case PixelFormat.EAC_R11_Signed:
                    format = Format.EacR11SNormBlock;
                    compressed = true;
                    pixelSize = 1; // 4bpp
                    break;
                case PixelFormat.EAC_RG11_Unsigned:
                    format = Format.EacR11G11UNormBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.EAC_RG11_Signed:
                    format = Format.EacR11G11SNormBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;

                case PixelFormat.BC1_UNorm:
                    format = Format.Bc1RgbaUNormBlock;
                    //format = Format.RAD_TEXTURE_FORMAT_DXT1_RGBA;
                    compressed = true;
                    pixelSize = 1; // 4bpp
                    break;
                case PixelFormat.BC1_UNorm_SRgb:
                    format = Format.Bc1RgbaSRgbBlock;
                    //format = Format.RAD_TEXTURE_FORMAT_DXT1_RGBA_SRgb;
                    compressed = true;
                    pixelSize = 1; // 4bpp
                    break;
                case PixelFormat.BC2_UNorm:
                    format = Format.Bc2UNormBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC2_UNorm_SRgb:
                    format = Format.Bc2SRgbBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC3_UNorm:
                    format = Format.Bc3UNormBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC3_UNorm_SRgb:
                    format = Format.Bc3SRgbBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC4_SNorm:
                    format = Format.Bc4SNormBlock;
                    compressed = true;
                    pixelSize = 1; // 4bpp
                    break;
                case PixelFormat.BC4_UNorm:
                    format = Format.Bc4UNormBlock;
                    compressed = true;
                    pixelSize = 1; // 4bpp
                    break;
                case PixelFormat.BC5_SNorm:
                    format = Format.Bc5SNormBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC5_UNorm:
                    format = Format.Bc5UNormBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC6H_Sf16:
                    format = Format.Bc6HSFloatBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC6H_Uf16:
                    format = Format.Bc6HUFloatBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC7_UNorm:
                    format = Format.Bc7UNormBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC7_UNorm_SRgb:
                    format = Format.Bc7SRgbBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                default:
                    throw new InvalidOperationException("Unsupported texture format: " + inputFormat);
            }
        }

        public static unsafe ColorComponentFlags ConvertColorWriteChannels(ColorWriteChannels colorWriteChannels)
        {
            return *(ColorComponentFlags*)&colorWriteChannels;
        }

        public static DescriptorType ConvertDescriptorType(EffectParameterClass @class, EffectParameterType type)
        {
            switch (@class)
            {
                case EffectParameterClass.ConstantBuffer:
                    return DescriptorType.UniformBuffer;
                case EffectParameterClass.Sampler:
                    return DescriptorType.Sampler;
                case EffectParameterClass.ShaderResourceView:
                    switch (type)
                    {
                        case EffectParameterType.Texture:
                        case EffectParameterType.Texture1D:
                        case EffectParameterType.Texture2D:
                        case EffectParameterType.Texture3D:
                        case EffectParameterType.TextureCube:
                        case EffectParameterType.Texture1DArray:
                        case EffectParameterType.Texture2DArray:
                        case EffectParameterType.TextureCubeArray:
                            return DescriptorType.SampledImage;

                        case EffectParameterType.Buffer:
                            return DescriptorType.UniformTexelBuffer;

                        default:
                            throw new NotImplementedException();
                    }
                case EffectParameterClass.UnorderedAccessView:
                    return DescriptorType.StorageBuffer;
                default:
                    throw new NotImplementedException();
            }
        }

        public static int BlockSizeInBytes(this Format format)
        {
            switch (format)
            {
                case Format.Bc1RgbUNormBlock:
                case Format.Bc1RgbSRgbBlock:
                case Format.Bc1RgbaUNormBlock:
                case Format.Bc1RgbaSRgbBlock:
                    return 8;

                case Format.Bc2UNormBlock:
                case Format.Bc2SRgbBlock:
                case Format.Bc3UNormBlock:
                case Format.Bc3SRgbBlock:
                    return 16;

                case Format.Bc4UNormBlock:
                case Format.Bc4SNormBlock:
                    return 8;

                case Format.Bc5UNormBlock:
                case Format.Bc5SNormBlock:
                case Format.Bc6HUFloatBlock:
                case Format.Bc6HSFloatBlock:
                case Format.Bc7UNormBlock:
                case Format.Bc7SRgbBlock:
                    return 16;

                case Format.Etc2R8G8B8UNormBlock:
                case Format.Etc2R8G8B8SRgbBlock:
                case Format.Etc2R8G8B8A1UNormBlock:
                case Format.Etc2R8G8B8A1SRgbBlock:
                    return 8;

                case Format.Etc2R8G8B8A8UNormBlock:
                case Format.Etc2R8G8B8A8SRgbBlock:
                    return 16;

                case Format.EacR11UNormBlock:
                case Format.EacR11SNormBlock:
                    return 8;

                case Format.EacR11G11UNormBlock:
                case Format.EacR11G11SNormBlock:
                    return 16;

                case Format.Astc4X4UNormBlock:
                case Format.Astc4X4SRgbBlock:
                case Format.Astc5X4UNormBlock:
                case Format.Astc5X4SRgbBlock:
                case Format.Astc5X5UNormBlock:
                case Format.Astc5X5SRgbBlock:
                case Format.Astc6X5UNormBlock:
                case Format.Astc6X5SRgbBlock:
                case Format.Astc6X6UNormBlock:
                case Format.Astc6X6SRgbBlock:
                case Format.Astc8X5UNormBlock:
                case Format.Astc8X5SRgbBlock:
                case Format.Astc8X6UNormBlock:
                case Format.Astc8X6SRgbBlock:
                case Format.Astc8X8UNormBlock:
                case Format.Astc8X8SRgbBlock:
                case Format.Astc10X5UNormBlock:
                case Format.Astc10X5SRgbBlock:
                case Format.Astc10X6UNormBlock:
                case Format.Astc10X6SRgbBlock:
                case Format.Astc10X8UNormBlock:
                case Format.Astc10X8SRgbBlock:
                case Format.Astc10X10UNormBlock:
                case Format.Astc10X10SRgbBlock:
                case Format.Astc12X10UNormBlock:
                case Format.Astc12X10SRgbBlock:
                case Format.Astc12X12UNormBlock:
                case Format.Astc12X12SRgbBlock:
                    return 16;

                //case Format.Pvrtc12BppUNormBlock:
                //case Format.Pvrtc14BppUNormBlock:
                //case Format.Pvrtc22BppUNormBlock:
                //case Format.Pvrtc24BppUNormBlock:
                //case Format.Pvrtc12BppSRgbBlock:
                //case Format.Pvrtc14BppSRgbBlock:
                //case Format.Pvrtc22BppSRgbBlock:
                //case Format.Pvrtc24BppSRgbBlock:
                //    return 8;

                default:
                    throw new ArgumentOutOfRangeException(nameof(format));
            }
        }
    }
}

#endif
