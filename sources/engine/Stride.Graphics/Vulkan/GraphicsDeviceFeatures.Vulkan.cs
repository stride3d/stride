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

            // Multisample support was never queried here: every format was reported as
            // MultisampleCount.None, so Texture.New threw "the maximum supported level is None" for
            // any multisampled render target - MSAA was simply unusable on Vulkan. Stride.Voxels'
            // dominant-axis voxelization allocates an 8x target and died on it.
            //
            // Only the framebuffer masks are read, not per-format image properties: this runs for
            // all 256 PixelFormat values at device creation, and the framebuffer limits are what
            // actually bound a render target's sample count.
            var physicalDevice = deviceRoot.NativePhysicalDevice;
            deviceRoot.NativeInstanceApi.vkGetPhysicalDeviceProperties(physicalDevice, out var physicalDeviceProperties);
            var colorSampleCounts = physicalDeviceProperties.limits.framebufferColorSampleCounts;
            var depthSampleCounts = physicalDeviceProperties.limits.framebufferDepthSampleCounts;

            static MultisampleCount MaximumSampleCount(VkSampleCountFlags supported)
            {
                if ((supported & VkSampleCountFlags.Count8) != 0)
                    return MultisampleCount.X8;
                if ((supported & VkSampleCountFlags.Count4) != 0)
                    return MultisampleCount.X4;
                if ((supported & VkSampleCountFlags.Count2) != 0)
                    return MultisampleCount.X2;
                return MultisampleCount.None;
            }

            // Reported per format, but computed from the intersection of the colour and depth masks
            // rather than per format: telling colour and depth formats apart here would need a
            // predicate Stride does not have on this side, and the two masks are identical on every
            // driver worth supporting. Erring low only costs a sample count; erring high would hand
            // out a count the device cannot attach.
            var maximumMultisampleCount = MaximumSampleCount(colorSampleCounts & depthSampleCounts);

            for (int i = 0; i < mapFeaturesPerFormat.Length; i++)
                mapFeaturesPerFormat[i] = new FeaturesPerFormat((PixelFormat) i, maximumMultisampleCount, ComputeShaderFormatSupport.None, FormatSupport.None);
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
    }
}

#endif
