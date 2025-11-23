// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using Vortice.Vulkan;
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
                    format = VkFormat.R8Unorm;
                    pixelSize = 1;
                    break;
                case PixelFormat.R8_SNorm:
                    format = VkFormat.R8Snorm;
                    pixelSize = 1;
                    break;
                case PixelFormat.R8_UInt:
                    format = VkFormat.R8Uint;
                    pixelSize = 1;
                    break;
                case PixelFormat.R8_SInt:
                    format = VkFormat.R8Sint;
                    pixelSize = 1;
                    break;

                case PixelFormat.R8G8B8A8_UNorm:
                    format = VkFormat.R8G8B8A8Unorm;
                    pixelSize = 4;
                    break;
                case PixelFormat.R8G8B8A8_UInt:
                    format = VkFormat.R8G8B8A8Uint;
                    pixelSize = 4;
                    break;
                case PixelFormat.R8G8B8A8_SInt:
                    format = VkFormat.R8G8B8A8Sint;
                    pixelSize = 4;
                    break;
                case PixelFormat.B8G8R8A8_UNorm:
                    format = VkFormat.B8G8R8A8Unorm;
                    pixelSize = 4;
                    break;
                case PixelFormat.R8G8B8A8_UNorm_SRgb:
                    format = VkFormat.R8G8B8A8Srgb;
                    pixelSize = 4;
                    break;
                case PixelFormat.B8G8R8A8_UNorm_SRgb:
                    format = VkFormat.B8G8R8A8Srgb;
                    pixelSize = 4;
                    break;

                case PixelFormat.R10G10B10A2_UInt:
                    format = VkFormat.A2R10G10B10UintPack32;
                    pixelSize = 4;
                    break;
                case PixelFormat.R10G10B10A2_UNorm:
                    format = VkFormat.A2R10G10B10UnormPack32;
                    pixelSize = 4;
                    break;

                case PixelFormat.R16_Float:
                    format = VkFormat.R16Sfloat;
                    pixelSize = 2;
                    break;
                case PixelFormat.R16_UNorm:
                    format = VkFormat.R16Unorm;
                    pixelSize = 2;
                    break;
                case PixelFormat.R16_UInt:
                    format = VkFormat.R16Uint;
                    pixelSize = 2;
                    break;
                case PixelFormat.R16_SInt:
                    format = VkFormat.R16Sint;
                    pixelSize = 2;
                    break;

                case PixelFormat.R16G16_Float:
                    format = VkFormat.R16G16Sfloat;
                    pixelSize = 4;
                    break;
                case PixelFormat.R16G16_SNorm:
                    format = VkFormat.R16G16Snorm;
                    pixelSize = 4;
                    break;
                case PixelFormat.R16G16_UNorm:
                    format = VkFormat.R16G16Unorm;
                    pixelSize = 4;
                    break;
                case PixelFormat.R16G16_SInt:
                    format = VkFormat.R16G16Snorm;
                    pixelSize = 4;
                    break;
                case PixelFormat.R16G16_UInt:
                    format = VkFormat.R16G16Unorm;
                    pixelSize = 4;
                    break;

                case PixelFormat.R16G16B16A16_Float:
                    format = VkFormat.R16G16B16A16Sfloat;
                    pixelSize = 8;
                    break;
                case PixelFormat.R16G16B16A16_UNorm:
                    format = VkFormat.R16G16B16A16Unorm;
                    pixelSize = 8;
                    break;
                case PixelFormat.R16G16B16A16_SNorm:
                    format = VkFormat.R16G16B16A16Snorm;
                    pixelSize = 8;
                    break;
                case PixelFormat.R16G16B16A16_UInt:
                    format = VkFormat.R16G16B16A16Uint;
                    pixelSize = 8;
                    break;
                case PixelFormat.R16G16B16A16_SInt:
                    format = VkFormat.R16G16B16A16Sint;
                    pixelSize = 8;
                    break;

                case PixelFormat.R32_UInt:
                    format = VkFormat.R32Uint;
                    pixelSize = 4;
                    break;
                case PixelFormat.R32_Float:
                    format = VkFormat.R32Sfloat;
                    pixelSize = 4;
                    break;

                case PixelFormat.R32G32_Float:
                    format = VkFormat.R32G32Sfloat;
                    pixelSize = 8;
                    break;
                case PixelFormat.R32G32_UInt:
                    format = VkFormat.R32G32Uint;
                    pixelSize = 8;
                    break;
                case PixelFormat.R32G32_SInt:
                    format = VkFormat.R32G32Sint;
                    pixelSize = 8;
                    break;

                case PixelFormat.R32G32B32_Float:
                    format = VkFormat.R32G32B32Sfloat;
                    pixelSize = 12;
                    break;
                case PixelFormat.R32G32B32_SInt:
                    format = VkFormat.R32G32B32Sint;
                    pixelSize = 12;
                    break;
                case PixelFormat.R32G32B32_UInt:
                    format = VkFormat.R32G32B32Uint;
                    pixelSize = 12;
                    break;

                case PixelFormat.R32G32B32A32_Float:
                    format = VkFormat.R32G32B32A32Sfloat;
                    pixelSize = 16;
                    break;
                case PixelFormat.R32G32B32A32_SInt:
                    format = VkFormat.R32G32B32A32Sint;
                    pixelSize = 16;
                    break;
                case PixelFormat.R32G32B32A32_UInt:
                    format = VkFormat.R32G32B32A32Uint;
                    pixelSize = 16;
                    break;

                case PixelFormat.D16_UNorm:
                    format = VkFormat.D16Unorm;
                    pixelSize = 2;
                    break;
                case PixelFormat.D24_UNorm_S8_UInt:
                    format = VkFormat.D24UnormS8Uint;
                    pixelSize = 4;
                    break;
                // TODO: Temporary depth format (need to decide relation between RenderTarget1D and Texture)
                case PixelFormat.D32_Float:
                    format = VkFormat.D32Sfloat;
                    pixelSize = 4;
                    break;
                case PixelFormat.D32_Float_S8X24_UInt:
                    format = VkFormat.D32SfloatS8Uint;
                    pixelSize = 8;
                    break;

                case PixelFormat.ETC1:
                case PixelFormat.ETC2_RGB: // ETC1 upper compatible
                    format = VkFormat.Etc2R8G8B8UnormBlock;
                    compressed = true;
                    pixelSize = 1; // 4bpp
                    break;
                case PixelFormat.ETC2_RGB_SRgb:
                    format = VkFormat.Etc2R8G8B8SrgbBlock;
                    compressed = true;
                    pixelSize = 1;
                    break;
                case PixelFormat.ETC2_RGB_A1:
                    format = VkFormat.Etc2R8G8B8A1UnormBlock;
                    compressed = true;
                    pixelSize = 1; // 4bpp
                    break;
                case PixelFormat.ETC2_RGBA: // ETC2 + EAC
                    format = VkFormat.Etc2R8G8B8A8UnormBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.ETC2_RGBA_SRgb: // ETC2 + EAC
                    format = VkFormat.Etc2R8G8B8A8SrgbBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.EAC_R11_Unsigned:
                    format = VkFormat.EacR11UnormBlock;
                    compressed = true;
                    pixelSize = 1; // 4bpp
                    break;
                case PixelFormat.EAC_R11_Signed:
                    format = VkFormat.EacR11SnormBlock;
                    compressed = true;
                    pixelSize = 1; // 4bpp
                    break;
                case PixelFormat.EAC_RG11_Unsigned:
                    format = VkFormat.EacR11G11UnormBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.EAC_RG11_Signed:
                    format = VkFormat.EacR11G11SnormBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;

                case PixelFormat.BC1_UNorm:
                    format = VkFormat.Bc1RgbaUnormBlock;
                    //format = VkFormat.RAD_TEXTURE_FORMAT_DXT1_RGBA;
                    compressed = true;
                    pixelSize = 1; // 4bpp
                    break;
                case PixelFormat.BC1_UNorm_SRgb:
                    format = VkFormat.Bc1RgbaSrgbBlock;
                    //format = VkFormat.RAD_TEXTURE_FORMAT_DXT1_RGBA_SRgb;
                    compressed = true;
                    pixelSize = 1; // 4bpp
                    break;
                case PixelFormat.BC2_UNorm:
                    format = VkFormat.Bc2UnormBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC2_UNorm_SRgb:
                    format = VkFormat.Bc2SrgbBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC3_UNorm:
                    format = VkFormat.Bc3UnormBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC3_UNorm_SRgb:
                    format = VkFormat.Bc3SrgbBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC4_SNorm:
                    format = VkFormat.Bc4SnormBlock;
                    compressed = true;
                    pixelSize = 1; // 4bpp
                    break;
                case PixelFormat.BC4_UNorm:
                    format = VkFormat.Bc4UnormBlock;
                    compressed = true;
                    pixelSize = 1; // 4bpp
                    break;
                case PixelFormat.BC5_SNorm:
                    format = VkFormat.Bc5SnormBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC5_UNorm:
                    format = VkFormat.Bc5UnormBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC6H_Sf16:
                    format = VkFormat.Bc6hSfloatBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC6H_Uf16:
                    format = VkFormat.Bc6hUfloatBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC7_UNorm:
                    format = VkFormat.Bc7UnormBlock;
                    compressed = true;
                    pixelSize = 2; // 8bpp
                    break;
                case PixelFormat.BC7_UNorm_SRgb:
                    format = VkFormat.Bc7SrgbBlock;
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
                        case EffectParameterType.RWTexture1D:
                        case EffectParameterType.RWTexture1DArray:
                        case EffectParameterType.RWTexture2D:
                        case EffectParameterType.RWTexture2DArray:
                        case EffectParameterType.RWTexture3D:
                            return VkDescriptorType.SampledImage;

                        case EffectParameterType.Buffer:
                            return VkDescriptorType.UniformTexelBuffer;
                        case EffectParameterType.StructuredBuffer:
                            return VkDescriptorType.StorageBuffer;

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
                        case EffectParameterType.RWTexture1D:
                        case EffectParameterType.RWTexture1DArray:
                        case EffectParameterType.RWTexture2D:
                        case EffectParameterType.RWTexture2DArray:
                        case EffectParameterType.RWTexture3D:
                        case EffectParameterType.RWBuffer:
                            return VkDescriptorType.StorageImage;

                        case EffectParameterType.Buffer:
                        case EffectParameterType.StructuredBuffer:
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
                case VkFormat.Bc1RgbUnormBlock:
                case VkFormat.Bc1RgbSrgbBlock:
                case VkFormat.Bc1RgbaUnormBlock:
                case VkFormat.Bc1RgbaSrgbBlock:
                    return 8;

                case VkFormat.Bc2UnormBlock:
                case VkFormat.Bc2SrgbBlock:
                case VkFormat.Bc3UnormBlock:
                case VkFormat.Bc3SrgbBlock:
                    return 16;

                case VkFormat.Bc4UnormBlock:
                case VkFormat.Bc4SnormBlock:
                    return 8;

                case VkFormat.Bc5UnormBlock:
                case VkFormat.Bc5SnormBlock:
                case VkFormat.Bc6hUfloatBlock:
                case VkFormat.Bc6hSfloatBlock:
                case VkFormat.Bc7UnormBlock:
                case VkFormat.Bc7SrgbBlock:
                    return 16;

                case VkFormat.Etc2R8G8B8UnormBlock:
                case VkFormat.Etc2R8G8B8SrgbBlock:
                case VkFormat.Etc2R8G8B8A1UnormBlock:
                case VkFormat.Etc2R8G8B8A1SrgbBlock:
                    return 8;

                case VkFormat.Etc2R8G8B8A8UnormBlock:
                case VkFormat.Etc2R8G8B8A8SrgbBlock:
                    return 16;

                case VkFormat.EacR11UnormBlock:
                case VkFormat.EacR11SnormBlock:
                    return 8;

                case VkFormat.EacR11G11UnormBlock:
                case VkFormat.EacR11G11SnormBlock:
                    return 16;

                case VkFormat.Astc4x4UnormBlock:
                case VkFormat.Astc4x4SrgbBlock:
                case VkFormat.Astc5x4UnormBlock:
                case VkFormat.Astc5x4SrgbBlock:
                case VkFormat.Astc5x5UnormBlock:
                case VkFormat.Astc5x5SrgbBlock:
                case VkFormat.Astc6x5UnormBlock:
                case VkFormat.Astc6x5SrgbBlock:
                case VkFormat.Astc6x6UnormBlock:
                case VkFormat.Astc6x6SrgbBlock:
                case VkFormat.Astc8x5UnormBlock:
                case VkFormat.Astc8x5SrgbBlock:
                case VkFormat.Astc8x6UnormBlock:
                case VkFormat.Astc8x6SrgbBlock:
                case VkFormat.Astc8x8UnormBlock:
                case VkFormat.Astc8x8SrgbBlock:
                case VkFormat.Astc10x5UnormBlock:
                case VkFormat.Astc10x5SrgbBlock:
                case VkFormat.Astc10x6UnormBlock:
                case VkFormat.Astc10x6SrgbBlock:
                case VkFormat.Astc10x8UnormBlock:
                case VkFormat.Astc10x8SrgbBlock:
                case VkFormat.Astc10x10UnormBlock:
                case VkFormat.Astc10x10SrgbBlock:
                case VkFormat.Astc12x10UnormBlock:
                case VkFormat.Astc12x10SrgbBlock:
                case VkFormat.Astc12x12UnormBlock:
                case VkFormat.Astc12x12SrgbBlock:
                    return 16;

                default:
                    throw new ArgumentOutOfRangeException(nameof(format));
            }
        }
    }
}

#endif
