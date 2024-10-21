// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
using Stride.Core;
using Stride.Core.Diagnostics;
using System.Text;
using Stride.Core.Extensions;

namespace Stride.Graphics
{
    public static partial class GraphicsAdapterFactory
    {
        private static GraphicsAdapterFactoryInstance defaultInstance;
        private static GraphicsAdapterFactoryInstance debugInstance;

        /// <summary>
        /// Initializes all adapters with the specified factory.
        /// </summary>
        internal static void InitializeInternal()
        {
            var result = vkInitialize();
            result.CheckResult();

            // Create the default instance to enumerate physical devices
            defaultInstance = new GraphicsAdapterFactoryInstance(false);
            var nativePhysicalDevices = vkEnumeratePhysicalDevices(defaultInstance.NativeInstance);

            var adapterList = new List<GraphicsAdapter>();
            for (int i = 0; i < nativePhysicalDevices.Length; i++)
            {
                var adapter = new GraphicsAdapter(nativePhysicalDevices[i], i);
                staticCollector.Add(adapter);
                adapterList.Add(adapter);
            }

            defaultAdapter = adapterList.Count > 0 ? adapterList[0] : null;
            adapters = adapterList.ToArray();

            staticCollector.Add(new AnonymousDisposable(Cleanup));
        }

        private static void Cleanup()
        {
            if (defaultInstance != null)
            {
                defaultInstance.Dispose();
                defaultInstance = null;
            }

            if (debugInstance != null)
            {
                debugInstance.Dispose();
                debugInstance = null;
            }
        }

        /// <summary>
        /// Gets the <see cref="GraphicsAdapterFactoryInstance"/> used by all GraphicsAdapter.
        /// </summary>
        internal static GraphicsAdapterFactoryInstance GetInstance(bool enableValidation)
        {
            lock (StaticLock)
            {
                Initialize();

                if (enableValidation)
                {
                    return debugInstance ?? (debugInstance = new GraphicsAdapterFactoryInstance(true));
                }
                else
                {
                    return defaultInstance;
                }
            }
        }
    }

    internal class GraphicsAdapterFactoryInstance : IDisposable
    {
        private VkDebugUtilsMessengerEXT debugReportCallback;

        internal VkInstance NativeInstance;
        internal bool HasXlibSurfaceSupport;

        // We use GraphicsDevice (similar to OpenGL)
        private static readonly Logger Log = GlobalLogger.GetLogger("GraphicsDevice");

        public unsafe GraphicsAdapterFactoryInstance(bool enableValidation)
        {
            var pEngineName = new VkUtf8ReadOnlyString("Stride"u8);
            var applicationInfo = new VkApplicationInfo
            {
                pEngineName = pEngineName,
                //engineVersion = new VkVersion()
            };

            var validationLayerNames = new List<VkUtf8String>()
            {
                VK_LAYER_KHRONOS_VALIDATION_EXTENSION_NAME,
            };
            var enabledLayerNames = new List<VkUtf8String>();

            if (enableValidation)
            {
                var layers = vkEnumerateInstanceLayerProperties();
                var availableLayerNames = new HashSet<VkUtf8String>();

                for (int index = 0; index < layers.Length; index++)
                {
                    var properties = layers[index];
                    var name = new ReadOnlySpan<byte>(properties.layerName, 256);
                    var actualName = new ReadOnlySpan<byte>(properties.layerName, name.IndexOf((byte)0) + 1);

                    availableLayerNames.Add(actualName);
                }

                enabledLayerNames.AddRange(validationLayerNames.Where(availableLayerNames.Contains));

                // Check if validation was really available
                enableValidation = enabledLayerNames.Count > 0;
            }

            vkEnumerateInstanceExtensionProperties(out uint extensionCount).CheckResult();
            var extensionProperties = new VkExtensionProperties[extensionCount];
            fixed (VkExtensionProperties* propertiesPtr = extensionProperties)
                vkEnumerateInstanceExtensionProperties(&extensionCount, propertiesPtr).CheckResult(); ;

            var availableExtensionNames = new List<VkUtf8String>();
            var desiredExtensionNames = new List<VkUtf8String>();

            for (int index = 0; index < extensionCount; index++)
            {
                var extensionProperty = extensionProperties[index];
                var name = new ReadOnlySpan<byte>(extensionProperty.extensionName, 256);
                var actualName = new ReadOnlySpan<byte>(extensionProperty.extensionName, name.IndexOf((byte)0) + 1);
                availableExtensionNames.Add(actualName);
            }

            desiredExtensionNames.Add(VK_KHR_SURFACE_EXTENSION_NAME);
            if (availableExtensionNames.Contains(VK_KHR_SURFACE_EXTENSION_NAME))
                throw new InvalidOperationException($"Required extension {Encoding.UTF8.GetString(VK_KHR_SURFACE_EXTENSION_NAME)} is not available");

            if (Platform.Type == PlatformType.Windows)
            {
                desiredExtensionNames.Add(VK_KHR_WIN32_SURFACE_EXTENSION_NAME);
                if (availableExtensionNames.Contains(VK_KHR_WIN32_SURFACE_EXTENSION_NAME))
                    throw new InvalidOperationException($"Required extension {Encoding.UTF8.GetString(VK_KHR_WIN32_SURFACE_EXTENSION_NAME)} is not available");
            }
            else if (Platform.Type == PlatformType.Android)
            {
                desiredExtensionNames.Add(VK_KHR_ANDROID_SURFACE_EXTENSION_NAME);
                if (availableExtensionNames.Contains(VK_KHR_ANDROID_SURFACE_EXTENSION_NAME))
                    throw new InvalidOperationException($"Required extension {Encoding.UTF8.GetString(VK_KHR_ANDROID_SURFACE_EXTENSION_NAME)} is not available");
            }
            else if (Platform.Type == PlatformType.Linux)
            {
                if (availableExtensionNames.Contains(VK_KHR_XLIB_SURFACE_EXTENSION_NAME))
                {
                    desiredExtensionNames.Add(VK_KHR_XLIB_SURFACE_EXTENSION_NAME);
                    HasXlibSurfaceSupport = true;
                }
                else if (availableExtensionNames.Contains(VK_KHR_XCB_SURFACE_EXTENSION_NAME))
                {
                    desiredExtensionNames.Add(VK_KHR_XCB_SURFACE_EXTENSION_NAME);
                }
                else
                {
                    throw new InvalidOperationException("None of the supported surface extensions VK_KHR_xcb_surface or VK_KHR_xlib_surface is available");
                }
            }

            bool enableDebugReport = enableValidation && availableExtensionNames.Contains(VK_EXT_DEBUG_UTILS_EXTENSION_NAME);
            if (enableDebugReport)
                desiredExtensionNames.Add(VK_EXT_DEBUG_UTILS_EXTENSION_NAME);

            using VkStringArray ppEnabledLayerNames = new(enabledLayerNames);
            using VkStringArray ppEnabledExtensionNames = new(desiredExtensionNames);

            var instanceCreateInfo = new VkInstanceCreateInfo
            {
                sType = VkStructureType.InstanceCreateInfo,
                pApplicationInfo = &applicationInfo,
                enabledLayerCount = ppEnabledLayerNames.Length,
                ppEnabledLayerNames = ppEnabledLayerNames,
                enabledExtensionCount = ppEnabledExtensionNames.Length,
                ppEnabledExtensionNames = ppEnabledExtensionNames,
            };

            vkCreateInstance(&instanceCreateInfo, null, out NativeInstance);
            vkLoadInstance(NativeInstance);

            // Check if validation layer was available (otherwise detected count is 0)
            if (enableValidation)
            {
                var createInfo = new VkDebugUtilsMessengerCreateInfoEXT
                {
                    sType = VkStructureType.DebugUtilsMessengerCreateInfoEXT,
                    messageSeverity = VkDebugUtilsMessageSeverityFlagsEXT.Verbose | VkDebugUtilsMessageSeverityFlagsEXT.Error | VkDebugUtilsMessageSeverityFlagsEXT.Warning,
                    messageType = VkDebugUtilsMessageTypeFlagsEXT.General | VkDebugUtilsMessageTypeFlagsEXT.Validation | VkDebugUtilsMessageTypeFlagsEXT.Performance,
                    pfnUserCallback = &DebugReport
                };

                vkCreateDebugUtilsMessengerEXT(NativeInstance, &createInfo, null, out debugReportCallback).CheckResult();
            }
        }

        [UnmanagedCallersOnly]
        private unsafe static uint DebugReport(VkDebugUtilsMessageSeverityFlagsEXT severity, VkDebugUtilsMessageTypeFlagsEXT types, VkDebugUtilsMessengerCallbackDataEXT* pCallbackData, void* userData)
        {
            var message = new VkUtf8String(pCallbackData->pMessage).ToString();

            // Redirect to log
            if (severity == VkDebugUtilsMessageSeverityFlagsEXT.Error)
            {
                Log.Error(message);
            }
            else if (severity == VkDebugUtilsMessageSeverityFlagsEXT.Warning)
            {
                Log.Warning(message);
            }
            else if (severity == VkDebugUtilsMessageSeverityFlagsEXT.Info)
            {
                Log.Info(message);
            }
            else if (severity == VkDebugUtilsMessageSeverityFlagsEXT.Verbose)
            {
                Log.Verbose(message);
            }

            return VK_FALSE;
        }

        public unsafe void Dispose()
        {
            if (debugReportCallback != VkDebugUtilsMessengerEXT.Null)
            {
                vkDestroyDebugUtilsMessengerEXT(NativeInstance, debugReportCallback, null);
                debugReportCallback = VkDebugUtilsMessengerEXT.Null;
            }

            vkDestroyInstance(NativeInstance, null);
        }
    }
}
#endif 
