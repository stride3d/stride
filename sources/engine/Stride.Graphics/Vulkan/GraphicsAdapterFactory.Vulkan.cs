// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Stride.Core;
using Stride.Core.Diagnostics;

using Vk = Silk.NET.Vulkan;


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
            //var result = Vk.

            // TODO : CHeck result
            //result.CheckResult();


            // Create the default instance to enumerate physical devices
            defaultInstance = new GraphicsAdapterFactoryInstance(false);
            Vk.PhysicalDevice[] nativePhysicalDevices = null;
            unsafe
            {

                fixed(Vk.PhysicalDevice* d = nativePhysicalDevices)
                    Vk.Vk.GetApi().EnumeratePhysicalDevices(defaultInstance.NativeInstance, null, d);
            }

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
                    return debugInstance ??= new GraphicsAdapterFactoryInstance(true);
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
        private Vk.DebugUtilsMessengerCallbackDataEXT debugReportCallback;
        private Vk.DebugUtilsMessengerEXT debugReport;

        internal Vk.Instance NativeInstance;
        internal bool HasXlibSurfaceSupport;

        // We use GraphicsDevice (similar to OpenGL)
        private static readonly Logger Log = GlobalLogger.GetLogger("GraphicsDevice");

        public unsafe GraphicsAdapterFactoryInstance(bool enableValidation)
        {
            var applicationInfo = new Vk.ApplicationInfo
            {
                SType = Vk.StructureType.ApplicationInfo,
                ApiVersion = 1,
                PEngineName = (byte*)Marshal.StringToHGlobalAnsi("Stride"),
                //engineVersion = new Vk.Version()
            };

            var validationLayerNames = new[]
            {
                "VK_LAYER_KHRONOS_validation",
            };

            IntPtr[] enabledLayerNames = new IntPtr[0];

            if (enableValidation)
            {
                LayerProperties[] layers = null;

                fixed (LayerProperties* l = layers)
                    Vk.Vk.GetApi().EnumerateInstanceLayerProperties(null, l);

                var availableLayerNames = new HashSet<string>();

                for (int index = 0; index < layers.Length; index++)
                {
                    var properties = layers[index];
                    var namePointer = properties.LayerName;
                    var name = Marshal.PtrToStringAnsi((IntPtr)namePointer);

                    availableLayerNames.Add(name);
                }

                enabledLayerNames = validationLayerNames
                    .Where(x => availableLayerNames.Contains(x))
                    .Select(Marshal.StringToHGlobalAnsi).ToArray();

                // Check if validation was really available
                enableValidation = enabledLayerNames.Length > 0;
            }

            ExtensionProperties[] extensionProperties = null;
            fixed(ExtensionProperties* e = extensionProperties)
                Vk.Vk.GetApi().EnumerateInstanceExtensionProperties("", null, e);
            var availableExtensionNames = new List<string>();
            var desiredExtensionNames = new List<string>();

            for (int index = 0; index < extensionProperties.Length; index++)
            {
                var extensionProperty = extensionProperties[index];
                var name = Marshal.PtrToStringAnsi((IntPtr)extensionProperty.ExtensionName);
                availableExtensionNames.Add(name);
            }

            desiredExtensionNames.Add(KhrSurface.ExtensionName);
            if (!availableExtensionNames.Contains(KhrSurface.ExtensionName))
                throw new InvalidOperationException($"Required extension {KhrSurface.ExtensionName} is not available");

            if (Platform.Type == PlatformType.Windows)
            {
                desiredExtensionNames.Add(KhrWin32Surface.ExtensionName);
                if (!availableExtensionNames.Contains(KhrWin32Surface.ExtensionName))
                    throw new InvalidOperationException($"Required extension {KhrWin32Surface.ExtensionName} is not available");
            }
            else if (Platform.Type == PlatformType.Android)
            {
                desiredExtensionNames.Add(KhrAndroidSurface.ExtensionName);
                if (!availableExtensionNames.Contains(KhrAndroidSurface.ExtensionName))
                    throw new InvalidOperationException($"Required extension {KhrAndroidSurface.ExtensionName} is not available");
            }
            else if (Platform.Type == PlatformType.Linux)
            {
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
            }

            bool enableDebugReport = enableValidation && availableExtensionNames.Contains(ExtDebugUtils.ExtensionName);
            if (enableDebugReport)
                desiredExtensionNames.Add(ExtDebugUtils.ExtensionName);

            var enabledExtensionNames = desiredExtensionNames.Select(Marshal.StringToHGlobalAnsi).ToArray();

            try
            {
                fixed (void* enabledExtensionNamesPointer = &enabledExtensionNames[0])
                {
                    var instanceCreateInfo = new Vk.InstanceCreateInfo
                    {
                        SType = Vk.StructureType.InstanceCreateInfo,
                        PApplicationInfo = &applicationInfo,
                        EnabledLayerCount = enabledLayerNames != null ? (uint)enabledLayerNames.Length : 0,
                        PpEnabledLayerNames = enabledLayerNames?.Length > 0 ? (byte**)Core.Interop.Fixed(enabledLayerNames) : null,
                        EnabledExtensionCount = (uint)enabledExtensionNames.Length,
                        PpEnabledExtensionNames = (byte**)enabledExtensionNamesPointer,
                    };

                    Vk.Vk.GetApi().CreateInstance(&instanceCreateInfo, null, out NativeInstance);
                    //vkLoadInstance(NativeInstance);

                }

                // Check if validation layer was available (otherwise detected count is 0)
                if (enableValidation)
                {
                    debugReport = DebugReport;
                    var createInfo = new Vk.DebugUtilsMessengerCreateInfoEXT
                    {
                        sType = Vk.StructureType.DebugUtilsMessengerCreateInfoEXT,
                        messageSeverity = Vk.DebugUtilsMessageSeverityFlagsEXT.Verbose | Vk.DebugUtilsMessageSeverityFlagsEXT.Error | Vk.DebugUtilsMessageSeverityFlagsEXT.Warning,
                        messageType = Vk.DebugUtilsMessageTypeFlagsEXT.General | Vk.DebugUtilsMessageTypeFlagsEXT.Validation | Vk.DebugUtilsMessageTypeFlagsEXT.Performance,
                        pfnUserCallback = Marshal.GetFunctionPointerForDelegate(debugReport)
                    };

                    Vk.CreateDebugUtilsMessengerEXT(NativeInstance, &createInfo, null, out debugReportCallback).CheckResult();
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

        private unsafe static bool DebugReport(Vk.DebugUtilsMessageSeverityFlagsEXT severity, Vk.DebugUtilsMessageTypeFlagsEXT types, Vk.DebugUtilsMessengerCallbackDataEXT* pCallbackData, IntPtr userData)
        {
            var message = Vortice.Vulkan.Interop.GetString(pCallbackData->PMessage);

            // Redirect to log
            if (severity == Vk.DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityErrorBitExt)
            {
                Log.Error(message);
            }
            else if (severity == Vk.DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityWarningBitExt)
            {
                Log.Warning(message);
            }
            else if (severity == Vk.DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityInfoBitExt)
            {
                Log.Info(message);
            }
            else if (severity == Vk.DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityVerboseBitExt)
            {
                Log.Verbose(message);
            }

            return false;
        }

        public unsafe void Dispose()
        {
            //if (debugReportCallback != Vk.DebugUtilsMessengerEXT)
            //{
            //    Vk.DestroyDebugUtilsMessengerEXT(NativeInstance, debugReportCallback, null);
            //    debugReportCallback = Vk.DebugUtilsMessengerEXT.Null;
            //}

            Vk.Vk.GetApi().DestroyInstance(NativeInstance, null);
        }
    }
}
#endif 
