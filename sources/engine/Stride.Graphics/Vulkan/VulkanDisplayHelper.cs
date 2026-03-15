using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Stride.Core;
using Stride.Core.Mathematics;
using Vortice.Vulkan;
using Sdl = SDL3.SDL;

namespace Stride.Graphics
{
    public static unsafe class VulkanDisplayHelper
    {
        private readonly ref struct EnumerationContext
        {
            public VkInstance Instance { get; }
            public VkInstanceApi InstanceApi { get; }
            public VkPhysicalDevice PhysicalDevice { get; }
            public Span<VkQueueFamilyProperties> QueueFamilies { get; }

            public EnumerationContext(
                VkInstance instance,
                VkInstanceApi instanceApi,
                VkPhysicalDevice physicalDevice,
                Span<VkQueueFamilyProperties> queueFamilies)
            {
                Instance = instance;
                InstanceApi = instanceApi;
                PhysicalDevice = physicalDevice;
                QueueFamilies = queueFamilies;
            }
        }

        /// <summary>
        /// Returns a list of displays the specified physical device can render/present to.
        /// </summary>
        public static List<DisplayInfo> GetDisplayInfos(
            VkInstance instance,
            VkInstanceApi instanceApi,
            VkPhysicalDevice physicalDevice)
        {
            var displayInfos = new List<DisplayInfo>();

            if (!Sdl.InitSubSystem(Sdl.InitFlags.Video))
                throw new Exception($"SDL Init failed: {Sdl.GetError()}");

            try
            {
                uint[] displays = Sdl.GetDisplays(out int displayCount);
                if (displays is null || displayCount <= 0)
                    throw new Exception($"GetDisplays failed: {Sdl.GetError()}");

                // Query queue family properties once – no heap allocation needed
                instanceApi.vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, out uint queueFamilyCount);
                Span<VkQueueFamilyProperties> queueFamilies = stackalloc VkQueueFamilyProperties[(int)queueFamilyCount];
                instanceApi.vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, queueFamilies);

                EnumerationContext context = new(instance, instanceApi, physicalDevice, queueFamilies);

                for (int index = 0; index < displayCount; index++)
                {
                    uint displayId = displays[index];

                    if (TryGetDisplayInfo(displayId, context, out var displayInfo))
                        displayInfos.Add(displayInfo);
                }
            }
            finally
            {
                Sdl.QuitSubSystem(Sdl.InitFlags.Video);
                Sdl.Quit();
            }

            return displayInfos;
        }

        private static bool TryGetDisplayInfo(
            uint displayId,
            in EnumerationContext context,
            out DisplayInfo displayInfo)
        {
            displayInfo = default!;

            if (!Sdl.GetDisplayBounds(displayId, out Sdl.Rect sdlBounds))
                return false;

            string displayName = Sdl.GetDisplayName(displayId) ?? "Unknown Display";
            nint nativeHandle = GetNativeDisplayHandle(displayId);
            Rectangle bounds = new Rectangle(sdlBounds.X, sdlBounds.Y, sdlBounds.W, sdlBounds.H);
            int winX = sdlBounds.X + sdlBounds.W / 4;
            int winY = sdlBounds.Y + sdlBounds.H / 4;

            if (!TryCreateTemporaryWindow(winX, winY, out nint windowHandle))
                return false;

            bool supportSrgb = DoesSupportSrgb(windowHandle);

            if (!TryGetCurrentDisplayMode(displayId, out DisplayMode currentMode))
                return false;

            if (!TryGetSupportedDisplayModes(displayId, supportSrgb, out List<DisplayMode> supportedModes))
                return false;

            if (!Sdl.VulkanCreateSurface(windowHandle, context.Instance, nint.Zero, out nint surfaceHandle))
            {
                Sdl.DestroyWindow(windowHandle);

                return false;
            }

            VkSurfaceKHR surface = Unsafe.As<nint, ulong>(ref surfaceHandle);
            VkSurfaceFormatKHR[] supportedFormats = default;
            bool supportsDisplay = HasGraphicsAndSurfaceSupport(context.InstanceApi, context.PhysicalDevice, surface, context.QueueFamilies);
            bool supportsAnyFormat = supportsDisplay && TryGetSurfaceFormats(context.InstanceApi, context.PhysicalDevice, surface, out supportedFormats);

            // Cleanup temporary resources
            Sdl.VulkanDestroySurface(context.Instance, surfaceHandle, nint.Zero);
            Sdl.DestroyWindow(windowHandle);

            if (!supportsAnyFormat)
                return false;

            displayInfo = new DisplayInfo(
                displayId,
                nativeHandle,
                displayName,
                bounds,
                currentMode,
                supportedModes.ToArray(),
                supportedFormats);

            return true;
        }

        private static bool TryCreateTemporaryWindow(int x, int y, out nint windowHandle)
        {
            windowHandle = Sdl.CreateWindow("Temp Vulkan Check", 128, 128, Sdl.WindowFlags.Hidden | Sdl.WindowFlags.Vulkan);

            bool createdWindow = windowHandle != nint.Zero;
            if (createdWindow)
                Sdl.SetWindowPosition(windowHandle, x, y);

            return createdWindow;
        }

        private static bool DoesSupportSrgb(nint window)
        {
            Sdl.Colorspace colorspace = Sdl.GetSurfaceColorspace(window);

            return colorspace == Sdl.Colorspace.SRGB || colorspace == Sdl.Colorspace.SRGBLinear;
        }

        private static bool TryGetCurrentDisplayMode(uint displayId, out DisplayMode currentMode)
        {
            currentMode = default;

            Sdl.DisplayMode? currentSdlMode = Sdl.GetCurrentDisplayMode(displayId);
            if (!currentSdlMode.HasValue)
                return false;

            return TryMapToStrideDisplayMode(currentSdlMode.Value, out currentMode);
        }

        private static bool TryGetSupportedDisplayModes(uint displayId, bool supportsSrgb, out List<DisplayMode> supportedModes)
        {
            supportedModes = new List<DisplayMode>();

            Sdl.DisplayMode[] supportedSdlModes = Sdl.GetFullscreenDisplayModes(displayId, out int modeCount) ?? [];
            if (modeCount <= 0)
                return false;

            for (int index = 0; index < modeCount; index++)
            {
                Sdl.DisplayMode sdlMode = supportedSdlModes[index];

                if (TryMapToStrideDisplayMode(sdlMode, out DisplayMode strideMode))
                {
                    supportedModes.Add(strideMode);

                    if (supportsSrgb && strideMode.Format.TryGetSrgbEquivalent(out PixelFormat strideFormat))
                    {
                        DisplayMode srgbEquivalent = new DisplayMode()
                        {
                            Format = strideFormat,
                            Width = strideMode.Width,
                            Height = strideMode.Height,
                            RefreshRate = strideMode.RefreshRate,
                        };

                        supportedModes.Add(srgbEquivalent);
                    }
                }
            }

            return true;
        }

        private static bool HasGraphicsAndSurfaceSupport(
            VkInstanceApi instanceApi,
            VkPhysicalDevice physicalDevice,
            VkSurfaceKHR surface,
            Span<VkQueueFamilyProperties> queueFamilies)
        {
            for (int index = 0; index < queueFamilies.Length; index++)
            {
                ref readonly VkQueueFamilyProperties properties = ref queueFamilies[index];
                bool supportsGraphicsOperations = (properties.queueFlags & VkQueueFlags.Graphics) != 0;
                if (supportsGraphicsOperations)
                {
                    bool callSucceeded = instanceApi.vkGetPhysicalDeviceSurfaceSupportKHR(physicalDevice, (uint)index, surface, out var isSurfaceSupported) == VkResult.Success;
                    if (callSucceeded && isSurfaceSupported)
                        return true;
                }
            }

            return false;
        }

        private static bool TryGetSurfaceFormats(
            VkInstanceApi instanceApi,
            VkPhysicalDevice physicalDevice,
            VkSurfaceKHR surface,
            out VkSurfaceFormatKHR[] surfaceFormats)
        {
            surfaceFormats = default;
            instanceApi.vkGetPhysicalDeviceSurfaceFormatsKHR(physicalDevice, surface, out uint formatCount);

            if (formatCount == 0)
                return false;

            Span<VkSurfaceFormatKHR> formats = stackalloc VkSurfaceFormatKHR[(int)formatCount];
            instanceApi.vkGetPhysicalDeviceSurfaceFormatsKHR(physicalDevice, surface, formats);
            surfaceFormats = formats.ToArray();

            return true;
        }

        private static nint GetNativeDisplayHandle(uint displayId)
        {
            uint props = Sdl.GetDisplayProperties(displayId);
            nint handle = nint.Zero;

            if (Platform.Type == PlatformType.Windows)
            {
                handle = Sdl.GetPointerProperty(props, "SDL.display.windows.hmonitor", nint.Zero);
            }
            else if (Platform.Type == PlatformType.Linux)
            {
                // Wayland: wl_output*; no equivalent per-display native handle exists for X11 in SDL3
                handle = Sdl.GetPointerProperty(props, "SDL.display.wayland.wl_output", nint.Zero);
            }

            return handle;
        }

        private static bool TryMapToStrideDisplayMode(Sdl.DisplayMode sdlMode, out DisplayMode strideMode)
        {
            strideMode = default;

            if (sdlMode.Format.TryMapPixelFormat(out PixelFormat strideFormat))
            {
                Rational refreshRate = new Rational(sdlMode.RefreshRateNumerator, sdlMode.RefreshRateDenominator);

                // If no exact match, fall back to a safe/common format (widely supported in Vulkan/DirectX swapchains)
                if (strideFormat == PixelFormat.None)
                    strideFormat = PixelFormat.B8G8R8A8_UNorm;

                strideMode = new DisplayMode(
                    strideFormat,
                    sdlMode.W,
                    sdlMode.H,
                    refreshRate);

                return true;
            }

            return false;
        }
    }
}
