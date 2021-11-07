// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using Silk.NET.Vulkan;
using static Silk.NET.Vulkan.Vk;
using Stride.Shaders;

namespace Stride.Graphics
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
                    return CullModeFlags.CullModeBackBit;
                case CullMode.Front:
                    return CullModeFlags.CullModeFrontBit;
                case CullMode.None:
                    return CullModeFlags.CullModeNone;
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

        public static ShaderStageFlags Convert(ShaderStage stage)
        {
            switch (stage)
            {
                case ShaderStage.Vertex:
                    return ShaderStageFlags.ShaderStageVertexBit;
                case ShaderStage.Hull:
                    return ShaderStageFlags.ShaderStageTessellationControlBit;
                case ShaderStage.Domain:
                    return ShaderStageFlags.ShaderStageTessellationEvaluationBit;
                case ShaderStage.Geometry:
                    return ShaderStageFlags.ShaderStageGeometryBit;
                case ShaderStage.Pixel:
                    return ShaderStageFlags.ShaderStageFragmentBit;
                case ShaderStage.Compute:
                    return ShaderStageFlags.ShaderStageComputeBit;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static CompareOp ConvertComparisonFunction(CompareFunction comparison)
        {
            switch (comparison)
            {
                case CompareFunction.Always:
                    return CompareOp.Always;
                case CompareFunction.Never:
                    return CompareOp.Never;
                case CompareFunction.Equal:
                    return CompareOp.Equal;
                case CompareFunction.Greater:
                    return CompareOp.Greater;
                case CompareFunction.GreaterEqual:
                    return CompareOp.GreaterOrEqual;
                case CompareFunction.Less:
                    return CompareOp.Less;
                case CompareFunction.LessEqual:
                    return CompareOp.LessOrEqual;
                case CompareFunction.NotEqual:
                    return CompareOp.NotEqual;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static StencilOp ConvertStencilOperation(StencilOperation operation)
        {
            switch (operation)
            {
                case StencilOperation.Decrement:
                    return StencilOp.DecrementAndWrap;
                case StencilOperation.DecrementSaturation:
                    return StencilOp.DecrementAndClamp;
                case StencilOperation.Increment:
                    return StencilOp.IncrementAndWrap;
                case StencilOperation.IncrementSaturation:
                    return StencilOp.IncrementAndClamp;
                case StencilOperation.Invert:
                    return StencilOp.Invert;
                case StencilOperation.Keep:
                    return StencilOp.Keep;
                case StencilOperation.Replace:
                    return StencilOp.Replace;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static BlendOp ConvertBlendFunction(BlendFunction blendFunction)
        {
            // TODO: Binary compatible
            switch (blendFunction)
            {
                case BlendFunction.Add:
                    return BlendOp.Add;
                case BlendFunction.Subtract:
                    return BlendOp.Subtract;
                case BlendFunction.ReverseSubtract:
                    return BlendOp.ReverseSubtract;
                case BlendFunction.Max:
                    return BlendOp.Max;
                case BlendFunction.Min:
                    return BlendOp.Min;
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
                    return BlendFactor.DstAlpha;
                case Blend.DestinationColor:
                    return BlendFactor.DstColor;
                case Blend.InverseBlendFactor:
                    return BlendFactor.OneMinusConstantColor;
                case Blend.InverseDestinationAlpha:
                    return BlendFactor.OneMinusDstAlpha;
                case Blend.InverseDestinationColor:
                    return BlendFactor.OneMinusDstColor;
                case Blend.InverseSecondarySourceAlpha:
                    return BlendFactor.OneMinusSrc1Alpha;
                case Blend.InverseSecondarySourceColor:
                    return BlendFactor.OneMinusSrc1Color;
                case Blend.InverseSourceAlpha:
                    return BlendFactor.OneMinusSrcAlpha;
                case Blend.InverseSourceColor:
                    return BlendFactor.OneMinusSrcColor;
                case Blend.One:
                    return BlendFactor.One;
                case Blend.SecondarySourceAlpha:
                    return BlendFactor.Src1Alpha;
                case Blend.SecondarySourceColor:
                    return BlendFactor.Src1Color;
                case Blend.SourceAlpha:
                    return BlendFactor.SrcAlpha;
                case Blend.SourceAlphaSaturate:
                    return BlendFactor.SrcAlphaSaturate;
                case Blend.SourceColor:
                    return BlendFactor.SrcColor;
                case Blend.Zero:
                    return BlendFactor.Zero;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static Format ConvertPixelFormat(PixelFormat inputFormat)
        {
            ConvertPixelFormat(inputFormat, out var format, out _, out _);
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
                    format = Format.R8Unorm;
                    pixelSize = 1;
                    break;
                case PixelFormat.R8_SNorm:
                    format = Format.R8SNorm;
                    pixelSize = 1;
                    break;
                case PixelFormat.R8_UInt:
                    format = Format.R8Uint;
                    pixelSize = 1;
                    break;
                case PixelFormat.R8_SInt:
                    format = Format.R8Sint;
                    pixelSize = 1;
                    break;

                case PixelFormat.R8G8B8A8_UNorm:
                    format = Format.R8G8B8A8Unorm;
                    pixelSize = 4;
                    break;
                case PixelFormat.R8G8B8A8_UInt:
                    format = Format.R8G8B8A8Uint;
                    pixelSize = 4;
                    break;
                case PixelFormat.R8G8B8A8_SInt:
                    format = Format.R8G8B8A8Sint;
                    pixelSize = 4;
                    break;
                case PixelFormat.B8G8R8A8_UNorm:
                    format = Format.B8G8R8A8Unorm;
                    pixelSize = 4;
                    break;
                case PixelFormat.R8G8B8A8_UNorm_SRgb:
                    format = Format.R8G8B8A8Srgb;
                    pixelSize = 4;
                    break;
                case PixelFormat.B8G8R8A8_UNorm_SRgb:
                    format = Format.B8G8R8A8Srgb;
                    pixelSize = 4;
                    break;

                case PixelFormat.R16_Float:
                    format = Format.R16Sfloat;
                    pixelSize = 2;
                    break;
                case PixelFormat.R16_UNorm:
                    format = Format.R16Unorm;
                    pixelSize = 2;
                    break;
                case PixelFormat.R16_UInt:
                    format = Format.R16Uint;
                    pixelSize = 2;
                    break;
                case PixelFormat.R16_SInt:
                    format = Format.R16Sint;
                    pixelSize = 2;
                    break;

                case PixelFormat.R16G16_Float:
                    format = Format.R16G16Sfloat;
                    pixelSize = 4;
                    break;
                case PixelFormat.R16G16_SNorm:
                    format = Format.R16G16SNorm;
                    pixelSize = 4;
                    break;
                case PixelFormat.R16G16_UNorm:
                    format = Format.R16G16Unorm;
                    pixelSize = 4;
                    break;
                case PixelFormat.R16G16_SInt:
                    format = Format.R16G16SNorm;
                    pixelSize = 4;
                    break;
                case PixelFormat.R16G16_UInt:
                    format = Format.R16G16Unorm;
                    pixelSize = 4;
                    break;

                case PixelFormat.R16G16B16A16_Float:
                    format = Format.R16G16B16A16Sfloat;
                    pixelSize = 8;
                    break;
                case PixelFormat.R16G16B16A16_UNorm:
                    format = Format.R16G16B16A16Unorm;
                    pixelSize = 8;
                    break;
                case PixelFormat.R16G16B16A16_SNorm:
                    format = Format.R16G16B16A16SNorm;
                    pixelSize = 8;
                    break;
                case PixelFormat.R16G16B16A16_UInt:
                    format = Format.R16G16B16A16Uint;
                    pixelSize = 8;
                    break;
                case PixelFormat.R16G16B16A16_SInt:
                    format = Format.R16G16B16A16Sint;
                    pixelSize = 8;
                    break;

                case PixelFormat.R32_UInt:
                    format = Format.R32Uint;
                    pixelSize = 4;
                    break;
                case PixelFormat.R32_Float:
                    format = Format.R32Sfloat;
                    pixelSize = 4;
                    break;

                case PixelFormat.R32G32_Float:
                    format = Format.R32G32Sfloat;
                    pixelSize = 8;
                    break;
                case PixelFormat.R32G32_UInt:
                    format = Format.R32G32Uint;
                    pixelSize = 8;
                    break;
                case PixelFormat.R32G32_SInt:
                    format = Format.R32G32Sint;
                    pixelSize = 8;
                    break;

                case PixelFormat.R32G32B32_Float:
                    format = Format.R32G32B32Sfloat;
                    pixelSize = 12;
                    break;
                case PixelFormat.R32G32B32_SInt:
                    format = Format.R32G32B32Sint;
                    pixelSize = 12;
                    break;
                case PixelFormat.R32G32B32_UInt:
                    format = Format.R32G32B32Uint;
                    pixelSize = 12;
                    break;

                case PixelFormat.R32G32B32A32_Float:
                    format = Format.R32G32B32A32Sfloat;
                    pixelSize = 16;
                    break;
                case PixelFormat.R32G32B32A32_SInt:
                    format = Format.R32G32B32A32Sint;
                    pixelSize = 16;
                    break;
                case PixelFormat.R32G32B32A32_UInt:
                    format = Format.R32G32B32A32Uint;
                    pixelSize = 16;
                    break;

                case PixelFormat.D16_UNorm:
                    format = Format.D16Unorm;
                    pixelSize = 2;
                    break;
                case PixelFormat.D24_UNorm_S8_UInt:
                    format = Format.D24UnormS8Uint;
                    pixelSize = 4;
                    break;
                // TODO: Temporary depth format (need to decide relation between RenderTarget1D and Texture)
                case PixelFormat.D32_Float:
                    format = Format.D32Sfloat;
                    pixelSize = 4;
                    break;

                case PixelFormat.ETC1:
                case PixelFormat.ETC2_RGB: // ETC1 upper compatible
                    format = Format.Etc2R8G8B8UnormBlock;
                    compressed = true;
                    pixelSize = 1; // 4bpp
                    break;
                case PixelFormat.ETC2_RGB_SRgb:
                    format = Format.Etc2R8G8B8SrgbBlock;
                    compressed = true;
                    pixelSize = 1;
                    break;
                case PixelFormat.ETC2_RGB_A1:
                    format = Format.Etc2R8G8B8A1UnormBlock;
                    compressed = true;
                    pixelSize = 1; // 4bpp
                    break;
                case PixelFormat.ETC2_RGBA: // ETC2 + EAC
                    format = Format.Etc2R8G8B8A8UnormBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.ETC2_RGBA_SRgb: // ETC2 + EAC
                    format = Format.Etc2R8G8B8A8SrgbBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.EAC_R11_Unsigned:
                    format = Format.EacR11UnormBlock;
                    compressed = true;
                    pixelSize = 1; // 4bpp
                    break;
                case PixelFormat.EAC_R11_Signed:
                    format = Format.EacR11SNormBlock;
                    compressed = true;
                    pixelSize = 1; // 4bpp
                    break;
                case PixelFormat.EAC_RG11_Unsigned:
                    format = Format.EacR11G11UnormBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.EAC_RG11_Signed:
                    format = Format.EacR11G11SNormBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;

                case PixelFormat.BC1_UNorm:
                    format = Format.BC1RgbaUnormBlock;
                    //format = Format.RAD_TEXTURE_FORMAT_DXT1_RGBA;
                    compressed = true;
                    pixelSize = 1; // 4bpp
                    break;
                case PixelFormat.BC1_UNorm_SRgb:
                    format = Format.BC1RgbaSrgbBlock;
                    //format = Format.RAD_TEXTURE_FORMAT_DXT1_RGBA_SRgb;
                    compressed = true;
                    pixelSize = 1; // 4bpp
                    break;
                case PixelFormat.BC2_UNorm:
                    format = Format.BC2UnormBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC2_UNorm_SRgb:
                    format = Format.BC2SrgbBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC3_UNorm:
                    format = Format.BC3UnormBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC3_UNorm_SRgb:
                    format = Format.BC3SrgbBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC4_SNorm:
                    format = Format.BC4SNormBlock;
                    compressed = true;
                    pixelSize = 1; // 4bpp
                    break;
                case PixelFormat.BC4_UNorm:
                    format = Format.BC4UnormBlock;
                    compressed = true;
                    pixelSize = 1; // 4bpp
                    break;
                case PixelFormat.BC5_SNorm:
                    format = Format.BC5SNormBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC5_UNorm:
                    format = Format.BC5UnormBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC6H_Sf16:
                    format = Format.BC6HSfloatBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC6H_Uf16:
                    format = Format.BC6HUfloatBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC7_UNorm:
                    format = Format.BC7UnormBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC7_UNorm_SRgb:
                    format = Format.BC7SrgbBlock;
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
                            return DescriptorType.StorageImage;

                        case EffectParameterType.Buffer:
                            return DescriptorType.StorageBuffer;

                        default:
                            throw new NotImplementedException();
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        public static int BlockSizeInBytes(this Format format)
        {
            switch (format)
            {
                case Format.BC1RgbUnormBlock:
                case Format.BC1RgbSrgbBlock:
                case Format.BC1RgbaUnormBlock:
                case Format.BC1RgbaSrgbBlock:
                    return 8;

                case Format.BC2UnormBlock:
                case Format.BC2SrgbBlock:
                case Format.BC3UnormBlock:
                case Format.BC3SrgbBlock:
                    return 16;

                case Format.BC4UnormBlock:
                case Format.BC4SNormBlock:
                    return 8;

                case Format.BC5UnormBlock:
                case Format.BC5SNormBlock:
                case Format.BC6HUfloatBlock:
                case Format.BC6HSfloatBlock:
                case Format.BC7UnormBlock:
                case Format.BC7SrgbBlock:
                    return 16;

                case Format.Etc2R8G8B8UnormBlock:
                case Format.Etc2R8G8B8SrgbBlock:
                case Format.Etc2R8G8B8A1UnormBlock:
                case Format.Etc2R8G8B8A1SrgbBlock:
                    return 8;
                    
                case Format.Etc2R8G8B8A8UnormBlock:
                case Format.Etc2R8G8B8A8SrgbBlock:
                    return 16;

                case Format.EacR11UnormBlock:
                case Format.EacR11SNormBlock:
                    return 8;

                case Format.EacR11G11UnormBlock:
                case Format.EacR11G11SNormBlock:
                    return 16;

                case Format.Astc4x4UnormBlock:
                case Format.Astc4x4SrgbBlock:
                case Format.Astc5x4UnormBlock:
                case Format.Astc5x4SrgbBlock:
                case Format.Astc5x5UnormBlock:
                case Format.Astc5x5SrgbBlock:
                case Format.Astc6x5UnormBlock:
                case Format.Astc6x5SrgbBlock:
                case Format.Astc6x6UnormBlock:
                case Format.Astc6x6SrgbBlock:
                case Format.Astc8x5UnormBlock:
                case Format.Astc8x5SrgbBlock:
                case Format.Astc8x6UnormBlock:
                case Format.Astc8x6SrgbBlock:
                case Format.Astc8x8UnormBlock:
                case Format.Astc8x8SrgbBlock:
                case Format.Astc10x5UnormBlock:
                case Format.Astc10x5SrgbBlock:
                case Format.Astc10x6UnormBlock:
                case Format.Astc10x6SrgbBlock:
                case Format.Astc10x8UnormBlock:
                case Format.Astc10x8SrgbBlock:
                case Format.Astc10x10UnormBlock:
                case Format.Astc10x10SrgbBlock:
                case Format.Astc12x10UnormBlock:
                case Format.Astc12x10SrgbBlock:
                case Format.Astc12x12UnormBlock:
                case Format.Astc12x12SrgbBlock:
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
