// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using System.Collections.Generic;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
using Stride.Core;

using ComponentBase = Stride.Core.ComponentBase;
using Utilities = Stride.Core.Utilities;

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
        private VkPhysicalDevice debugPhysicalDevice;

        private readonly int adapterOrdinal;
        private readonly VkPhysicalDeviceProperties properties;

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
        internal GraphicsAdapter(VkPhysicalDevice defaultPhysicalDevice, int adapterOrdinal)
        {
            this.adapterOrdinal = adapterOrdinal;
            this.defaultPhysicalDevice = defaultPhysicalDevice;

            vkGetPhysicalDeviceProperties(defaultPhysicalDevice, out properties);

            // TODO VULKAN
            //var displayProperties = physicalDevice.DisplayProperties;
            //outputs = new GraphicsOutput[displayProperties.Length];
            //for (var i = 0; i < outputs.Length; i++)
            //    outputs[i] = new GraphicsOutput(this, displayProperties[i], i).DisposeBy(this);
            outputs = new[] { new GraphicsOutput() };
        }

        /// <summary>
        /// Gets the description of this adapter.
        /// </summary>
        /// <value>The description.</value>
        public unsafe string Description
        {
            get
            {
                // TODO VULKAN api
                var propertiesCopy = properties;

                var description = Marshal.PtrToStringAnsi((IntPtr)propertiesCopy.deviceName);
                if (VendorNames.TryGetValue(VendorId, out var vendorName))
                    description = $"{vendorName} {description}";

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
                return (int)properties.vendorID;
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

        internal VkPhysicalDevice GetPhysicalDevice(bool enableValidation)
        {
            if (enableValidation)
            {
                if (debugPhysicalDevice == VkPhysicalDevice.Null)
                {
                    debugPhysicalDevice = vkEnumeratePhysicalDevices(GraphicsAdapterFactory.GetInstance(true).NativeInstance).ToArray()[adapterOrdinal];
                }

                return debugPhysicalDevice;
            }
            else
            {
                return defaultPhysicalDevice;
            }
        }

        /// <summary>
        /// Tests to see if the adapter supports the requested profile.
        /// </summary>
        /// <param name="graphicsProfile">The graphics profile.</param>
        /// <returns>true if the profile is supported</returns>
        public bool IsProfileSupported(GraphicsProfile graphicsProfile)
        {
            // TODO VULKAN
            return true;
            //return SharpDX.Direct3D11.Device.IsSupportedFeatureLevel(this.NativeAdapter, (SharpDX.Direct3D.FeatureLevel)graphicsProfile);
        }
    }
} 
#endif
