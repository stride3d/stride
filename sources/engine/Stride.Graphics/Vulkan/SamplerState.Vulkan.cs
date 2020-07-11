// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

using Stride.Core.Mathematics;

namespace Stride.Graphics
{
    /// <summary>
    /// Describes a sampler state used for texture sampling.
    /// </summary>
    public partial class SamplerState
    {
        internal VkSampler NativeSampler;

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
            NativeSampler = VkSampler.Null;

            base.OnDestroyed();
        }

        private unsafe void CreateNativeSampler()
        {
            var createInfo = new VkSamplerCreateInfo
            {
                sType = VkStructureType.SamplerCreateInfo,
                addressModeU = ConvertAddressMode(Description.AddressU),
                addressModeV = ConvertAddressMode(Description.AddressV),
                addressModeW = ConvertAddressMode(Description.AddressW),
                mipLodBias = Description.MipMapLevelOfDetailBias,
                maxAnisotropy = Description.MaxAnisotropy,
                compareOp = VulkanConvertExtensions.ConvertComparisonFunction(Description.CompareFunction),
                minLod = Description.MinMipLevel,
                maxLod = Description.MaxMipLevel,
            };

            if (Description.AddressU == TextureAddressMode.Border ||
                Description.AddressV == TextureAddressMode.Border ||
                Description.AddressW == TextureAddressMode.Border)
            {
                if (Description.BorderColor == Color4.White)
                    createInfo.borderColor = VkBorderColor.FloatOpaqueWhite;
                else if (Description.BorderColor == Color4.Black)
                    createInfo.borderColor = VkBorderColor.FloatOpaqueBlack;
                else if (Description.BorderColor == Color.Transparent)
                    createInfo.borderColor = VkBorderColor.FloatTransparentBlack;
                else
                    throw new NotImplementedException("Vulkan: only simple BorderColor are supported");
            }

            ConvertMinFilter(Description.Filter, out createInfo.minFilter, out createInfo.magFilter, out createInfo.mipmapMode, out createInfo.compareEnable, out createInfo.anisotropyEnable);

            vkCreateSampler(GraphicsDevice.NativeDevice, &createInfo, null, out NativeSampler);
        }

        private static VkSamplerAddressMode ConvertAddressMode(TextureAddressMode addressMode)
        {
            switch (addressMode)
            {
                case TextureAddressMode.Wrap:
                    return VkSamplerAddressMode.Repeat;
                case TextureAddressMode.Border:
                    return VkSamplerAddressMode.ClampToBorder;
                case TextureAddressMode.Clamp:
                    return VkSamplerAddressMode.ClampToEdge;
                case TextureAddressMode.Mirror:
                    return VkSamplerAddressMode.MirroredRepeat;
                case TextureAddressMode.MirrorOnce:
                    return VkSamplerAddressMode.MirrorClampToEdge;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ConvertMinFilter(TextureFilter filter, out VkFilter minFilter, out VkFilter magFilter, out VkSamplerMipmapMode mipmapMode, out VkBool32 enableComparison, out VkBool32 enableAnisotropy)
        {
            minFilter = magFilter = VkFilter.Nearest;
            mipmapMode = VkSamplerMipmapMode.Nearest;
            enableComparison = false;
            enableAnisotropy = false;

            switch (filter)
            {
                // Mip point
                case TextureFilter.Point:
                    break;
                case TextureFilter.MinLinearMagMipPoint:
                    minFilter = VkFilter.Linear;
                    break;
                case TextureFilter.MinPointMagLinearMipPoint:
                    magFilter = VkFilter.Linear;
                    break;
                case TextureFilter.MinMagLinearMipPoint:
                    minFilter = VkFilter.Linear;
                    magFilter = VkFilter.Linear;
                    break;

                // Mip linear
                case TextureFilter.MinMagPointMipLinear:
                    mipmapMode = VkSamplerMipmapMode.Linear;
                    break;
                case TextureFilter.MinLinearMagPointMipLinear:
                    mipmapMode = VkSamplerMipmapMode.Linear;
                    minFilter = VkFilter.Linear;
                    break;
                case TextureFilter.MinPointMagMipLinear:
                    mipmapMode = VkSamplerMipmapMode.Linear;
                    magFilter = VkFilter.Linear;
                    break;
                case TextureFilter.Linear:
                    mipmapMode = VkSamplerMipmapMode.Linear;
                    minFilter = VkFilter.Linear;
                    magFilter = VkFilter.Linear;
                    break;
                case TextureFilter.Anisotropic:
                    enableAnisotropy = true;
                    mipmapMode = VkSamplerMipmapMode.Linear;
                    minFilter = VkFilter.Linear;
                    magFilter = VkFilter.Linear;
                    break;

                // Comparison mip point
                case TextureFilter.ComparisonPoint:
                    enableComparison = true;
                    break;
                case TextureFilter.ComparisonMinLinearMagMipPoint:
                    enableComparison = true;
                    minFilter = VkFilter.Linear;
                    break;
                case TextureFilter.ComparisonMinPointMagLinearMipPoint:
                    enableComparison = true;
                    magFilter = VkFilter.Linear;
                    break;
                case TextureFilter.ComparisonMinMagLinearMipPoint:
                    enableComparison = true;
                    minFilter = VkFilter.Linear;
                    magFilter = VkFilter.Linear;
                    break;

                // Comparison mip linear
                case TextureFilter.ComparisonMinMagPointMipLinear:
                    enableComparison = true;
                    mipmapMode = VkSamplerMipmapMode.Linear;
                    break;
                case TextureFilter.ComparisonMinLinearMagPointMipLinear:
                    enableComparison = true;
                    mipmapMode = VkSamplerMipmapMode.Linear;
                    minFilter = VkFilter.Linear;
                    break;
                case TextureFilter.ComparisonMinPointMagMipLinear:
                    enableComparison = true;
                    mipmapMode = VkSamplerMipmapMode.Linear;
                    magFilter = VkFilter.Linear;
                    break;
                case TextureFilter.ComparisonLinear:
                    enableComparison = true;
                    mipmapMode = VkSamplerMipmapMode.Linear;
                    minFilter = VkFilter.Linear;
                    magFilter = VkFilter.Linear;
                    break;
                case TextureFilter.ComparisonAnisotropic:
                    enableComparison = true;
                    enableAnisotropy = true;
                    mipmapMode = VkSamplerMipmapMode.Linear;
                    minFilter = VkFilter.Linear;
                    magFilter = VkFilter.Linear;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
} 
#endif
