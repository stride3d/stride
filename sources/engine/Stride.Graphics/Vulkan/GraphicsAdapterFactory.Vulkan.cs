// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
using Stride.Core;
using Stride.Core.Diagnostics;

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
        private vkDebugUtilsMessengerCallbackEXT debugReport;

        internal VkInstance NativeInstance;
        internal bool HasXlibSurfaceSupport;

        // We use GraphicsDevice (similar to OpenGL)
        private static readonly Logger Log = GlobalLogger.GetLogger("GraphicsDevice");

        public unsafe GraphicsAdapterFactoryInstance(bool enableValidation)
        {
            var applicationInfo = new VkApplicationInfo
            {
                sType = VkStructureType.ApplicationInfo,
                apiVersion = new VkVersion(1, 0, 0),
                pEngineName = (byte*)Marshal.StringToHGlobalAnsi("Stride"),
                //engineVersion = new VkVersion()
            };

            var validationLayerNames = new[]
            {
                "VK_LAYER_KHRONOS_validation",
            };

            IntPtr[] enabledLayerNames = new IntPtr[0];

            if (enableValidation)
            {
                var layers = vkEnumerateInstanceLayerProperties();
                var availableLayerNames = new HashSet<string>();

                for (int index = 0; index < layers.Length; index++)
                {
                    var properties = layers[index];
                    var namePointer = properties.layerName;
                    var name = Marshal.PtrToStringAnsi((IntPtr)namePointer);

                    availableLayerNames.Add(name);
                }

                enabledLayerNames = validationLayerNames
                    .Where(x => availableLayerNames.Contains(x))
                    .Select(Marshal.StringToHGlobalAnsi).ToArray();

                // Check if validation was really available
                enableValidation = enabledLayerNames.Length > 0;
            }

            var extensionProperties = vkEnumerateInstanceExtensionProperties();
            var availableExtensionNames = new List<string>();
            var desiredExtensionNames = new List<string>();

            for (int index = 0; index < extensionProperties.Length; index++)
            {
                var extensionProperty = extensionProperties[index];
                var name = Marshal.PtrToStringAnsi((IntPtr)extensionProperty.extensionName);
                availableExtensionNames.Add(name);
            }

            desiredExtensionNames.Add(KHRSurfaceExtensionName);
            if (!availableExtensionNames.Contains(KHRSurfaceExtensionName))
                throw new InvalidOperationException($"Required extension {KHRSurfaceExtensionName} is not available");

#if STRIDE_PLATFORM_WINDOWS_DESKTOP
            desiredExtensionNames.Add(KHRWin32SurfaceExtensionName);
            if (!availableExtensionNames.Contains(KHRWin32SurfaceExtensionName))
                throw new InvalidOperationException($"Required extension {KHRWin32SurfaceExtensionName} is not available");
#elif STRIDE_PLATFORM_ANDROID
            desiredExtensionNames.Add(KHRAndroidSurfaceExtensionName);
            if (!availableExtensionNames.Contains(KHRAndroidSurfaceExtensionName))
                throw new InvalidOperationException($"Required extension {KHRAndroidSurfaceExtensionName} is not available");
#elif STRIDE_PLATFORM_LINUX
            if (availableExtensionNames.Contains("VK_KHR_xlib_surface"))
            {
                desiredExtensionNames.Add("VK_KHR_xlib_surface");
                HasXlibSurfaceSupport = true;
            }
            else if (availableExtensionNames.Contains("VK_KHR_xcb_surface"))
            {
                desiredExtensionNames.Add("VK_KHR_xcb_surface");
            }
            else
            {
                throw new InvalidOperationException("None of the supported surface extensions VK_KHR_xcb_surface or VK_KHR_xlib_surface is available");
            }
#endif
            bool enableDebugReport = enableValidation && availableExtensionNames.Contains(EXTDebugUtilsExtensionName);
            if (enableDebugReport)
                desiredExtensionNames.Add(EXTDebugUtilsExtensionName);

            var enabledExtensionNames = desiredExtensionNames.Select(Marshal.StringToHGlobalAnsi).ToArray();

            try
            {
                fixed (void* enabledExtensionNamesPointer = &enabledExtensionNames[0])
                {
                    var instanceCreateInfo = new VkInstanceCreateInfo
                    {
                        sType = VkStructureType.InstanceCreateInfo,
                        pApplicationInfo = &applicationInfo,
                        enabledLayerCount = enabledLayerNames != null ? (uint)enabledLayerNames.Length : 0,
                        ppEnabledLayerNames = enabledLayerNames?.Length > 0 ? (byte**)Core.Interop.Fixed(enabledLayerNames) : null,
                        enabledExtensionCount = (uint)enabledExtensionNames.Length,
                        ppEnabledExtensionNames = (byte**)enabledExtensionNamesPointer,
                    };

                    vkCreateInstance(&instanceCreateInfo, null, out NativeInstance);
                    vkLoadInstance(NativeInstance);
                }

                // Check if validation layer was available (otherwise detected count is 0)
                if (enableValidation)
                {
                    debugReport = DebugReport;
                    var createInfo = new VkDebugUtilsMessengerCreateInfoEXT
                    {
                        sType = VkStructureType.DebugUtilsMessengerCreateInfoEXT,
                        messageSeverity = VkDebugUtilsMessageSeverityFlagsEXT.VerboseEXT | VkDebugUtilsMessageSeverityFlagsEXT.ErrorEXT | VkDebugUtilsMessageSeverityFlagsEXT.WarningEXT,
                        messageType = VkDebugUtilsMessageTypeFlagsEXT.GeneralEXT | VkDebugUtilsMessageTypeFlagsEXT.ValidationEXT | VkDebugUtilsMessageTypeFlagsEXT.PerformanceEXT,
                        pfnUserCallback = Marshal.GetFunctionPointerForDelegate(debugReport)
                    };

                    vkCreateDebugUtilsMessengerEXT(NativeInstance, &createInfo, null, out debugReportCallback).CheckResult();
                }
            }
            finally
            {
                foreach (var enabledExtensionName in enabledExtensionNames)
                {
                    Marshal.FreeHGlobal(enabledExtensionName);
                }

                foreach (var enabledLayerName in enabledLayerNames)
                {
                    Marshal.FreeHGlobal(enabledLayerName);
                }

                Marshal.FreeHGlobal((IntPtr)applicationInfo.pEngineName);
            }
        }

        private unsafe static VkBool32 DebugReport(VkDebugUtilsMessageSeverityFlagsEXT severity, VkDebugUtilsMessageTypeFlagsEXT types, VkDebugUtilsMessengerCallbackDataEXT* pCallbackData, IntPtr userData)
        {
            var message = Vortice.Vulkan.Interop.GetString(pCallbackData->pMessage);

            // Redirect to log
            if (severity == VkDebugUtilsMessageSeverityFlagsEXT.ErrorEXT)
            {
                Log.Error(message);
            }
            else if (severity == VkDebugUtilsMessageSeverityFlagsEXT.WarningEXT)
            {
                Log.Warning(message);
            }
            else if (severity == VkDebugUtilsMessageSeverityFlagsEXT.InfoEXT)
            {
                Log.Info(message);
            }
            else if (severity == VkDebugUtilsMessageSeverityFlagsEXT.VerboseEXT)
            {
                Log.Verbose(message);
            }

            return false;
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
