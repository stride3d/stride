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
        private VkDebugReportCallbackEXT debugReportCallback;
        private DebugReportCallbackDelegate debugReport;

        internal VkInstance NativeInstance;
        internal bool HasXlibSurfaceSupport;

        internal BeginDebugMarkerDelegate BeginDebugMarker;
        internal EndDebugMarkerDelegate EndDebugMarker;

        public unsafe GraphicsAdapterFactoryInstance(bool enableValidation)
        {
            var applicationInfo = new VkApplicationInfo
            {
                sType = VkStructureType.ApplicationInfo,
                apiVersion = new VkVersion(1, 0, 0),
                pEngineName = (byte*)Marshal.StringToHGlobalAnsi("Stride"),
                //engineVersion = new VkVersion()
            };

            var desiredLayerNames = new[]
            {
                    //"VK_LAYER_LUNARG_standard_validation",
                    "VK_LAYER_GOOGLE_threading",
                    "VK_LAYER_LUNARG_parameter_validation",
                    "VK_LAYER_LUNARG_device_limits",
                    "VK_LAYER_LUNARG_object_tracker",
                    "VK_LAYER_LUNARG_image",
                    "VK_LAYER_LUNARG_core_validation",
                    "VK_LAYER_LUNARG_swapchain",
                    "VK_LAYER_GOOGLE_unique_objects",
                    //"VK_LAYER_LUNARG_api_dump",
                    //"VK_LAYER_LUNARG_vktrace"
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

                enabledLayerNames = desiredLayerNames
                    .Where(x => availableLayerNames.Contains(x))
                    .Select(Marshal.StringToHGlobalAnsi).ToArray();
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

            desiredExtensionNames.Add("VK_KHR_surface");
            if (!availableExtensionNames.Contains("VK_KHR_surface"))
                throw new InvalidOperationException("Required extension VK_KHR_surface is not available");

#if STRIDE_PLATFORM_WINDOWS_DESKTOP
            desiredExtensionNames.Add("VK_KHR_win32_surface");
            if (!availableExtensionNames.Contains("VK_KHR_win32_surface"))
                throw new InvalidOperationException("Required extension VK_KHR_win32_surface is not available");
#elif STRIDE_PLATFORM_ANDROID
                desiredExtensionNames.Add("VK_KHR_android_surface");
                if (!availableExtensionNames.Contains("VK_KHR_android_surface"))
                    throw new InvalidOperationException("Required extension VK_KHR_android_surface is not available");
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
            bool enableDebugReport = enableValidation && availableExtensionNames.Contains("VK_EXT_debug_report");
            if (enableDebugReport)
                desiredExtensionNames.Add("VK_EXT_debug_report");

            var enabledExtensionNames = desiredExtensionNames.Select(Marshal.StringToHGlobalAnsi).ToArray();

            var createDebugReportCallbackName = Marshal.StringToHGlobalAnsi("vkCreateDebugReportCallbackEXT");

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

                if (enableDebugReport)
                {
                    var createDebugReportCallback = (CreateDebugReportCallbackDelegate)Marshal.GetDelegateForFunctionPointer(vkGetInstanceProcAddr(NativeInstance, (byte*)createDebugReportCallbackName), typeof(CreateDebugReportCallbackDelegate));

                    debugReport = DebugReport;
                    var createInfo = new VkDebugReportCallbackCreateInfoEXT
                    {
                        sType = VkStructureType.DebugReportCallbackCreateInfoEXT,
                        flags = VkDebugReportFlagsEXT.ErrorEXT | VkDebugReportFlagsEXT.WarningEXT /* | VkDebugReportFlagsEXT.PerformanceWarningEXT | VkDebugReportFlagsEXT.InformationEXT | VkDebugReportFlagsEXT.DebugEXT*/,
                        pfnCallback = Marshal.GetFunctionPointerForDelegate(debugReport)
                    };
                    createDebugReportCallback(NativeInstance, ref createInfo, null, out debugReportCallback);
                }

                if (availableExtensionNames.Contains("VK_EXT_debug_marker"))
                {
                    var beginDebugMarkerName = System.Text.Encoding.ASCII.GetBytes("vkCmdDebugMarkerBeginEXT");

                    var ptr = vkGetInstanceProcAddr(NativeInstance, (byte*)Core.Interop.Fixed(beginDebugMarkerName));
                    if (ptr != IntPtr.Zero)
                        BeginDebugMarker = (BeginDebugMarkerDelegate)Marshal.GetDelegateForFunctionPointer(ptr, typeof(BeginDebugMarkerDelegate));

                    var endDebugMarkerName = System.Text.Encoding.ASCII.GetBytes("vkCmdDebugMarkerEndEXT");
                    ptr = vkGetInstanceProcAddr(NativeInstance, (byte*)Core.Interop.Fixed(endDebugMarkerName));
                    if (ptr != IntPtr.Zero)
                        EndDebugMarker = (EndDebugMarkerDelegate)Marshal.GetDelegateForFunctionPointer(ptr, typeof(EndDebugMarkerDelegate));
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
                Marshal.FreeHGlobal(createDebugReportCallbackName);
            }
        }

        private static VkBool32 DebugReport(VkDebugReportFlagsEXT flags, VkDebugReportObjectTypeEXT objectType, ulong @object, VkPointerSize location, int messageCode, string layerPrefix, string message, IntPtr userData)
        {
            Debug.WriteLine($"{flags}: {message} ([{messageCode}] {layerPrefix})");
            return false;
        }

        public unsafe void Dispose()
        {
            if (debugReportCallback != VkDebugReportCallbackEXT.Null)
            {
                vkDestroyDebugReportCallbackEXT(NativeInstance, debugReportCallback, null);
            }

            vkDestroyInstance(NativeInstance, null);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal unsafe delegate void BeginDebugMarkerDelegate(VkCommandBuffer commandBuffer, VkDebugMarkerMarkerInfoEXT* markerInfo);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate void EndDebugMarkerDelegate(VkCommandBuffer commandBuffer);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private delegate VkBool32 DebugReportCallbackDelegate(VkDebugReportFlagsEXT flags, VkDebugReportObjectTypeEXT objectType, ulong @object, VkPointerSize location, int messageCode, string layerPrefix, string message, IntPtr userData);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private unsafe delegate VkResult CreateDebugReportCallbackDelegate(VkInstance instance, ref VkDebugReportCallbackCreateInfoEXT createInfo, VkAllocationCallbacks* allocator, out VkDebugReportCallbackEXT callback);
    }
}
#endif 
