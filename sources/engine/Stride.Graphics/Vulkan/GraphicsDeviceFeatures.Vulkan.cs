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

            // VK_INDEX_TYPE_UINT32 is core Vulkan.
            HasIndex32Bits = true;

            HasResourceRenaming = false;

            // Ask the device about each format. MultisampleCountMax deliberately stays None below:
            // this backend does not implement multisampling, GraphicsBackend.Vulkan says so, and
            // ForwardRenderer and Texture.InitializeFrom read that field raw rather than through
            // Supports, so an honest value there would let them create images this backend cannot make.
            for (int i = 0; i < mapFeaturesPerFormat.Length; i++)
            {
                var pixelFormat = (PixelFormat) i;
                var formatSupport = FormatSupport.None;

                if (VulkanConvertExtensions.TryConvertPixelFormat(pixelFormat, out var vulkanFormat) &&
                    vulkanFormat != VkFormat.Undefined)
                {
                    deviceRoot.NativeInstanceApi.vkGetPhysicalDeviceFormatProperties(
                        deviceRoot.NativePhysicalDevice, vulkanFormat, out var formatProperties);

                    formatSupport = ConvertFormatSupport(formatProperties);
                }

                mapFeaturesPerFormat[i] = new FeaturesPerFormat(pixelFormat, MultisampleCount.None, ComputeShaderFormatSupport.None, formatSupport);
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

        /// <summary>
        ///   Maps what Vulkan reports for a format onto the flags Stride uses.
        /// </summary>
        /// <remarks>
        ///   <see cref="FormatSupport"/> mirrors D3D11_FORMAT_SUPPORT, so the two do not line up. Only
        ///   flags this query answers precisely are set.
        ///   <para>
        ///     Left unset on purpose: Texture1D, Texture3D and TextureCube, which need
        ///     vkGetPhysicalDeviceImageFormatProperties rather than this call, and would over-claim if
        ///     taken from SampledImage alone — a block-compressed format samples in 2D but not in 3D.
        ///     Also unset are every multisample flag, CpuLockable, Display, MipAutogen and the video
        ///     flags, none of which this query reports.
        ///   </para>
        /// </remarks>
        private static FormatSupport ConvertFormatSupport(VkFormatProperties formatProperties)
        {
            var imageFeatures = formatProperties.optimalTilingFeatures;
            var bufferFeatures = formatProperties.bufferFeatures;

            var formatSupport = FormatSupport.None;

            if ((imageFeatures & VkFormatFeatureFlags.SampledImage) != 0)
                formatSupport |= FormatSupport.ShaderLoad | FormatSupport.Texture2D;

            // D3D separates a filtered sample from an unfiltered load, and so does Vulkan.
            if ((imageFeatures & VkFormatFeatureFlags.SampledImageFilterLinear) != 0)
                formatSupport |= FormatSupport.ShaderSample;

            if ((imageFeatures & VkFormatFeatureFlags.ColorAttachment) != 0)
                formatSupport |= FormatSupport.RenderTarget;

            if ((imageFeatures & VkFormatFeatureFlags.ColorAttachmentBlend) != 0)
                formatSupport |= FormatSupport.Blendable;

            if ((imageFeatures & VkFormatFeatureFlags.DepthStencilAttachment) != 0)
                formatSupport |= FormatSupport.DepthStencil;

            if ((imageFeatures & VkFormatFeatureFlags.StorageImage) != 0)
                formatSupport |= FormatSupport.TypedUnorderedAccessView;

            if ((bufferFeatures & VkFormatFeatureFlags.VertexBuffer) != 0)
                formatSupport |= FormatSupport.InputAssemblyVertexBuffer;

            if ((bufferFeatures & (VkFormatFeatureFlags.UniformTexelBuffer | VkFormatFeatureFlags.StorageTexelBuffer)) != 0)
                formatSupport |= FormatSupport.Buffer;

            return formatSupport;
        }
    }
}

#endif
