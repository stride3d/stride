// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Stride.Core;
using Stride.Core.Diagnostics;


namespace Stride.Graphics
{
    internal class GraphicsAdapterFactoryInstance : IDisposable
    {
        internal Instance NativeInstance;
        internal bool HasXlibSurfaceSupport;
        internal Vk vk;

        // We use GraphicsDevice (similar to OpenGL)
        private static readonly Logger Log = GlobalLogger.GetLogger("GraphicsDevice");

        public unsafe GraphicsAdapterFactoryInstance(bool enableValidation)
        {
            vk = Vk.GetApi();

            var applicationInfo = new ApplicationInfo
            {
                SType = StructureType.ApplicationInfo,
                ApiVersion = Vk.Version10,
                PEngineName = (byte*)Marshal.StringToHGlobalAnsi("Stride")
            };

            var validationLayerNames = new[]
            {
                "VK_LAYER_KHRONOS_validation",
            };

            IntPtr[] enabledLayerNames = [];

            if (enableValidation)
            {
                uint layerCount = 0;
                vk.EnumerateInstanceLayerProperties(ref layerCount, null);

                Span<LayerProperties> layers = stackalloc LayerProperties[(int)layerCount];

                vk.EnumerateInstanceLayerProperties(&layerCount, layers);

                var availableLayerNames = new HashSet<string>();

                for (int index = 0; index < layers.Length; index++)
                {
                    var properties = layers[index];
                    var namePointer = properties.LayerName;
                    var name = Marshal.PtrToStringAnsi((IntPtr)namePointer);

                    availableLayerNames.Add(name);
                }

                enabledLayerNames = validationLayerNames
                    .Where(availableLayerNames.Contains)
                    .Select(Marshal.StringToHGlobalAnsi).ToArray();

                // Check if validation was really available
                enableValidation = enabledLayerNames.Length > 0;
            }

            var enabledExtensionNames = SetEnabledExtensionName(enableValidation);

            try
            {
                fixed (void* enabledExtensionNamesPointer = &enabledExtensionNames[0])
                fixed (void* fEnabledLayerNames = enabledLayerNames) // null if array is empty or null
                {
                    var instanceCreateInfo = new InstanceCreateInfo
                    {
                        SType = StructureType.InstanceCreateInfo,
                        PApplicationInfo = &applicationInfo,
                        EnabledLayerCount = enabledLayerNames != null ? (uint)enabledLayerNames.Length : 0,
                        PpEnabledLayerNames = (byte**)fEnabledLayerNames,
                        EnabledExtensionCount = (uint)enabledExtensionNames.Length,
                        PpEnabledExtensionNames = (byte**)enabledExtensionNamesPointer,
                    };

                    vk.CreateInstance(&instanceCreateInfo, null, out NativeInstance);

                }
                // Check if validation layer was available (otherwise detected count is 0)
                if (enableValidation && vk.TryGetInstanceExtension(NativeInstance, out ExtDebugUtils debugUtils))
                {
                    var createInfo = new DebugUtilsMessengerCreateInfoEXT
                    {
                        SType = StructureType.DebugUtilsMessengerCreateInfoExt,
                        MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt | DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt | DebugUtilsMessageSeverityFlagsEXT.WarningBitExt,
                        MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt | DebugUtilsMessageTypeFlagsEXT.ValidationBitExt | DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt,
                        PfnUserCallback = new PfnDebugUtilsMessengerCallbackEXT(DebugReport),
                    };

                    debugUtils.CreateDebugUtilsMessenger(NativeInstance, &createInfo, null, out _);
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

                Marshal.FreeHGlobal((IntPtr)applicationInfo.PEngineName);
            }
        }

        private unsafe nint[] SetEnabledExtensionName(bool enableValidation)
        {
            uint extCount = 0;
            vk.EnumerateInstanceExtensionProperties((byte*)null, ref extCount, null);
            Span<ExtensionProperties> extensionProperties = stackalloc ExtensionProperties[(int)extCount];
            
            vk.EnumerateInstanceExtensionProperties("", &extCount, extensionProperties);
            
            var availableExtensionNames = new List<string>();
            var desiredExtensionNames = new List<string>();

            for (int index = 0; index < extensionProperties.Length; index++)
            {
                var extensionProperty = extensionProperties[index];
                var name = Marshal.PtrToStringAnsi((IntPtr)extensionProperty.ExtensionName);
                availableExtensionNames.Add(name);
            }

            desiredExtensionNames.Add(KhrSurface.ExtensionName);
            desiredExtensionNames.Add(KhrGetSurfaceCapabilities2.ExtensionName);
            if (enableValidation)
                desiredExtensionNames.Add("VK_EXT_debug_utils");
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
                if (availableExtensionNames.Contains(KhrWaylandSurface.ExtensionName))
                {
                    desiredExtensionNames.Add(KhrWaylandSurface.ExtensionName);
                }  
                if (availableExtensionNames.Contains(KhrXlibSurface.ExtensionName))
                {
                    desiredExtensionNames.Add(KhrXlibSurface.ExtensionName);
                    HasXlibSurfaceSupport = true;
                }
                else if (availableExtensionNames.Contains(KhrXcbSurface.ExtensionName))
                {
                    desiredExtensionNames.Add(KhrXcbSurface.ExtensionName);
                }
                else
                {
                    throw new InvalidOperationException("None of the supported surface extensions VK_KHR_xcb_surface, VK_KHR_wayland_surface or VK_KHR_xlib_surface is available");
                }
            }
            
            bool enableDebugReport = enableValidation && availableExtensionNames.Contains(ExtDebugUtils.ExtensionName);
            if (enableDebugReport)
                desiredExtensionNames.Add(ExtDebugUtils.ExtensionName);

            var enabledExtensionNames = desiredExtensionNames.Select(Marshal.StringToHGlobalAnsi).ToArray();
            return enabledExtensionNames;
        }

        private static unsafe uint DebugReport(DebugUtilsMessageSeverityFlagsEXT severity, DebugUtilsMessageTypeFlagsEXT types, DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* userData)
        {
            var message = new string((sbyte*)pCallbackData->PMessage);

            // Redirect to log
            if (severity == DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt)
            {
                Log.Error(message);
            }
            else if (severity == DebugUtilsMessageSeverityFlagsEXT.WarningBitExt)
            {
                Log.Warning(message);
            }
            else if (severity == DebugUtilsMessageSeverityFlagsEXT.InfoBitExt)
            {
                Log.Info(message);
            }
            else if (severity == DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt)
            {
                Log.Verbose(message);
            }

            return Vk.False;
        }

        public unsafe void Dispose()
        {
            vk.DestroyInstance(NativeInstance, null);
        }
    }
}
#endif 
