// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_VULKAN

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Stride.Core;
using Vortice.Vulkan;

using static Vortice.Vulkan.Vulkan;

namespace Stride.Graphics
{
    /// <summary>
    /// Provides methods to retrieve and manipulate graphics adapters. This is the equivalent to <see cref="Adapter1"/>.
    /// </summary>
    /// <msdn-id>ff471329</msdn-id>
    /// <unmanaged>IDXGIAdapter1</unmanaged>
    /// <unmanaged-short>IDXGIAdapter1</unmanaged-short>
    public partial class GraphicsAdapter
    {
        private VkPhysicalDevice defaultPhysicalDevice;
        private Lazy<VkPhysicalDevice> debugPhysicalDevice;

        private readonly int adapterOrdinal;
        private readonly string description;
        private readonly VkPhysicalDeviceProperties deviceProperties;

        private static readonly Dictionary<int, string> VendorNames = new Dictionary<int, string>
        {
            [0x1002] = "AMD",
            [0x1010] = "ImgTec",
            [0x10DE] = "NVIDIA",
            [0x13B5] = "ARM",
            [0x5143] = "Qualcomm",
            [0x8086] = "INTEL",
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphicsAdapter" /> class.
        /// </summary>
        /// <param name="physicalDevice">The default factory.</param>
        /// <param name="adapterOrdinal">The adapter ordinal.</param>
        internal unsafe GraphicsAdapter(VkPhysicalDevice defaultPhysicalDevice,
            List<DisplayInfo> displayInfos,
            VkPhysicalDeviceProperties deviceProperties,
            int adapterOrdinal)
        {
            this.adapterOrdinal = adapterOrdinal;
            this.debugPhysicalDevice = new Lazy<VkPhysicalDevice>(GetDebugPhysicalDevice);
            this.defaultPhysicalDevice = defaultPhysicalDevice;
            this.deviceProperties = deviceProperties;

            description = Marshal.PtrToStringAnsi((IntPtr)deviceProperties.deviceName);
            if (VendorNames.TryGetValue(VendorId, out var vendorName))
                description = $"{vendorName} {description}";

            graphicsOutputs = new GraphicsOutput[displayInfos.Count];

            for (var index = 0; index < graphicsOutputs.Length; index++)
                graphicsOutputs[index] = new GraphicsOutput(this, displayInfos[index], index).DisposeBy(this);
        }

        /// <summary>
        /// Gets the description of this adapter.
        /// </summary>
        /// <value>The description.</value>
        public unsafe string Description
        {
            get
            {
                return description;
            }
        }

        /// <summary>
        /// Gets or sets the vendor identifier.
        /// </summary>
        /// <value>
        /// The vendor identifier.
        /// </value>
        public int VendorId
        {
            get
            {
                return (int)deviceProperties.vendorID;
            }
        }

        /// <summary>
        /// Determines if this instance of GraphicsAdapter is the default adapter.
        /// </summary>
        public bool IsDefaultAdapter
        {
            get
            {
                return adapterOrdinal == 0;
            }
        }

        internal unsafe VkPhysicalDevice GetPhysicalDevice(bool enableValidation)
        {
            return enableValidation
                ? debugPhysicalDevice.Value
                : defaultPhysicalDevice;
        }

        /// <summary>
        /// Tests to see if the adapter supports the requested profile.
        /// </summary>
        /// <param name="graphicsProfile">The graphics profile.</param>
        /// <returns>true if the profile is supported</returns>
        public bool IsProfileSupported(GraphicsProfile graphicsProfile)
        {
            // Lower profiles are always supported on any conformant Vulkan device
            if (graphicsProfile <= GraphicsProfile.Level_10_1)
                return true;

            if (graphicsProfile >= GraphicsProfile.Level_11_0)
                return deviceProperties.apiVersion >= VkVersion.Version_1_1;

            return false;
        }

        private unsafe VkPhysicalDevice GetDebugPhysicalDevice()
        {
            GraphicsAdapterFactoryInstance defaultInstance = GraphicsAdapterFactory.GetInstance(true);
            uint physicalDevicesCount = 0;
            defaultInstance.NativeInstanceApi.vkEnumeratePhysicalDevices(defaultInstance.NativeInstance, &physicalDevicesCount, null).CheckResult();

            Span<VkPhysicalDevice> nativePhysicalDevices = stackalloc VkPhysicalDevice[(int)physicalDevicesCount];
            defaultInstance.NativeInstanceApi.vkEnumeratePhysicalDevices(defaultInstance.NativeInstance, nativePhysicalDevices).CheckResult();

            return nativePhysicalDevices[adapterOrdinal];
        }
    }
}

#endif
