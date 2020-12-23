// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using System.Collections.Generic;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

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
            HasMultisampleDepthAsSRV = true;

            HasResourceRenaming = false;

            // TODO D3D12
            for (int i = 0; i < mapFeaturesPerFormat.Length; i++)
                mapFeaturesPerFormat[i] = new FeaturesPerFormat((PixelFormat)i, MultisampleCount.None, FormatSupport.None);
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
