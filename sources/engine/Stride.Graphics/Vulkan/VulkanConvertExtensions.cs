// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using Silk.NET.Vulkan;
using Stride.Shaders;

namespace Stride.Graphics
{
    internal static class VulkanConvertExtensions
    {
        public static PolygonMode ConvertFillMode(FillMode fillMode)
        {
            // NOTE: Vulkan's PolygonMode.Point is not exposed

            return fillMode switch
            {
                FillMode.Solid => PolygonMode.Fill,
                FillMode.Wireframe => PolygonMode.Line,
                _ => throw new ArgumentOutOfRangeException(nameof(fillMode)),
            };
        }

        public static CullModeFlags ConvertCullMode(CullMode cullMode)
        {
            // NOTE: Vulkan's CullModeFlags.FrontAndBack is not exposed

            return cullMode switch
            {
                CullMode.Back => CullModeFlags.BackBit,
                CullMode.Front => CullModeFlags.FrontBit,
                CullMode.None => CullModeFlags.None,
                _ => throw new ArgumentOutOfRangeException(nameof(cullMode)),
            };
        }

        public static PrimitiveTopology ConvertPrimitiveType(PrimitiveType primitiveType)
        {
            return primitiveType switch
            {
                PrimitiveType.PointList => PrimitiveTopology.PointList,
                PrimitiveType.LineList => PrimitiveTopology.LineList,
                PrimitiveType.LineStrip => PrimitiveTopology.LineStrip,
                PrimitiveType.TriangleList => PrimitiveTopology.TriangleList,
                PrimitiveType.TriangleStrip => PrimitiveTopology.TriangleStrip,
                PrimitiveType.LineListWithAdjacency => PrimitiveTopology.LineListWithAdjacency,
                PrimitiveType.LineStripWithAdjacency => PrimitiveTopology.LineStripWithAdjacency,
                PrimitiveType.TriangleListWithAdjacency => PrimitiveTopology.TriangleListWithAdjacency,
                PrimitiveType.TriangleStripWithAdjacency => PrimitiveTopology.TriangleStripWithAdjacency,
                _ => throw new ArgumentOutOfRangeException(nameof(primitiveType)),
            };
        }

        public static bool ConvertPrimitiveRestart(PrimitiveType primitiveType)
        {
            return primitiveType switch
            {
                PrimitiveType.PointList or PrimitiveType.LineList or 
                PrimitiveType.TriangleList or PrimitiveType.LineListWithAdjacency or
                PrimitiveType.TriangleListWithAdjacency or PrimitiveType.PatchList => false,
                _ => true,
            };
        }

        public static ShaderStageFlags Convert(ShaderStage stage)
        {
            return stage switch
            {
                ShaderStage.Vertex => ShaderStageFlags.VertexBit,
                ShaderStage.Hull => ShaderStageFlags.TessellationControlBit,
                ShaderStage.Domain => ShaderStageFlags.TessellationEvaluationBit,
                ShaderStage.Geometry => ShaderStageFlags.GeometryBit,
                ShaderStage.Pixel => ShaderStageFlags.FragmentBit,
                ShaderStage.Compute => ShaderStageFlags.ComputeBit,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        public static CompareOp ConvertComparisonFunction(CompareFunction comparison)
        {
            return comparison switch
            {
                CompareFunction.Always => CompareOp.Always,
                CompareFunction.Never => CompareOp.Never,
                CompareFunction.Equal => CompareOp.Equal,
                CompareFunction.Greater => CompareOp.Greater,
                CompareFunction.GreaterEqual => CompareOp.GreaterOrEqual,
                CompareFunction.Less => CompareOp.Less,
                CompareFunction.LessEqual => CompareOp.LessOrEqual,
                CompareFunction.NotEqual => CompareOp.NotEqual,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        public static StencilOp ConvertStencilOperation(StencilOperation operation)
        {
            return operation switch
            {
                StencilOperation.Decrement => StencilOp.DecrementAndWrap,
                StencilOperation.DecrementSaturation => StencilOp.DecrementAndClamp,
                StencilOperation.Increment => StencilOp.IncrementAndWrap,
                StencilOperation.IncrementSaturation => StencilOp.IncrementAndClamp,
                StencilOperation.Invert => StencilOp.Invert,
                StencilOperation.Keep => StencilOp.Keep,
                StencilOperation.Replace => StencilOp.Replace,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        public static BlendOp ConvertBlendFunction(BlendFunction blendFunction)
        {
            // TODO: Binary compatible
            return blendFunction switch
            {
                BlendFunction.Add => BlendOp.Add,
                BlendFunction.Subtract => BlendOp.Subtract,
                BlendFunction.ReverseSubtract => BlendOp.ReverseSubtract,
                BlendFunction.Max => BlendOp.Max,
                BlendFunction.Min => BlendOp.Min,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        public static BlendFactor ConvertBlend(Blend blend)
        {
            return blend switch
            {
                Blend.BlendFactor => BlendFactor.ConstantColor,
                Blend.DestinationAlpha => BlendFactor.DstAlpha,
                Blend.DestinationColor => BlendFactor.DstColor,
                Blend.InverseBlendFactor => BlendFactor.OneMinusConstantColor,
                Blend.InverseDestinationAlpha => BlendFactor.OneMinusDstAlpha,
                Blend.InverseDestinationColor => BlendFactor.OneMinusDstColor,
                Blend.InverseSecondarySourceAlpha => BlendFactor.OneMinusSrc1Alpha,
                Blend.InverseSecondarySourceColor => BlendFactor.OneMinusSrc1Color,
                Blend.InverseSourceAlpha => BlendFactor.OneMinusSrcAlpha,
                Blend.InverseSourceColor => BlendFactor.OneMinusSrcColor,
                Blend.One => BlendFactor.One,
                Blend.SecondarySourceAlpha => BlendFactor.Src1Alpha,
                Blend.SecondarySourceColor => BlendFactor.Src1Color,
                Blend.SourceAlpha => BlendFactor.SrcAlpha,
                Blend.SourceAlphaSaturate => BlendFactor.SrcAlphaSaturate,
                Blend.SourceColor => BlendFactor.SrcColor,
                Blend.Zero => BlendFactor.Zero,
                _ => throw new ArgumentOutOfRangeException(),
            };
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
            return @class switch
            {
                EffectParameterClass.ConstantBuffer => DescriptorType.UniformBuffer,
                EffectParameterClass.Sampler => DescriptorType.Sampler,
                EffectParameterClass.ShaderResourceView => type switch
                {
                    EffectParameterType.Texture or EffectParameterType.Texture1D or 
                    EffectParameterType.Texture2D or EffectParameterType.Texture3D or 
                    EffectParameterType.TextureCube or EffectParameterType.Texture1DArray or
                    EffectParameterType.Texture2DArray or EffectParameterType.TextureCubeArray => DescriptorType.SampledImage,
                    EffectParameterType.Buffer => DescriptorType.UniformTexelBuffer,
                    _ => throw new NotImplementedException(),
                },
                EffectParameterClass.UnorderedAccessView => type switch
                {
                    EffectParameterType.Texture or EffectParameterType.Texture1D or
                    EffectParameterType.Texture2D or EffectParameterType.Texture3D or 
                    EffectParameterType.TextureCube or EffectParameterType.Texture1DArray or 
                    EffectParameterType.Texture2DArray or EffectParameterType.TextureCubeArray => DescriptorType.StorageImage,
                    EffectParameterType.Buffer => DescriptorType.StorageBuffer,
                    _ => throw new NotImplementedException(),
                },
                _ => throw new NotImplementedException(),
            };
        }

        public static int BlockSizeInBytes(this Format format)
        {
            return format switch
            {
                Format.BC1RgbUnormBlock or Format.BC1RgbSrgbBlock or Format.BC1RgbaUnormBlock or Format.BC1RgbaSrgbBlock => 8,
                Format.BC2UnormBlock or Format.BC2SrgbBlock or Format.BC3UnormBlock or Format.BC3SrgbBlock => 16,
                Format.BC4UnormBlock or Format.BC4SNormBlock => 8,
                Format.BC5UnormBlock or Format.BC5SNormBlock or Format.BC6HUfloatBlock or Format.BC6HSfloatBlock or Format.BC7UnormBlock or Format.BC7SrgbBlock => 16,
                Format.Etc2R8G8B8UnormBlock or Format.Etc2R8G8B8SrgbBlock or Format.Etc2R8G8B8A1UnormBlock or Format.Etc2R8G8B8A1SrgbBlock => 8,
                Format.Etc2R8G8B8A8UnormBlock or Format.Etc2R8G8B8A8SrgbBlock => 16,
                Format.EacR11UnormBlock or Format.EacR11SNormBlock => 8,
                Format.EacR11G11UnormBlock or Format.EacR11G11SNormBlock or
                Format.Astc4x4UnormBlock or Format.Astc4x4SrgbBlock or Format.Astc5x4UnormBlock or 
                Format.Astc5x4SrgbBlock or Format.Astc5x5UnormBlock or Format.Astc5x5SrgbBlock or
                Format.Astc6x5UnormBlock or Format.Astc6x5SrgbBlock or Format.Astc6x6UnormBlock or 
                Format.Astc6x6SrgbBlock or Format.Astc8x5UnormBlock or Format.Astc8x5SrgbBlock or 
                Format.Astc8x6UnormBlock or Format.Astc8x6SrgbBlock or Format.Astc8x8UnormBlock or 
                Format.Astc8x8SrgbBlock or Format.Astc10x5UnormBlock or Format.Astc10x5SrgbBlock or 
                Format.Astc10x6UnormBlock or Format.Astc10x6SrgbBlock or Format.Astc10x8UnormBlock or 
                Format.Astc10x8SrgbBlock or Format.Astc10x10UnormBlock or Format.Astc10x10SrgbBlock or 
                Format.Astc12x10UnormBlock or Format.Astc12x10SrgbBlock or Format.Astc12x12UnormBlock or Format.Astc12x12SrgbBlock => 16,
                //case Format.Pvrtc12BppUNormBlock:
                //case Format.Pvrtc14BppUNormBlock:
                //case Format.Pvrtc22BppUNormBlock:
                //case Format.Pvrtc24BppUNormBlock:
                //case Format.Pvrtc12BppSRgbBlock:
                //case Format.Pvrtc14BppSRgbBlock:
                //case Format.Pvrtc22BppSRgbBlock:
                //case Format.Pvrtc24BppSRgbBlock:
                //    return 8;
                _ => throw new ArgumentOutOfRangeException(nameof(format)),
            };
        }
    }
}

#endif
