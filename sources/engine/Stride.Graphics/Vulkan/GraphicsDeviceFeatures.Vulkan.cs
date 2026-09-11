// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_VULKAN

using Vortice.Vulkan;

namespace Stride.Graphics
{
    /// <summary>
    /// Features supported by a <see cref="GraphicsDevice"/>.
    /// </summary>
    /// <remarks>
    /// This class gives also features for a particular format, using the operator this[dxgiFormat] on this structure.
    /// </remarks>
    public partial struct GraphicsDeviceFeatures
    {
        internal GraphicsDeviceFeatures(GraphicsDevice deviceRoot)
        {
            //var nativeDevice = deviceRoot.NativeDevice;

            //PhysicalDeviceFeatures features;
            //deviceRoot.Adapter.PhysicalDevice.GetFeatures(out features);

            HasSRgb = true;

            mapFeaturesPerFormat = new FeaturesPerFormat[256];

            // Set back the real GraphicsProfile that is used
            RequestedProfile = deviceRoot.RequestedProfile;
            CurrentProfile = deviceRoot.RequestedProfile; // GraphicsProfileHelper.FromFeatureLevel(deviceRoot.CurrentFeatureLevel);

            HasComputeShaders = true;
            HasDoublePrecision = false;

            HasMultiThreadingConcurrentResources = true;
            HasDriverCommandLists = true;

            HasDepthAsSRV = true;
            HasDepthAsReadOnlyRT = true;
            HasMultiSampleDepthAsSRV = true;

            HasResourceRenaming = false;

            var physicalDevice = deviceRoot.NativePhysicalDevice;
            var instanceApi = deviceRoot.NativeInstanceApi;

            for (int i = 0; i < mapFeaturesPerFormat.Length; i++)
            {
                var pixelFormat = (PixelFormat) i;
                var maximumMultisampleCount = GetMaximumMultisampleCount(instanceApi, physicalDevice, pixelFormat);
                mapFeaturesPerFormat[i] = new FeaturesPerFormat(pixelFormat, maximumMultisampleCount, ComputeShaderFormatSupport.None, FormatSupport.None);
            }
            //// Check features for each DXGI.Format
            //foreach (var format in Enum.GetValues(typeof(SharpDX.DXGI.Format)))
            //{
            //    var dxgiFormat = (SharpDX.DXGI.Format)format;
            //    var maximumMultisampleCount = MultisampleCount.None;
            //    var computeShaderFormatSupport = ComputeShaderFormatSupport.None;
            //    var formatSupport = FormatSupport.None;

            //    if (!ObsoleteFormatToExcludes.Contains(dxgiFormat))
            //    {
            //        maximumMultisampleCount = GetMaximumMultisampleCount(nativeDevice, dxgiFormat);
            //        if (HasComputeShaders)
            //            computeShaderFormatSupport = nativeDevice.CheckComputeShaderFormatSupport(dxgiFormat);

            //        formatSupport = (FormatSupport)nativeDevice.CheckFormatSupport(dxgiFormat);
            //    }

            //    //mapFeaturesPerFormat[(int)dxgiFormat] = new FeaturesPerFormat((PixelFormat)dxgiFormat, maximumMultisampleCount, computeShaderFormatSupport, formatSupport);
            //    mapFeaturesPerFormat[(int)dxgiFormat] = new FeaturesPerFormat((PixelFormat)dxgiFormat, maximumMultisampleCount, formatSupport);
            //}
        }

        private static MultisampleCount GetMaximumMultisampleCount(VkInstanceApi instanceApi, VkPhysicalDevice physicalDevice, PixelFormat pixelFormat)
        {
            if (!VulkanConvertExtensions.TryConvertPixelFormat(pixelFormat, out var format, out _, out _))
                return MultisampleCount.None;

            // Same usage as Texture.CreateImage for a render target or depth stencil of that format
            var usage = VkImageUsageFlags.TransferSrc | VkImageUsageFlags.TransferDst;
            usage |= Texture.IsDepthFormat(pixelFormat) ? VkImageUsageFlags.DepthStencilAttachment : VkImageUsageFlags.ColorAttachment;

            var result = instanceApi.vkGetPhysicalDeviceImageFormatProperties(physicalDevice, format, VkImageType.Image2D, VkImageTiling.Optimal, usage, VkImageCreateFlags.None, out var imageFormatProperties);
            if (result != VkResult.Success)
                return MultisampleCount.None;

            var sampleCounts = imageFormatProperties.sampleCounts;
            if ((sampleCounts & VkSampleCountFlags.Count8) != 0)
                return MultisampleCount.X8;
            if ((sampleCounts & VkSampleCountFlags.Count4) != 0)
                return MultisampleCount.X4;
            if ((sampleCounts & VkSampleCountFlags.Count2) != 0)
                return MultisampleCount.X2;
            return MultisampleCount.None;
        }
    }
}

#endif
