// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
using Stride.Core;
using Stride.Core.Diagnostics;
using System.Text;

namespace Stride.Graphics
{
    public static partial class GraphicsAdapterFactory
    {
        private static GraphicsAdapterFactoryInstance defaultInstance;
        private static GraphicsAdapterFactoryInstance debugInstance;

        /// <summary>
        /// Initializes all adapters with the specified factory.
        /// </summary>
        internal static unsafe void InitializeInternal()
        {
            var result = vkInitialize();
            result.CheckResult();

            // Create the default instance to enumerate physical devices
            defaultInstance = new GraphicsAdapterFactoryInstance(false);
            uint physicalDevicesCount = 0;
            defaultInstance.NativeInstanceApi.vkEnumeratePhysicalDevices(defaultInstance.NativeInstance, &physicalDevicesCount, null).CheckResult();

            if (physicalDevicesCount == 0)
                throw new Exception("Vulkan: Failed to find GPUs with Vulkan support");

            Span<VkPhysicalDevice> nativePhysicalDevices = stackalloc VkPhysicalDevice[(int)physicalDevicesCount];
            defaultInstance.NativeInstanceApi.vkEnumeratePhysicalDevices(defaultInstance.NativeInstance, nativePhysicalDevices).CheckResult();
            
            var adapterList = new List<GraphicsAdapter>();
            for (int index = 0; index < nativePhysicalDevices.Length; index++)
            {
                VkPhysicalDeviceProperties properties;
                defaultInstance.NativeInstanceApi.vkGetPhysicalDeviceProperties(nativePhysicalDevices[index], out properties);
                var adapter = new GraphicsAdapter(nativePhysicalDevices[index], properties, index);
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
        internal VkInstanceApi NativeInstanceApi;
        internal bool HasXlibSurfaceSupport;

        // We use GraphicsDevice (similar to OpenGL)
        private static readonly Logger Log = GlobalLogger.GetLogger("GraphicsDevice");

        public unsafe GraphicsAdapterFactoryInstance(bool enableValidation)
        {
            var pEngineName = new VkUtf8ReadOnlyString("Stride"u8);
            var applicationInfo = new VkApplicationInfo
            {
                pEngineName = pEngineName,
                apiVersion = new VkVersion(1, 4, 304)
            };

            Span<VkUtf8String> validationLayerNames = stackalloc VkUtf8String[]
            {
                VK_LAYER_KHRONOS_VALIDATION_EXTENSION_NAME,
            };
            var enabledLayerNames = new List<VkUtf8String>();

            if (enableValidation)
            {
                uint count = 0;
                var callResult = vkEnumerateInstanceLayerProperties(&count, null);

                if (callResult == VkResult.Success && count > 0)
                {
                    VkLayerProperties[] layers = new VkLayerProperties[(int)count];
                    vkEnumerateInstanceLayerProperties(layers).CheckResult();

                    for (int index = 0; index < count; index++)
                    {
                        var properties = layers[index];
                        var name = new VkUtf8String(properties.layerName);
                        var indexOfLayerName = validationLayerNames.IndexOf(name);

                        if (indexOfLayerName >= 0)
                            enabledLayerNames.Add(validationLayerNames[indexOfLayerName]);
                    }

                    // Check if validation was really available
                    enableValidation = enabledLayerNames.Count > 0;
                }
            }

            var supportedExtensionNames = stackalloc VkUtf8String[]
            {
                VK_KHR_SURFACE_EXTENSION_NAME,
                VK_KHR_WIN32_SURFACE_EXTENSION_NAME,
                VK_KHR_ANDROID_SURFACE_EXTENSION_NAME,
                VK_KHR_XLIB_SURFACE_EXTENSION_NAME,
                VK_KHR_XCB_SURFACE_EXTENSION_NAME,
                VK_EXT_DEBUG_UTILS_EXTENSION_NAME
            };
            var supportedExtensions = new Span<VkUtf8String>(supportedExtensionNames, 6);
            var availableExtensionNames = GetAvailableExtensionNames(supportedExtensions);
            ValidateSurfaceExtensionNamesAvailability(availableExtensionNames);
            var desiredExtensionNames = new HashSet<VkUtf8String>
            {
                VK_KHR_SURFACE_EXTENSION_NAME,
                GetPlatformRelatedSurfaceExtensionName(availableExtensionNames)
            };

            HasXlibSurfaceSupport = desiredExtensionNames.Contains(VK_KHR_XLIB_SURFACE_EXTENSION_NAME);

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

            VkResult result = vkCreateInstance(&instanceCreateInfo, out NativeInstance);
            if (result != VK_SUCCESS)
                throw new InvalidOperationException($"Failed to create vulkan instance: {result}");

            NativeInstanceApi = GetApi(NativeInstance);

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

                NativeInstanceApi.vkCreateDebugUtilsMessengerEXT(NativeInstance, &createInfo, null, out debugReportCallback).CheckResult();
            }
        }

        private unsafe static HashSet<VkUtf8String> GetAvailableExtensionNames(Span<VkUtf8String> supportedExtensionNames)
        {
            var availableExtensionNames = new HashSet<VkUtf8String>();
            vkEnumerateInstanceExtensionProperties(out uint extensionCount).CheckResult();
            var extensionProperties = new VkExtensionProperties[extensionCount];
            vkEnumerateInstanceExtensionProperties(extensionProperties).CheckResult();

            for (int index = 0; index < extensionCount; index++)
            {
                var extensionProperty = extensionProperties[index];
                var name = new VkUtf8String(extensionProperty.extensionName).Span;
                var indexOfExtensionName = supportedExtensionNames.IndexOf(name);

                if (indexOfExtensionName >= 0)
                    availableExtensionNames.Add(supportedExtensionNames[indexOfExtensionName]);
            }

            return availableExtensionNames;
        }

        private static void ValidateSurfaceExtensionNamesAvailability(HashSet<VkUtf8String> availableExtensionNames)
        {
            if (!availableExtensionNames.Contains(VK_KHR_SURFACE_EXTENSION_NAME))
                throw new InvalidOperationException($"Required extension {Encoding.UTF8.GetString(VK_KHR_SURFACE_EXTENSION_NAME)} is not available");

            if (Platform.Type == PlatformType.Windows)
            {
                if (!availableExtensionNames.Contains(VK_KHR_WIN32_SURFACE_EXTENSION_NAME))
                    throw new InvalidOperationException($"Required extension {Encoding.UTF8.GetString(VK_KHR_WIN32_SURFACE_EXTENSION_NAME)} is not available");
            }
            else if (Platform.Type == PlatformType.Android)
            {
                if (!availableExtensionNames.Contains(VK_KHR_ANDROID_SURFACE_EXTENSION_NAME))
                    throw new InvalidOperationException($"Required extension {Encoding.UTF8.GetString(VK_KHR_ANDROID_SURFACE_EXTENSION_NAME)} is not available");
            }
            else if (Platform.Type == PlatformType.Linux)
            {
                if (!availableExtensionNames.Contains(VK_KHR_XLIB_SURFACE_EXTENSION_NAME)
                    && !availableExtensionNames.Contains(VK_KHR_XCB_SURFACE_EXTENSION_NAME))
                {
                    throw new InvalidOperationException("None of the supported surface extensions VK_KHR_xcb_surface or VK_KHR_xlib_surface is available");
                }
            }
        }

        private static VkUtf8String GetPlatformRelatedSurfaceExtensionName(HashSet<VkUtf8String> availableExtensionNames)
        {
            VkUtf8String surfaceExtensionName = VK_KHR_SURFACE_EXTENSION_NAME;

            if (Platform.Type == PlatformType.Windows)
            {
                surfaceExtensionName = VK_KHR_WIN32_SURFACE_EXTENSION_NAME;
            }
            else if (Platform.Type == PlatformType.Android)
            {
                surfaceExtensionName = VK_KHR_ANDROID_SURFACE_EXTENSION_NAME;
            }
            else if (Platform.Type == PlatformType.Linux)
            {
                if (availableExtensionNames.Contains(VK_KHR_XLIB_SURFACE_EXTENSION_NAME))
                {
                    surfaceExtensionName = VK_KHR_XLIB_SURFACE_EXTENSION_NAME;
                }
                else if (availableExtensionNames.Contains(VK_KHR_XCB_SURFACE_EXTENSION_NAME))
                {
                    surfaceExtensionName = VK_KHR_XCB_SURFACE_EXTENSION_NAME;
                }
            }

            return surfaceExtensionName;
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
                NativeInstanceApi.vkDestroyDebugUtilsMessengerEXT(NativeInstance, debugReportCallback, null);
                debugReportCallback = VkDebugUtilsMessengerEXT.Null;
            }

            NativeInstanceApi.vkDestroyInstance(NativeInstance, null);
        }
    }
}
#endif 
