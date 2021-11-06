// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using Silk.NET.Vulkan;
using static Silk.NET.Vulkan.Vk;

using Stride.Core.Mathematics;
using Silk.NET.Core;

namespace Stride.Graphics
{
    /// <summary>
    /// Describes a sampler state used for texture sampling.
    /// </summary>
    public partial class SamplerState
    {
        internal Sampler NativeSampler;

        /// <summary>
        /// Initializes a new instance of the <see cref="SamplerState"/> class.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="name">The name.</param>
        /// <param name="samplerStateDescription">The sampler state description.</param>
        private SamplerState(GraphicsDevice device, SamplerStateDescription samplerStateDescription) : base(device)
        {
            Description = samplerStateDescription;

            CreateNativeSampler();
        }

        /// <inheritdoc/>
        protected internal override bool OnRecreate()
        {
            base.OnRecreate();
            CreateNativeSampler();
            return true;
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed()
        {
            GraphicsDevice.Collect(NativeSampler);
            NativeSampler = new Sampler(0);

            base.OnDestroyed();
        }

        private unsafe void CreateNativeSampler()
        {
            var createInfo = new SamplerCreateInfo
            {
                SType = StructureType.SamplerCreateInfo,
                AddressModeU = ConvertAddressMode(Description.AddressU),
                AddressModeV = ConvertAddressMode(Description.AddressV),
                AddressModeW = ConvertAddressMode(Description.AddressW),
                MipLodBias = Description.MipMapLevelOfDetailBias,
                MaxAnisotropy = Description.MaxAnisotropy,
                CompareOp = VulkanConvertExtensions.ConvertComparisonFunction(Description.CompareFunction),
                MinLod = Description.MinMipLevel,
                MaxLod = Description.MaxMipLevel,
            };

            if (Description.AddressU == TextureAddressMode.Border ||
                Description.AddressV == TextureAddressMode.Border ||
                Description.AddressW == TextureAddressMode.Border)
            {
                if (Description.BorderColor == Color4.White)
                    createInfo.BorderColor = BorderColor.FloatOpaqueWhite;
                else if (Description.BorderColor == Color4.Black)
                    createInfo.BorderColor = BorderColor.FloatOpaqueBlack;
                else if (Description.BorderColor == Color.Transparent)
                    createInfo.BorderColor = BorderColor.FloatTransparentBlack;
                else
                    throw new NotImplementedException("Vulkan: only simple BorderColor are supported");
            }

            ConvertMinFilter(Description.Filter, out createInfo.MinFilter, out createInfo.MagFilter, out createInfo.MipmapMode, out createInfo.CompareEnable, out createInfo.AnisotropyEnable);

            GetApi().CreateSampler(GraphicsDevice.NativeDevice, &createInfo, null, out NativeSampler);
        }

        private static SamplerAddressMode ConvertAddressMode(TextureAddressMode addressMode)
        {
            switch (addressMode)
            {
                case TextureAddressMode.Wrap:
                    return SamplerAddressMode.Repeat;
                case TextureAddressMode.Border:
                    return SamplerAddressMode.ClampToBorder;
                case TextureAddressMode.Clamp:
                    return SamplerAddressMode.ClampToEdge;
                case TextureAddressMode.Mirror:
                    return SamplerAddressMode.MirroredRepeat;
                case TextureAddressMode.MirrorOnce:
                    return SamplerAddressMode.MirrorClampToEdge;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ConvertMinFilter(TextureFilter filter, out Filter minFilter, out Filter magFilter, out SamplerMipmapMode mipmapMode, out Bool32 enableComparison, out Bool32 enableAnisotropy)
        {
            minFilter = magFilter = Filter.Nearest;
            mipmapMode = SamplerMipmapMode.Nearest;
            enableComparison = false;
            enableAnisotropy = false;

            switch (filter)
            {
                // Mip point
                case TextureFilter.Point:
                    break;
                case TextureFilter.MinLinearMagMipPoint:
                    minFilter = Filter.Linear;
                    break;
                case TextureFilter.MinPointMagLinearMipPoint:
                    magFilter = Filter.Linear;
                    break;
                case TextureFilter.MinMagLinearMipPoint:
                    minFilter = Filter.Linear;
                    magFilter = Filter.Linear;
                    break;

                // Mip linear
                case TextureFilter.MinMagPointMipLinear:
                    mipmapMode = SamplerMipmapMode.Linear;
                    break;
                case TextureFilter.MinLinearMagPointMipLinear:
                    mipmapMode = SamplerMipmapMode.Linear;
                    minFilter = Filter.Linear;
                    break;
                case TextureFilter.MinPointMagMipLinear:
                    mipmapMode = SamplerMipmapMode.Linear;
                    magFilter = Filter.Linear;
                    break;
                case TextureFilter.Linear:
                    mipmapMode = SamplerMipmapMode.Linear;
                    minFilter = Filter.Linear;
                    magFilter = Filter.Linear;
                    break;
                case TextureFilter.Anisotropic:
                    enableAnisotropy = true;
                    mipmapMode = SamplerMipmapMode.Linear;
                    minFilter = Filter.Linear;
                    magFilter = Filter.Linear;
                    break;

                // Comparison mip point
                case TextureFilter.ComparisonPoint:
                    enableComparison = true;
                    break;
                case TextureFilter.ComparisonMinLinearMagMipPoint:
                    enableComparison = true;
                    minFilter = Filter.Linear;
                    break;
                case TextureFilter.ComparisonMinPointMagLinearMipPoint:
                    enableComparison = true;
                    magFilter = Filter.Linear;
                    break;
                case TextureFilter.ComparisonMinMagLinearMipPoint:
                    enableComparison = true;
                    minFilter = Filter.Linear;
                    magFilter = Filter.Linear;
                    break;

                // Comparison mip linear
                case TextureFilter.ComparisonMinMagPointMipLinear:
                    enableComparison = true;
                    mipmapMode = SamplerMipmapMode.Linear;
                    break;
                case TextureFilter.ComparisonMinLinearMagPointMipLinear:
                    enableComparison = true;
                    mipmapMode = SamplerMipmapMode.Linear;
                    minFilter = Filter.Linear;
                    break;
                case TextureFilter.ComparisonMinPointMagMipLinear:
                    enableComparison = true;
                    mipmapMode = SamplerMipmapMode.Linear;
                    magFilter = Filter.Linear;
                    break;
                case TextureFilter.ComparisonLinear:
                    enableComparison = true;
                    mipmapMode = SamplerMipmapMode.Linear;
                    minFilter = Filter.Linear;
                    magFilter = Filter.Linear;
                    break;
                case TextureFilter.ComparisonAnisotropic:
                    enableComparison = true;
                    enableAnisotropy = true;
                    mipmapMode = SamplerMipmapMode.Linear;
                    minFilter = Filter.Linear;
                    magFilter = Filter.Linear;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
} 
#endif
