// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using System.Linq;
using Vortice.Vulkan;

namespace Stride.Graphics
{
    /// <summary>
    /// Graphics presenter for SwapChain.
    /// </summary>
    public class SwapChainGraphicsPresenter : GraphicsPresenter
    {
        private VkSwapchainKHR swapChain;
        private VkSurfaceKHR surface;

        private Texture backbuffer;
        private SwapChainImageInfo[] swapchainImages;
        private uint currentBufferIndex;

        private struct SwapChainImageInfo
        {
            public VkImage NativeImage;
            public VkImageView NativeColorAttachmentView;
        }

        public SwapChainGraphicsPresenter(GraphicsDevice device, PresentationParameters presentationParameters)
            : base(device, presentationParameters)
        {
            PresentInterval = presentationParameters.PresentationInterval;

            backbuffer = new Texture(device);

            CreateSurface();

            // Initialize the swap chain
            CreateSwapChain();
        }

        public override Texture BackBuffer
        {
            get
            {
                return backbuffer;
            }
        }

        public override object NativePresenter
        {
            get
            {
                return swapChain;
            }
        }

        public override bool IsFullScreen
        {
            get
            {
                //return swapChain.IsFullScreen;
                return false;
            }

            set
            {
//#if !STRIDE_PLATFORM_UWP
//                if (swapChain == null)
//                    return;

//                var outputIndex = Description.PreferredFullScreenOutputIndex;

//                // no outputs connected to the current graphics adapter
//                var output = GraphicsDevice.Adapter != null && outputIndex < GraphicsDevice.Adapter.Outputs.Length ? GraphicsDevice.Adapter.Outputs[outputIndex] : null;

//                Output currentOutput = null;

//                try
//                {
//                    VkBool32 isCurrentlyFullscreen;
//                    swapChain.GetFullscreenState(out isCurrentlyFullscreen, out currentOutput);

//                    // check if the current fullscreen monitor is the same as new one
//                    if (isCurrentlyFullscreen == value && output != null && currentOutput != null && currentOutput.NativePointer == output.NativeOutput.NativePointer)
//                        return;
//                }
//                finally
//                {
//                    if (currentOutput != null)
//                        currentOutput.Dispose();
//                }

//                bool switchToFullScreen = value;
//                // If going to fullscreen mode: call 1) SwapChain.ResizeTarget 2) SwapChain.IsFullScreen
//                var description = new ModeDescription(backBuffer.ViewWidth, backBuffer.ViewHeight, Description.RefreshRate.ToSharpDX(), (SharpDX.DXGI.Format)Description.BackBufferFormat);
//                if (switchToFullScreen)
//                {
//                    // Force render target destruction
//                    // TODO: We should track all user created render targets that points to back buffer as well (or deny their creation?)
//                    backBuffer.OnDestroyed();

//                    OnDestroyed();

//                    Description.IsFullScreen = true;

//                    OnRecreated();

//                    // Recreate render target
//                    backBuffer.OnRecreate();
//                }
//                else
//                {
//                    Description.IsFullScreen = false;
//                    swapChain.IsFullScreen = false;

//                    // call 1) SwapChain.IsFullScreen 2) SwapChain.Resize
//                    Resize(backBuffer.ViewWidth, backBuffer.ViewHeight, backBuffer.ViewFormat);
//                }

//                // If going to window mode: 
//                if (!switchToFullScreen)
//                {
//                    // call 1) SwapChain.IsFullScreen 2) SwapChain.Resize
//                    description.RefreshRate = new SharpDX.DXGI.Rational(0, 0);
//                    swapChain.ResizeTarget(ref description);
//                }
//#endif
            }
        }


        public override unsafe void Present()
        {
            var swapChainCopy = swapChain;
            var currentBufferIndexCopy = currentBufferIndex;
            var presentInfo = new VkPresentInfoKHR
            {
                sType = VkStructureType.PresentInfoKHR,
                swapchainCount = 1,
                pSwapchains = &swapChainCopy,
                pImageIndices = &currentBufferIndexCopy,
            };

            // Present
            while (GraphicsDevice.NativeDeviceApi.vkQueuePresentKHR(GraphicsDevice.NativeCommandQueue, &presentInfo) == VkResult.ErrorOutOfDateKHR)
            {
                OnRecreated();
                swapChainCopy = swapChain;
                presentInfo.pSwapchains = &swapChainCopy;
            }

            // Get next image
            while (GraphicsDevice.NativeDeviceApi.vkAcquireNextImageKHR(GraphicsDevice.NativeDevice, swapChain, ulong.MaxValue, GraphicsDevice.GetNextPresentSemaphore(), VkFence.Null, out currentBufferIndex) == VkResult.ErrorOutOfDateKHR)
            {
                OnRecreated();
            }

            // Flip render targets
            backbuffer.SetNativeHandles(swapchainImages[currentBufferIndex].NativeImage, swapchainImages[currentBufferIndex].NativeColorAttachmentView);
        }

        public override void BeginDraw(CommandList commandList)
        {
            // Backbuffer needs to be cleared
            backbuffer.IsInitialized = false;
        }

        public override void EndDraw(CommandList commandList, bool present)
        {
        }

        protected override void OnNameChanged()
        {
            base.OnNameChanged();
        }

        /// <inheritdoc/>
        protected internal override unsafe void OnDestroyed()
        {
            DestroySwapchain();

            GraphicsDevice.NativeInstanceApi.vkDestroySurfaceKHR(GraphicsDevice.NativeInstance, surface, null);
            surface = VkSurfaceKHR.Null;

            base.OnDestroyed();
        }

        /// <inheritdoc/>
        public override void OnRecreated()
        {
            // Don't seem to get any crashes for calling the following, looks like standard swapchain recreation code.
            // For the time being, comment out the not implemented exception.
            // throw new NotImplementedException();

            base.OnRecreated();

            // Recreate swap chain
            CreateSwapChain();
        }

        protected override void ResizeBackBuffer(int width, int height, PixelFormat format)
        {
            CreateSwapChain();
        }

        protected override void ResizeDepthStencilBuffer(int width, int height, PixelFormat format)
        {
            var newTextureDescription = DepthStencilBuffer.Description;
            newTextureDescription.Width = width;
            newTextureDescription.Height = height;

            // Manually update the texture
            DepthStencilBuffer.OnDestroyed();

            // Put it in our back buffer texture
            DepthStencilBuffer.InitializeFrom(newTextureDescription);
        }


        private unsafe void DestroySwapchain()
        {
            if (swapChain == VkSwapchainKHR.Null)
                return;

            GraphicsDevice.NativeDeviceApi.vkDeviceWaitIdle(GraphicsDevice.NativeDevice);

            backbuffer.OnDestroyed();

            foreach (var swapchainImage in swapchainImages)
            {
                GraphicsDevice.NativeDeviceApi.vkDestroyImageView(GraphicsDevice.NativeDevice, swapchainImage.NativeColorAttachmentView, null);
            }
            swapchainImages = null;

            GraphicsDevice.NativeDeviceApi.vkDestroySwapchainKHR(GraphicsDevice.NativeDevice, swapChain, null);
            swapChain = VkSwapchainKHR.Null;
        }

        private unsafe void CreateSwapChain()
        {
            var formats = new[] { PixelFormat.B8G8R8A8_UNorm_SRgb, PixelFormat.R8G8B8A8_UNorm_SRgb, PixelFormat.B8G8R8A8_UNorm, PixelFormat.R8G8B8A8_UNorm };

            foreach (var format in formats)
            {
                var nativeFromat = VulkanConvertExtensions.ConvertPixelFormat(format);

                GraphicsDevice.NativeInstanceApi.vkGetPhysicalDeviceFormatProperties(GraphicsDevice.NativePhysicalDevice, nativeFromat, out var formatProperties);

                if ((formatProperties.optimalTilingFeatures & VkFormatFeatureFlags.ColorAttachment) != 0)
                {
                    Description.BackBufferFormat = format;
                    break;
                }
            }

            // Queue
            // TODO VULKAN: Queue family is needed when creating the Device, so here we can just do a sanity check?
            GraphicsDevice.NativeInstanceApi.vkGetPhysicalDeviceQueueFamilyProperties(GraphicsDevice.NativePhysicalDevice, out uint queueFamilyCount);
            Span<VkQueueFamilyProperties> queueFamilies = stackalloc VkQueueFamilyProperties[(int)queueFamilyCount];
            GraphicsDevice.NativeInstanceApi.vkGetPhysicalDeviceQueueFamilyProperties(GraphicsDevice.NativePhysicalDevice, queueFamilies);
            var queueNodeIndex = queueFamilies.ToArray()
                .Where((properties, index) => (properties.queueFlags & VkQueueFlags.Graphics) != 0 && GraphicsDevice.NativeInstanceApi.vkGetPhysicalDeviceSurfaceSupportKHR(GraphicsDevice.NativePhysicalDevice, (uint)index, surface, out var supported) == VkResult.Success && supported)
                .Select((properties, index) => index).First();

            // Surface format
            var backBufferFormat = VulkanConvertExtensions.ConvertPixelFormat(Description.BackBufferFormat);

            GraphicsDevice.NativeInstanceApi.vkGetPhysicalDeviceSurfaceFormatsKHR(GraphicsDevice.NativePhysicalDevice, surface, out uint surfaceFormatCount);
            Span<VkSurfaceFormatKHR> surfaceFormats = stackalloc VkSurfaceFormatKHR[(int)surfaceFormatCount];
            GraphicsDevice.NativeInstanceApi.vkGetPhysicalDeviceSurfaceFormatsKHR(GraphicsDevice.NativePhysicalDevice, surface, surfaceFormats);
            if ((surfaceFormats.Length != 1 || surfaceFormats[0].format != VkFormat.Undefined) &&
                !surfaceFormats.ToArray().Any(x => x.format == backBufferFormat))
            {
                backBufferFormat = surfaceFormats[0].format;
            }

            // Create swapchain
            GraphicsDevice.NativeInstanceApi.vkGetPhysicalDeviceSurfaceCapabilitiesKHR(GraphicsDevice.NativePhysicalDevice, surface, out var surfaceCapabilities);

            // Buffer count
            uint desiredImageCount = Math.Max(surfaceCapabilities.minImageCount, 2);
            if (surfaceCapabilities.maxImageCount > 0 && desiredImageCount > surfaceCapabilities.maxImageCount)
            {
                desiredImageCount = surfaceCapabilities.maxImageCount;
            }

            // Transform
            VkSurfaceTransformFlagsKHR preTransform;
            if ((surfaceCapabilities.supportedTransforms & VkSurfaceTransformFlagsKHR.Identity) != 0)
            {
                preTransform = VkSurfaceTransformFlagsKHR.Identity;
            }
            else
            {
                preTransform = surfaceCapabilities.currentTransform;
            }

            // Find present mode
            GraphicsDevice.NativeInstanceApi.vkGetPhysicalDeviceSurfacePresentModesKHR(GraphicsDevice.NativePhysicalDevice, surface, out uint presentModeCount);
            Span<VkPresentModeKHR> presentModes = stackalloc VkPresentModeKHR[(int)presentModeCount];
            GraphicsDevice.NativeInstanceApi.vkGetPhysicalDeviceSurfacePresentModesKHR(GraphicsDevice.NativePhysicalDevice, surface, presentModes);
            var swapChainPresentMode = VkPresentModeKHR.Fifo; // Always supported
            foreach (var presentMode in presentModes)
            {
                // TODO VULKAN: Handle PresentInterval.Two
                if (Description.PresentationInterval == PresentInterval.Immediate)
                {
                    // Prefer mailbox to immediate
                    if (presentMode == VkPresentModeKHR.Immediate)
                    {
                        swapChainPresentMode = VkPresentModeKHR.Immediate;
                    }
                    else if (presentMode == VkPresentModeKHR.Mailbox)
                    {
                        swapChainPresentMode = VkPresentModeKHR.Mailbox;
                        break;
                    }
                }
            }

            // Create swapchain
            var swapchainCreateInfo = new VkSwapchainCreateInfoKHR
            {
                sType = VkStructureType.SwapchainCreateInfoKHR,
                surface = surface,
                imageArrayLayers = 1,
                imageSharingMode = VkSharingMode.Exclusive,
                imageExtent = new VkExtent2D(Description.BackBufferWidth, Description.BackBufferHeight),
                imageFormat = backBufferFormat,
                imageColorSpace = Description.ColorSpace == ColorSpace.Gamma ? VkColorSpaceKHR.SrgbNonLinear : 0,
                imageUsage = VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.TransferDst | (surfaceCapabilities.supportedUsageFlags & VkImageUsageFlags.TransferSrc), // TODO VULKAN: Use off-screen buffer to emulate
                presentMode = swapChainPresentMode,
                compositeAlpha = VkCompositeAlphaFlagsKHR.Opaque,
                minImageCount = desiredImageCount,
                preTransform = preTransform,
                oldSwapchain = swapChain,
                clipped = true
            };
            GraphicsDevice.NativeDeviceApi.vkCreateSwapchainKHR(GraphicsDevice.NativeDevice, &swapchainCreateInfo, null, out var newSwapChain);

            DestroySwapchain();

            swapChain = newSwapChain;
            CreateBackBuffers();
        }

        private unsafe void CreateSurface()
        {
            // Check for Window Handle parameter
            if (Description.DeviceWindowHandle == null)
            {
                throw new ArgumentException("DeviceWindowHandle cannot be null");
            }
            // Create surface
#if STRIDE_UI_SDL
            var control = Description.DeviceWindowHandle.NativeWindow as SDL.Window;
            Silk.NET.Core.Native.VkNonDispatchableHandle surfaceHandle = default;
            SDL.Window.SDL.VulkanCreateSurface((Silk.NET.SDL.Window*)control.SdlHandle, new Silk.NET.Core.Native.VkHandle(GraphicsDevice.NativeInstance.Handle), ref surfaceHandle);
            surface = new VkSurfaceKHR(surfaceHandle.Handle);
#else
            if (Platform.Type == PlatformType.Windows)
            {
                var controlHandle = Description.DeviceWindowHandle.Handle;
                if (controlHandle == IntPtr.Zero)
                {
                    throw new NotSupportedException($"Form of type [{Description.DeviceWindowHandle.GetType().Name}] is not supported. Only System.Windows.Control are supported");
                }

                var surfaceCreateInfo = new VkWin32SurfaceCreateInfoKHR
                {
                    sType = VkStructureType.Win32SurfaceCreateInfoKHR,
                    instanceHandle = Process.GetCurrentProcess().Handle,
                    windowHandle = controlHandle,
                };
                vkCreateWin32SurfaceKHR(GraphicsDevice.NativeInstance, &surfaceCreateInfo, null, out surface);
            }
            else if (Platform.Type == PlatformType.Android)
            {
                throw new NotImplementedException();
            }
            else if (Platform.Type == PlatformType.Linux)
            {
                throw new NotSupportedException("Only SDL is supported for the time being on Linux");
            }
            else
            {
                throw new NotSupportedException();
            }
#endif
        }

        private unsafe void CreateBackBuffers()
        {
            // Create the texture object
            var backBufferDescription = new TextureDescription
            {
                ArraySize = 1,
                Dimension = TextureDimension.Texture2D,
                Height = Description.BackBufferHeight,
                Width = Description.BackBufferWidth,
                Depth = 1,
                Flags = TextureFlags.RenderTarget,
                Format = Description.BackBufferFormat,
                MipLevels = 1,
                MultisampleCount = MultisampleCount.None,
                Usage = GraphicsResourceUsage.Default
            };
            backbuffer.InitializeWithoutResources(backBufferDescription);

            var createInfo = new VkImageViewCreateInfo
            {
                sType = VkStructureType.ImageViewCreateInfo,
                subresourceRange = new VkImageSubresourceRange(VkImageAspectFlags.Color, 0, 1, 0, 1),
                format = backbuffer.NativeFormat,
                viewType = VkImageViewType.Image2D,
            };

            // We initialize swapchain images to PresentSource, since we swap them out while in this layout.
            backbuffer.NativeAccessMask = VkAccessFlags.MemoryRead;
            backbuffer.NativeLayout = VkImageLayout.PresentSrcKHR;

            var imageMemoryBarrier = new VkImageMemoryBarrier
            {
                sType = VkStructureType.ImageMemoryBarrier,
                subresourceRange = new VkImageSubresourceRange(VkImageAspectFlags.Color, 0, 1, 0, 1),
                oldLayout = VkImageLayout.Undefined,
                newLayout = VkImageLayout.PresentSrcKHR,
                srcAccessMask = VkAccessFlags.None,
                dstAccessMask = VkAccessFlags.MemoryRead
            };

            var commandBufferAllocationInfo = new VkCommandBufferAllocateInfo
            {
                sType = VkStructureType.CommandBufferAllocateInfo,
                level = VkCommandBufferLevel.Primary,
                commandPool = GraphicsDevice.NativeCopyCommandPools.Value,
                commandBufferCount = 1
            };
            VkCommandBuffer commandBuffer;
            GraphicsDevice.NativeDeviceApi.vkAllocateCommandBuffers(GraphicsDevice.NativeDevice, &commandBufferAllocationInfo, &commandBuffer);

            var beginInfo = new VkCommandBufferBeginInfo { sType = VkStructureType.CommandBufferBeginInfo };
            GraphicsDevice.NativeDeviceApi.vkBeginCommandBuffer(commandBuffer, &beginInfo);

            GraphicsDevice.NativeDeviceApi.vkGetSwapchainImagesKHR(GraphicsDevice.NativeDevice, swapChain, out uint swapchainImageCount);
            Span<VkImage> buffers = stackalloc VkImage[(int)swapchainImageCount];
            GraphicsDevice.NativeDeviceApi.vkGetSwapchainImagesKHR(GraphicsDevice.NativeDevice, swapChain, buffers);
            swapchainImages = new SwapChainImageInfo[buffers.Length];

            for (int index = 0; index < buffers.Length; index++)
            {
                // Create image views
                swapchainImages[index].NativeImage = createInfo.image = buffers[index];
                GraphicsDevice.NativeDeviceApi.vkCreateImageView(GraphicsDevice.NativeDevice, &createInfo, null, out swapchainImages[index].NativeColorAttachmentView);

                // Transition to default layout
                imageMemoryBarrier.image = buffers[index];
                GraphicsDevice.NativeDeviceApi.vkCmdPipelineBarrier(commandBuffer, VkPipelineStageFlags.AllCommands, VkPipelineStageFlags.AllCommands, VkDependencyFlags.None, 0, null, 0, null, 1, &imageMemoryBarrier);
            }

            // Close and submit
            GraphicsDevice.NativeDeviceApi.vkEndCommandBuffer(commandBuffer);

            var submitInfo = new VkSubmitInfo
            {
                sType = VkStructureType.SubmitInfo,
                commandBufferCount = 1,
                pCommandBuffers = &commandBuffer,
            };

            lock (GraphicsDevice.QueueLock)
            {
                GraphicsDevice.NativeDeviceApi.vkQueueSubmit(GraphicsDevice.NativeCommandQueue, 1, &submitInfo, VkFence.Null);
                GraphicsDevice.NativeDeviceApi.vkQueueWaitIdle(GraphicsDevice.NativeCommandQueue);
            }

            GraphicsDevice.NativeDeviceApi.vkFreeCommandBuffers(GraphicsDevice.NativeDevice, GraphicsDevice.NativeCopyCommandPools.Value, 1, &commandBuffer);

            // Get next image
            GraphicsDevice.NativeDeviceApi.vkAcquireNextImageKHR(GraphicsDevice.NativeDevice, swapChain, ulong.MaxValue, GraphicsDevice.GetNextPresentSemaphore(), VkFence.Null, out currentBufferIndex);
            
            // Apply the first swap chain image to the texture
            backbuffer.SetNativeHandles(swapchainImages[currentBufferIndex].NativeImage, swapchainImages[currentBufferIndex].NativeColorAttachmentView);
        }
    }
}
#endif
