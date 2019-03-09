// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_GRAPHICS_API_VULKAN
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using SharpVulkan;
using Xenko.Core;

namespace Xenko.Graphics
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
            // Create the default instance to enumerate physical devices
            defaultInstance = new GraphicsAdapterFactoryInstance(false);
            var nativePhysicalDevices = defaultInstance.NativeInstance.PhysicalDevices;

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
        private DebugReportCallback debugReportCallback;
        private DebugReportCallbackDelegate debugReport;

        internal Instance NativeInstance;
        internal bool HasXlibSurfaceSupport;

        internal BeginDebugMarkerDelegate BeginDebugMarker;
        internal EndDebugMarkerDelegate EndDebugMarker;

        public unsafe GraphicsAdapterFactoryInstance(bool enableValidation)
        {
            var applicationInfo = new ApplicationInfo
            {
                StructureType = StructureType.ApplicationInfo,
                ApiVersion = new SharpVulkan.Version(1, 0, 0),
                EngineName = Marshal.StringToHGlobalAnsi("Xenko"),
                //EngineVersion = new SharpVulkan.Version()
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
                var layers = Vulkan.InstanceLayerProperties;
                var availableLayerNames = new HashSet<string>();

                for (int index = 0; index < layers.Length; index++)
                {
                    var properties = layers[index];
                    var namePointer = new IntPtr(Interop.Fixed(ref properties.LayerName));
                    var name = Marshal.PtrToStringAnsi(namePointer);

                    availableLayerNames.Add(name);
                }

                enabledLayerNames = desiredLayerNames
                    .Where(x => availableLayerNames.Contains(x))
                    .Select(Marshal.StringToHGlobalAnsi).ToArray();
            }

            var extensionProperties = Vulkan.GetInstanceExtensionProperties();
            var availableExtensionNames = new List<string>();
            var desiredExtensionNames = new List<string>();

            for (int index = 0; index < extensionProperties.Length; index++)
            {
                var namePointer = new IntPtr(Interop.Fixed(ref extensionProperties[index].ExtensionName));
                var name = Marshal.PtrToStringAnsi(namePointer);
                availableExtensionNames.Add(name);
            }

            desiredExtensionNames.Add("VK_KHR_surface");
            if (!availableExtensionNames.Contains("VK_KHR_surface"))
                throw new InvalidOperationException("Required extension VK_KHR_surface is not available");

#if XENKO_PLATFORM_WINDOWS_DESKTOP
            desiredExtensionNames.Add("VK_KHR_win32_surface");
            if (!availableExtensionNames.Contains("VK_KHR_win32_surface"))
                throw new InvalidOperationException("Required extension VK_KHR_win32_surface is not available");

            // set OpenVR extensions required
            desiredExtensionNames.Add("VK_NV_external_memory_capabilities");

#elif XENKO_PLATFORM_ANDROID
            desiredExtensionNames.Add("VK_KHR_android_surface");
            if (!availableExtensionNames.Contains("VK_KHR_android_surface"))
                throw new InvalidOperationException("Required extension VK_KHR_android_surface is not available");
#elif XENKO_PLATFORM_LINUX
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
                    var instanceCreateInfo = new InstanceCreateInfo
                    {
                        StructureType = StructureType.InstanceCreateInfo,
                        ApplicationInfo = new IntPtr(&applicationInfo),
                        EnabledLayerCount = enabledLayerNames != null ? (uint)enabledLayerNames.Length : 0,
                        EnabledLayerNames = enabledLayerNames?.Length > 0 ? new IntPtr(Interop.Fixed(enabledLayerNames)) : IntPtr.Zero,
                        EnabledExtensionCount = (uint)enabledExtensionNames.Length,
                        EnabledExtensionNames = new IntPtr(enabledExtensionNamesPointer)
                    };

                    NativeInstance = Vulkan.CreateInstance(ref instanceCreateInfo);
                }

                if (enableDebugReport)
                {
                    var createDebugReportCallback = (CreateDebugReportCallbackDelegate)Marshal.GetDelegateForFunctionPointer(NativeInstance.GetProcAddress((byte*)createDebugReportCallbackName), typeof(CreateDebugReportCallbackDelegate));

                    debugReport = DebugReport;
                    var createInfo = new DebugReportCallbackCreateInfo
                    {
                        StructureType = StructureType.DebugReportCallbackCreateInfo,
                        Flags = (uint)(DebugReportFlags.Error | DebugReportFlags.Warning /* | DebugReportFlags.PerformanceWarning | DebugReportFlags.Information | DebugReportFlags.Debug*/),
                        Callback = Marshal.GetFunctionPointerForDelegate(debugReport)
                    };
                    createDebugReportCallback(NativeInstance, ref createInfo, null, out debugReportCallback);
                }

                if (availableExtensionNames.Contains("VK_EXT_debug_marker"))
                {
                    var beginDebugMarkerName = System.Text.Encoding.ASCII.GetBytes("vkCmdDebugMarkerBeginEXT");

                    var ptr = NativeInstance.GetProcAddress((byte*)Interop.Fixed(beginDebugMarkerName));
                    if (ptr != IntPtr.Zero)
                        BeginDebugMarker = (BeginDebugMarkerDelegate)Marshal.GetDelegateForFunctionPointer(ptr, typeof(BeginDebugMarkerDelegate));

                    var endDebugMarkerName = System.Text.Encoding.ASCII.GetBytes("vkCmdDebugMarkerEndEXT");
                    ptr = NativeInstance.GetProcAddress((byte*)Interop.Fixed(endDebugMarkerName));
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

                Marshal.FreeHGlobal(applicationInfo.EngineName);
                Marshal.FreeHGlobal(createDebugReportCallbackName);
            }
        }

        private static RawBool DebugReport(DebugReportFlags flags, DebugReportObjectType objectType, ulong @object, PointerSize location, int messageCode, string layerPrefix, string message, IntPtr userData)
        {
            Debug.WriteLine($"{flags}: {message} ([{messageCode}] {layerPrefix})");
            return true;
        }

        public unsafe void Dispose()
        {
            if (debugReportCallback != DebugReportCallback.Null)
            {
                NativeInstance.DestroyDebugReportCallback(debugReportCallback);
            }

            NativeInstance.Destroy();
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal unsafe delegate void BeginDebugMarkerDelegate(CommandBuffer commandBuffer, DebugMarkerMarkerInfo* markerInfo);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate void EndDebugMarkerDelegate(CommandBuffer commandBuffer);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private delegate RawBool DebugReportCallbackDelegate(DebugReportFlags flags, DebugReportObjectType objectType, ulong @object, PointerSize location, int messageCode, string layerPrefix, string message, IntPtr userData);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private unsafe delegate Result CreateDebugReportCallbackDelegate(Instance instance, ref DebugReportCallbackCreateInfo createInfo, AllocationCallbacks* allocator, out DebugReportCallback callback);
    }
}
#endif 
