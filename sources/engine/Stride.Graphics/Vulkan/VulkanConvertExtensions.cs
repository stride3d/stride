// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
using Stride.Shaders;

namespace Stride.Graphics
{
    internal static class VulkanConvertExtensions
    {
        public static VkPolygonMode ConvertFillMode(FillMode fillMode)
        {
            // NOTE: Vulkan's PolygonMode.Point is not exposed

            switch (fillMode)
            {
                case FillMode.Solid:
                    return VkPolygonMode.Fill;
                case FillMode.Wireframe:
                    return VkPolygonMode.Line;
                default:
                    throw new ArgumentOutOfRangeException(nameof(fillMode));
            }
        }

        public static VkCullModeFlags ConvertCullMode(CullMode cullMode)
        {
            // NOTE: Vulkan's VkCullModeFlags.FrontAndBack is not exposed

            switch (cullMode)
            {
                case CullMode.Back:
                    return VkCullModeFlags.Back;
                case CullMode.Front:
                    return VkCullModeFlags.Front;
                case CullMode.None:
                    return VkCullModeFlags.None;
                default:
                    throw new ArgumentOutOfRangeException(nameof(cullMode));
            }
        }

        public static VkPrimitiveTopology ConvertPrimitiveType(PrimitiveType primitiveType)
        {
            switch (primitiveType)
            {
                case PrimitiveType.PointList:
                    return VkPrimitiveTopology.PointList;
                case PrimitiveType.LineList:
                    return VkPrimitiveTopology.LineList;
                case PrimitiveType.LineStrip:
                    return VkPrimitiveTopology.LineStrip;
                case PrimitiveType.TriangleList:
                    return VkPrimitiveTopology.TriangleList;
                case PrimitiveType.TriangleStrip:
                    return VkPrimitiveTopology.TriangleStrip;
                case PrimitiveType.LineListWithAdjacency:
                    return VkPrimitiveTopology.LineListWithAdjacency;
                case PrimitiveType.LineStripWithAdjacency:
                    return VkPrimitiveTopology.LineStripWithAdjacency;
                case PrimitiveType.TriangleListWithAdjacency:
                    return VkPrimitiveTopology.TriangleListWithAdjacency;
                case PrimitiveType.TriangleStripWithAdjacency:
                    return VkPrimitiveTopology.TriangleStripWithAdjacency;
                default:
                    throw new ArgumentOutOfRangeException(nameof(primitiveType));
            }
        }

        public static bool ConvertPrimitiveRestart(PrimitiveType primitiveType)
        {
            switch (primitiveType)
            {
                case PrimitiveType.PointList:
                case PrimitiveType.LineList:
                case PrimitiveType.TriangleList:
                case PrimitiveType.LineListWithAdjacency:
                case PrimitiveType.TriangleListWithAdjacency:
                case PrimitiveType.PatchList:
                    return false;
                default:
                    return true;
            }
        }

        public static VkShaderStageFlags Convert(ShaderStage stage)
        {
            switch (stage)
            {
                case ShaderStage.Vertex:
                    return VkShaderStageFlags.Vertex;
                case ShaderStage.Hull:
                    return VkShaderStageFlags.TessellationControl;
                case ShaderStage.Domain:
                    return VkShaderStageFlags.TessellationEvaluation;
                case ShaderStage.Geometry:
                    return VkShaderStageFlags.Geometry;
                case ShaderStage.Pixel:
                    return VkShaderStageFlags.Fragment;
                case ShaderStage.Compute:
                    return VkShaderStageFlags.Compute;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static VkCompareOp ConvertComparisonFunction(CompareFunction comparison)
        {
            switch (comparison)
            {
                case CompareFunction.Always:
                    return VkCompareOp.Always;
                case CompareFunction.Never:
                    return VkCompareOp.Never;
                case CompareFunction.Equal:
                    return VkCompareOp.Equal;
                case CompareFunction.Greater:
                    return VkCompareOp.Greater;
                case CompareFunction.GreaterEqual:
                    return VkCompareOp.GreaterOrEqual;
                case CompareFunction.Less:
                    return VkCompareOp.Less;
                case CompareFunction.LessEqual:
                    return VkCompareOp.LessOrEqual;
                case CompareFunction.NotEqual:
                    return VkCompareOp.NotEqual;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static VkStencilOp ConvertStencilOperation(StencilOperation operation)
        {
            switch (operation)
            {
                case StencilOperation.Decrement:
                    return VkStencilOp.DecrementAndWrap;
                case StencilOperation.DecrementSaturation:
                    return VkStencilOp.DecrementAndClamp;
                case StencilOperation.Increment:
                    return VkStencilOp.IncrementAndWrap;
                case StencilOperation.IncrementSaturation:
                    return VkStencilOp.IncrementAndClamp;
                case StencilOperation.Invert:
                    return VkStencilOp.Invert;
                case StencilOperation.Keep:
                    return VkStencilOp.Keep;
                case StencilOperation.Replace:
                    return VkStencilOp.Replace;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static VkBlendOp ConvertBlendFunction(BlendFunction blendFunction)
        {
            // TODO: Binary compatible
            switch (blendFunction)
            {
                case BlendFunction.Add:
                    return VkBlendOp.Add;
                case BlendFunction.Subtract:
                    return VkBlendOp.Subtract;
                case BlendFunction.ReverseSubtract:
                    return VkBlendOp.ReverseSubtract;
                case BlendFunction.Max:
                    return VkBlendOp.Max;
                case BlendFunction.Min:
                    return VkBlendOp.Min;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static VkBlendFactor ConvertBlend(Blend blend)
        {
            switch (blend)
            {
                case Blend.BlendFactor:
                    return VkBlendFactor.ConstantColor;
                case Blend.DestinationAlpha:
                    return VkBlendFactor.DstAlpha;
                case Blend.DestinationColor:
                    return VkBlendFactor.DstColor;
                case Blend.InverseBlendFactor:
                    return VkBlendFactor.OneMinusConstantColor;
                case Blend.InverseDestinationAlpha:
                    return VkBlendFactor.OneMinusDstAlpha;
                case Blend.InverseDestinationColor:
                    return VkBlendFactor.OneMinusDstColor;
                case Blend.InverseSecondarySourceAlpha:
                    return VkBlendFactor.OneMinusSrc1Alpha;
                case Blend.InverseSecondarySourceColor:
                    return VkBlendFactor.OneMinusSrc1Color;
                case Blend.InverseSourceAlpha:
                    return VkBlendFactor.OneMinusSrcAlpha;
                case Blend.InverseSourceColor:
                    return VkBlendFactor.OneMinusSrcColor;
                case Blend.One:
                    return VkBlendFactor.One;
                case Blend.SecondarySourceAlpha:
                    return VkBlendFactor.Src1Alpha;
                case Blend.SecondarySourceColor:
                    return VkBlendFactor.Src1Color;
                case Blend.SourceAlpha:
                    return VkBlendFactor.SrcAlpha;
                case Blend.SourceAlphaSaturate:
                    return VkBlendFactor.SrcAlphaSaturate;
                case Blend.SourceColor:
                    return VkBlendFactor.SrcColor;
                case Blend.Zero:
                    return VkBlendFactor.Zero;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static VkFormat ConvertPixelFormat(PixelFormat inputFormat)
        {
            ConvertPixelFormat(inputFormat, out var format, out _, out _);
            return format;
        }

        public static void ConvertPixelFormat(PixelFormat inputFormat, out VkFormat format, out int pixelSize, out bool compressed)
        {
            compressed = false;

            // TODO VULKAN: Complete supported formats
            switch (inputFormat)
            {
                //case PixelFormat.A8_UNorm:
                //    format = VkFormat.;
                //    pixelSize = 1;
                //    break;
                case PixelFormat.R8_UNorm:
                    format = VkFormat.R8UNorm;
                    pixelSize = 1;
                    break;
                case PixelFormat.R8_SNorm:
                    format = VkFormat.R8SNorm;
                    pixelSize = 1;
                    break;
                case PixelFormat.R8_UInt:
                    format = VkFormat.R8UInt;
                    pixelSize = 1;
                    break;
                case PixelFormat.R8_SInt:
                    format = VkFormat.R8SInt;
                    pixelSize = 1;
                    break;

                case PixelFormat.R8G8B8A8_UNorm:
                    format = VkFormat.R8G8B8A8UNorm;
                    pixelSize = 4;
                    break;
                case PixelFormat.R8G8B8A8_UInt:
                    format = VkFormat.R8G8B8A8UInt;
                    pixelSize = 4;
                    break;
                case PixelFormat.R8G8B8A8_SInt:
                    format = VkFormat.R8G8B8A8SInt;
                    pixelSize = 4;
                    break;
                case PixelFormat.B8G8R8A8_UNorm:
                    format = VkFormat.B8G8R8A8UNorm;
                    pixelSize = 4;
                    break;
                case PixelFormat.R8G8B8A8_UNorm_SRgb:
                    format = VkFormat.R8G8B8A8SRgb;
                    pixelSize = 4;
                    break;
                case PixelFormat.B8G8R8A8_UNorm_SRgb:
                    format = VkFormat.B8G8R8A8SRgb;
                    pixelSize = 4;
                    break;

                case PixelFormat.R16_Float:
                    format = VkFormat.R16SFloat;
                    pixelSize = 2;
                    break;
                case PixelFormat.R16_UNorm:
                    format = VkFormat.R16UNorm;
                    pixelSize = 2;
                    break;
                case PixelFormat.R16_UInt:
                    format = VkFormat.R16UInt;
                    pixelSize = 2;
                    break;
                case PixelFormat.R16_SInt:
                    format = VkFormat.R16SInt;
                    pixelSize = 2;
                    break;

                case PixelFormat.R16G16_Float:
                    format = VkFormat.R16G16SFloat;
                    pixelSize = 4;
                    break;
                case PixelFormat.R16G16_SNorm:
                    format = VkFormat.R16G16SNorm;
                    pixelSize = 4;
                    break;
                case PixelFormat.R16G16_UNorm:
                    format = VkFormat.R16G16UNorm;
                    pixelSize = 4;
                    break;
                case PixelFormat.R16G16_SInt:
                    format = VkFormat.R16G16SNorm;
                    pixelSize = 4;
                    break;
                case PixelFormat.R16G16_UInt:
                    format = VkFormat.R16G16UNorm;
                    pixelSize = 4;
                    break;

                case PixelFormat.R16G16B16A16_Float:
                    format = VkFormat.R16G16B16A16SFloat;
                    pixelSize = 8;
                    break;
                case PixelFormat.R16G16B16A16_UNorm:
                    format = VkFormat.R16G16B16A16UNorm;
                    pixelSize = 8;
                    break;
                case PixelFormat.R16G16B16A16_SNorm:
                    format = VkFormat.R16G16B16A16SNorm;
                    pixelSize = 8;
                    break;
                case PixelFormat.R16G16B16A16_UInt:
                    format = VkFormat.R16G16B16A16UInt;
                    pixelSize = 8;
                    break;
                case PixelFormat.R16G16B16A16_SInt:
                    format = VkFormat.R16G16B16A16SInt;
                    pixelSize = 8;
                    break;

                case PixelFormat.R32_UInt:
                    format = VkFormat.R32UInt;
                    pixelSize = 4;
                    break;
                case PixelFormat.R32_Float:
                    format = VkFormat.R32SFloat;
                    pixelSize = 4;
                    break;

                case PixelFormat.R32G32_Float:
                    format = VkFormat.R32G32SFloat;
                    pixelSize = 8;
                    break;
                case PixelFormat.R32G32_UInt:
                    format = VkFormat.R32G32UInt;
                    pixelSize = 8;
                    break;
                case PixelFormat.R32G32_SInt:
                    format = VkFormat.R32G32SInt;
                    pixelSize = 8;
                    break;

                case PixelFormat.R32G32B32_Float:
                    format = VkFormat.R32G32B32SFloat;
                    pixelSize = 12;
                    break;
                case PixelFormat.R32G32B32_SInt:
                    format = VkFormat.R32G32B32SInt;
                    pixelSize = 12;
                    break;
                case PixelFormat.R32G32B32_UInt:
                    format = VkFormat.R32G32B32UInt;
                    pixelSize = 12;
                    break;

                case PixelFormat.R32G32B32A32_Float:
                    format = VkFormat.R32G32B32A32SFloat;
                    pixelSize = 16;
                    break;
                case PixelFormat.R32G32B32A32_SInt:
                    format = VkFormat.R32G32B32A32SInt;
                    pixelSize = 16;
                    break;
                case PixelFormat.R32G32B32A32_UInt:
                    format = VkFormat.R32G32B32A32UInt;
                    pixelSize = 16;
                    break;

                case PixelFormat.D16_UNorm:
                    format = VkFormat.D16UNorm;
                    pixelSize = 2;
                    break;
                case PixelFormat.D24_UNorm_S8_UInt:
                    format = VkFormat.D24UNormS8UInt;
                    pixelSize = 4;
                    break;
                // TODO: Temporary depth format (need to decide relation between RenderTarget1D and Texture)
                case PixelFormat.D32_Float:
                    format = VkFormat.D32SFloat;
                    pixelSize = 4;
                    break;

                case PixelFormat.ETC1:
                case PixelFormat.ETC2_RGB: // ETC1 upper compatible
                    format = VkFormat.ETC2R8G8B8UNormBlock;
                    compressed = true;
                    pixelSize = 1; // 4bpp
                    break;
                case PixelFormat.ETC2_RGB_SRgb:
                    format = VkFormat.ETC2R8G8B8SRgbBlock;
                    compressed = true;
                    pixelSize = 1;
                    break;
                case PixelFormat.ETC2_RGB_A1:
                    format = VkFormat.ETC2R8G8B8A1UNormBlock;
                    compressed = true;
                    pixelSize = 1; // 4bpp
                    break;
                case PixelFormat.ETC2_RGBA: // ETC2 + EAC
                    format = VkFormat.ETC2R8G8B8A8UNormBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.ETC2_RGBA_SRgb: // ETC2 + EAC
                    format = VkFormat.ETC2R8G8B8A8SRgbBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.EAC_R11_Unsigned:
                    format = VkFormat.EACR11UNormBlock;
                    compressed = true;
                    pixelSize = 1; // 4bpp
                    break;
                case PixelFormat.EAC_R11_Signed:
                    format = VkFormat.EACR11SNormBlock;
                    compressed = true;
                    pixelSize = 1; // 4bpp
                    break;
                case PixelFormat.EAC_RG11_Unsigned:
                    format = VkFormat.EACR11G11UNormBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.EAC_RG11_Signed:
                    format = VkFormat.EACR11G11SNormBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;

                case PixelFormat.BC1_UNorm:
                    format = VkFormat.BC1RGBAUNormBlock;
                    //format = VkFormat.RAD_TEXTURE_FORMAT_DXT1_RGBA;
                    compressed = true;
                    pixelSize = 1; // 4bpp
                    break;
                case PixelFormat.BC1_UNorm_SRgb:
                    format = VkFormat.BC1RGBASRgbBlock;
                    //format = VkFormat.RAD_TEXTURE_FORMAT_DXT1_RGBA_SRgb;
                    compressed = true;
                    pixelSize = 1; // 4bpp
                    break;
                case PixelFormat.BC2_UNorm:
                    format = VkFormat.BC2UNormBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC2_UNorm_SRgb:
                    format = VkFormat.BC2SRgbBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC3_UNorm:
                    format = VkFormat.BC3UNormBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC3_UNorm_SRgb:
                    format = VkFormat.BC3SRgbBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC4_SNorm:
                    format = VkFormat.BC4SNormBlock;
                    compressed = true;
                    pixelSize = 1; // 4bpp
                    break;
                case PixelFormat.BC4_UNorm:
                    format = VkFormat.BC4UNormBlock;
                    compressed = true;
                    pixelSize = 1; // 4bpp
                    break;
                case PixelFormat.BC5_SNorm:
                    format = VkFormat.BC5SNormBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC5_UNorm:
                    format = VkFormat.BC5UNormBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC6H_Sf16:
                    format = VkFormat.BC6HSFloatBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC6H_Uf16:
                    format = VkFormat.BC6HUFloatBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC7_UNorm:
                    format = VkFormat.BC7UNormBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC7_UNorm_SRgb:
                    format = VkFormat.BC7SRgbBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                default:
                    throw new InvalidOperationException("Unsupported texture format: " + inputFormat);
            }
        }

        public static unsafe VkColorComponentFlags ConvertColorWriteChannels(ColorWriteChannels colorWriteChannels)
        {
            return *(VkColorComponentFlags*)&colorWriteChannels;
        }

        public static VkDescriptorType ConvertDescriptorType(EffectParameterClass @class, EffectParameterType type)
        {
            switch (@class)
            {
                case EffectParameterClass.ConstantBuffer:
                    return VkDescriptorType.UniformBuffer;
                case EffectParameterClass.Sampler:
                    return VkDescriptorType.Sampler;
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
                            return VkDescriptorType.SampledImage;

                        case EffectParameterType.Buffer:
                            return VkDescriptorType.UniformTexelBuffer;

                        default:
                            throw new NotImplementedException();
                    }
                case EffectParameterClass.UnorderedAccessView:
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
                            return VkDescriptorType.StorageImage;

                        case EffectParameterType.Buffer:
                            return VkDescriptorType.StorageBuffer;

                        default:
                            throw new NotImplementedException();
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        public static int BlockSizeInBytes(this VkFormat format)
        {
            switch (format)
            {
                case VkFormat.BC1RGBUNormBlock:
                case VkFormat.BC1RGBSRgbBlock:
                case VkFormat.BC1RGBAUNormBlock:
                case VkFormat.BC1RGBASRgbBlock:
                    return 8;

                case VkFormat.BC2UNormBlock:
                case VkFormat.BC2SRgbBlock:
                case VkFormat.BC3UNormBlock:
                case VkFormat.BC3SRgbBlock:
                    return 16;

                case VkFormat.BC4UNormBlock:
                case VkFormat.BC4SNormBlock:
                    return 8;

                case VkFormat.BC5UNormBlock:
                case VkFormat.BC5SNormBlock:
                case VkFormat.BC6HUFloatBlock:
                case VkFormat.BC6HSFloatBlock:
                case VkFormat.BC7UNormBlock:
                case VkFormat.BC7SRgbBlock:
                    return 16;

                case VkFormat.ETC2R8G8B8UNormBlock:
                case VkFormat.ETC2R8G8B8SRgbBlock:
                case VkFormat.ETC2R8G8B8A1UNormBlock:
                case VkFormat.ETC2R8G8B8A1SRgbBlock:
                    return 8;

                case VkFormat.ETC2R8G8B8A8UNormBlock:
                case VkFormat.ETC2R8G8B8A8SRgbBlock:
                    return 16;

                case VkFormat.EACR11UNormBlock:
                case VkFormat.EACR11SNormBlock:
                    return 8;

                case VkFormat.EACR11G11UNormBlock:
                case VkFormat.EACR11G11SNormBlock:
                    return 16;

                case VkFormat.ASTC4x4UNormBlock:
                case VkFormat.ASTC4x4SRgbBlock:
                case VkFormat.ASTC5x4UNormBlock:
                case VkFormat.ASTC5x4SRgbBlock:
                case VkFormat.ASTC5x5UNormBlock:
                case VkFormat.ASTC5x5SRgbBlock:
                case VkFormat.ASTC6x5UNormBlock:
                case VkFormat.ASTC6x5SRgbBlock:
                case VkFormat.ASTC6x6UNormBlock:
                case VkFormat.ASTC6x6SRgbBlock:
                case VkFormat.ASTC8x5UNormBlock:
                case VkFormat.ASTC8x5SRgbBlock:
                case VkFormat.ASTC8x6UNormBlock:
                case VkFormat.ASTC8x6SRgbBlock:
                case VkFormat.ASTC8x8UNormBlock:
                case VkFormat.ASTC8x8SRgbBlock:
                case VkFormat.ASTC10x5UNormBlock:
                case VkFormat.ASTC10x5SRgbBlock:
                case VkFormat.ASTC10x6UNormBlock:
                case VkFormat.ASTC10x6SRgbBlock:
                case VkFormat.ASTC10x8UNormBlock:
                case VkFormat.ASTC10x8SRgbBlock:
                case VkFormat.ASTC10x10UNormBlock:
                case VkFormat.ASTC10x10SRgbBlock:
                case VkFormat.ASTC12x10UNormBlock:
                case VkFormat.ASTC12x10SRgbBlock:
                case VkFormat.ASTC12x12UNormBlock:
                case VkFormat.ASTC12x12SRgbBlock:
                    return 16;

                //case VkFormat.Pvrtc12BppUNormBlock:
                //case VkFormat.Pvrtc14BppUNormBlock:
                //case VkFormat.Pvrtc22BppUNormBlock:
                //case VkFormat.Pvrtc24BppUNormBlock:
                //case VkFormat.Pvrtc12BppSRgbBlock:
                //case VkFormat.Pvrtc14BppSRgbBlock:
                //case VkFormat.Pvrtc22BppSRgbBlock:
                //case VkFormat.Pvrtc24BppSRgbBlock:
                //    return 8;

                default:
                    throw new ArgumentOutOfRangeException(nameof(format));
            }
        }
    }
}

#endif
