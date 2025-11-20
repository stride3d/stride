// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using System.Diagnostics;
using System.Linq;
using Stride.Core;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Stride.Graphics
{
    /// <summary>
    /// Graphics presenter for SwapChain.
    /// </summary>
    public class SwapChainGraphicsPresenter : GraphicsPresenter
    {
        private VkSwapchainKHR swapChain;
        private VkSurfaceKHR surface;

        private Texture backBuffer;

        // As many as swapchain backbuffer
        private VkSemaphore[] submitSemaphores;
        private SwapChainImageInfo[] swapchainImages;
        private uint currentBufferIndex;

        private const int kNumberOfFramesInFlight = 2;
        private int currentFrameIndex = 0;
        private VkSemaphore[] acquireSemaphores;
        private VkFence[] frameFences;

        private struct SwapChainImageInfo
        {
            public VkImage NativeImage;
            public VkImageView NativeColorAttachmentView;
        }

        public unsafe SwapChainGraphicsPresenter(GraphicsDevice device, PresentationParameters presentationParameters)
            : base(device, presentationParameters)
        {
            PresentInterval = presentationParameters.PresentationInterval;

            backBuffer = new Texture(device);

            CreateSurface();

            // Initialize the swap chain
            CreateSwapChain(Description.BackBufferWidth, Description.BackBufferHeight, Description.BackBufferFormat);
        }

        public override Texture BackBuffer
        {
            get
            {
                return backBuffer;
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
//                    backBuffer.OnDestroyed(true);

//                    OnDestroyed(true);

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
            // Code is inspired from https://docs.vulkan.org/guide/latest/swapchain_semaphore_reuse.html (good code example)
            // except we start the loop from vkQueueSubmit+vkQueuePresent and then proceed with preparing next frame resources

            lock (GraphicsDevice.QueueLock)
            {
                // Signal semaphore (that we will wait on during present for GPU=>GPU sync, to make sure all previous command buffers have been executed)
                var submitSemaphore = submitSemaphores[currentBufferIndex];
                var frameFence = GraphicsDevice.FrameFence.Semaphore;
                var frameFenceValue = GraphicsDevice.FrameFence.NextFenceValue - 1;
                var pipelineStageFlags = VkPipelineStageFlags.BottomOfPipe;
                var timelineInfo = new VkTimelineSemaphoreSubmitInfo
                {
                    sType = VkStructureType.TimelineSemaphoreSubmitInfo,
                    waitSemaphoreValueCount = 1,
                    pWaitSemaphoreValues = &frameFenceValue,
                };
                var submitInfo = new VkSubmitInfo
                {
                    sType = VkStructureType.SubmitInfo,
                    pNext = &timelineInfo,
                    waitSemaphoreCount = 1,
                    pWaitSemaphores = &frameFence,
                    pWaitDstStageMask = &pipelineStageFlags,
                    signalSemaphoreCount = 1,
                    pSignalSemaphores = &submitSemaphore,
                };
                
                GraphicsDevice.CheckResult(vkQueueSubmit(GraphicsDevice.NativeCommandQueue, 1, &submitInfo, frameFences[currentFrameIndex]));

                var currentBufferIndexCopy = currentBufferIndex;
                var swapChainCopy = swapChain;
                var presentInfo = new VkPresentInfoKHR
                {
                    sType = VkStructureType.PresentInfoKHR,
                    swapchainCount = 1,
                    pSwapchains = &swapChainCopy,
                    pImageIndices = &currentBufferIndexCopy,
                    waitSemaphoreCount = 1,
                    pWaitSemaphores = &submitSemaphore,
                };

                // Present
                Debug.WriteLine($"Present image {currentBufferIndex}: {swapchainImages[currentBufferIndex].NativeImage.Handle.ToString("X")}");
                var presentResult = vkQueuePresentKHR(GraphicsDevice.NativeCommandQueue, &presentInfo);
                if (presentResult == VkResult.ErrorOutOfDateKHR)
                {
                    OnRecreated();
                    return;
                }

                GraphicsDevice.CheckResult(presentResult);
            }

            currentFrameIndex = (currentFrameIndex + 1) % kNumberOfFramesInFlight;

            // Wait for frame fence to be available
            GraphicsDevice.CheckResult(vkWaitForFences(GraphicsDevice.NativeDevice, frameFences[currentFrameIndex], VkBool32.True, ulong.MaxValue));
            vkResetFences(GraphicsDevice.NativeDevice, frameFences[currentFrameIndex]);

            // Get next image
            if (vkAcquireNextImageKHR(GraphicsDevice.NativeDevice, swapChain, ulong.MaxValue, acquireSemaphores[currentFrameIndex], VkFence.Null, out currentBufferIndex) == VkResult.ErrorOutOfDateKHR)
            {
                OnRecreated();
            }

            // Flip render targets
            backBuffer.SetNativeHandles(swapchainImages[currentBufferIndex].NativeImage, swapchainImages[currentBufferIndex].NativeColorAttachmentView);
            Debug.WriteLine($"Next swapchain image {currentBufferIndex}: {swapchainImages[currentBufferIndex].NativeImage.Handle.ToString("X")}");

            lock (GraphicsDevice.QueueLock)
            {
                // Signal vkAcquireNextImageKHR Fence => GraphicsDevice.CommandList (so that next command list will wait for this to complete)
                var acquireSemaphore = acquireSemaphores[currentFrameIndex];
                var commandListFence = GraphicsDevice.CommandListFence.Semaphore;
                var nextCommandListFenceValue = ++GraphicsDevice.CommandListFence.NextFenceValue;
                var pipelineStageFlags = VkPipelineStageFlags.BottomOfPipe;
                var timelineInfo = new VkTimelineSemaphoreSubmitInfo
                {
                    sType = VkStructureType.TimelineSemaphoreSubmitInfo,
                    signalSemaphoreValueCount = 1,
                    pSignalSemaphoreValues = &nextCommandListFenceValue,
                };
                var submitInfo = new VkSubmitInfo
                {
                    sType = VkStructureType.SubmitInfo,
                    pNext = &timelineInfo,
                    waitSemaphoreCount = 1,
                    pWaitSemaphores = &acquireSemaphore,
                    pWaitDstStageMask = &pipelineStageFlags,
                    signalSemaphoreCount = 1,
                    pSignalSemaphores = &commandListFence,
                };

                GraphicsDevice.CheckResult(vkQueueSubmit(GraphicsDevice.NativeCommandQueue, 1, &submitInfo, VkFence.Null));
            }
        }

        public override void BeginDraw(CommandList commandList)
        {
            // Backbuffer needs to be cleared
            backBuffer.IsInitialized = false;
        }

        public override void EndDraw(CommandList commandList, bool present)
        {
        }

        protected override void OnNameChanged()
        {
            base.OnNameChanged();
        }

        /// <inheritdoc/>
        protected internal override unsafe void OnDestroyed(bool immediate = false)
        {
            DestroySwapchain();

            vkDestroySurfaceKHR(GraphicsDevice.NativeInstance, surface, null);
            surface = VkSurfaceKHR.Null;

            base.OnDestroyed(immediate);
        }

        /// <inheritdoc/>
        public override void OnRecreated()
        {
            // Don't seem to get any crashes for calling the following, looks like standard swapchain recreation code.
            // For the time being, comment out the not implemented exception.
            // throw new NotImplementedException();

            base.OnRecreated();

            // Manually update all children textures
            var fastList = DestroyChildrenTextures(backBuffer);

            // Recreate swap chain
            CreateSwapChain(backBuffer.Width, backBuffer.Height, backBuffer.Format);

            foreach (var texture in fastList)
            {
                texture.InitializeFrom(backBuffer, texture.ViewDescription);
            }
        }

        protected override void ResizeBackBuffer(int width, int height, PixelFormat format)
        {
            // Manually update all children textures
            var fastList = DestroyChildrenTextures(backBuffer);

            CreateSwapChain(width, height, format);

            foreach (var texture in fastList)
            {
                texture.InitializeFrom(backBuffer, texture.ViewDescription);
            }
        }

        protected override void ResizeDepthStencilBuffer(int width, int height, PixelFormat format)
        {
            var newTextureDescription = DepthStencilBuffer.Description;
            newTextureDescription.Width = width;
            newTextureDescription.Height = height;

            // Manually update all children textures
            var fastList = DestroyChildrenTextures(DepthStencilBuffer);

            // Manually update the texture
            DepthStencilBuffer.OnDestroyed(true);

            // Put it in our back buffer texture
            DepthStencilBuffer.InitializeFrom(newTextureDescription);

            foreach (var texture in fastList)
            {
                texture.InitializeFrom(DepthStencilBuffer, texture.ViewDescription);
            }
        }


        private unsafe void DestroySwapchain()
        {
            if (swapChain == VkSwapchainKHR.Null)
                return;

            GraphicsDevice.CheckResult(vkDeviceWaitIdle(GraphicsDevice.NativeDevice));

            backBuffer.OnDestroyed(true);

            foreach (var semaphore in submitSemaphores)
            {
                vkDestroySemaphore(GraphicsDevice.NativeDevice, semaphore);
            }
            submitSemaphores = null;

            foreach (var swapchainImage in swapchainImages)
            {
                vkDestroyImageView(GraphicsDevice.NativeDevice, swapchainImage.NativeColorAttachmentView, null);
            }
            swapchainImages = null;

            for (int i = 0; i < kNumberOfFramesInFlight; i++)
            {
                vkDestroySemaphore(GraphicsDevice.NativeDevice, acquireSemaphores[i]);
                vkDestroyFence(GraphicsDevice.NativeDevice, frameFences[i]);
            }
            acquireSemaphores = null;
            frameFences = null;

            vkDestroySwapchainKHR(GraphicsDevice.NativeDevice, swapChain, null);
            swapChain = VkSwapchainKHR.Null;
        }

        private unsafe void CreateSwapChain(int width, int height, PixelFormat desiredFormat)
        {
            var formats = new[] { desiredFormat, PixelFormat.B8G8R8A8_UNorm_SRgb, PixelFormat.R8G8B8A8_UNorm_SRgb, PixelFormat.B8G8R8A8_UNorm, PixelFormat.R8G8B8A8_UNorm };

            foreach (var format in formats)
            {
                var nativeFromat = VulkanConvertExtensions.ConvertPixelFormat(format);

                vkGetPhysicalDeviceFormatProperties(GraphicsDevice.NativePhysicalDevice, nativeFromat, out var formatProperties);

                if ((formatProperties.optimalTilingFeatures & VkFormatFeatureFlags.ColorAttachment) != 0)
                {
                    Description.BackBufferFormat = format;
                    if (format != desiredFormat)
                    {
                        // Investigate what formats we want to allow if desired format can't be created
                        if (Debugger.IsAttached) Debugger.Break();
                    }
                    break;
                }
            }

            // Queue
            // TODO VULKAN: Queue family is needed when creating the Device, so here we can just do a sanity check?
            var queueNodeIndex = vkGetPhysicalDeviceQueueFamilyProperties(GraphicsDevice.NativePhysicalDevice).ToArray().
                Where((properties, index) => (properties.queueFlags & VkQueueFlags.Graphics) != 0 && vkGetPhysicalDeviceSurfaceSupportKHR(GraphicsDevice.NativePhysicalDevice, (uint)index, surface, out var supported) == VkResult.Success && supported).
                Select((properties, index) => index).First();

            // Surface format
            var backBufferFormat = VulkanConvertExtensions.ConvertPixelFormat(Description.BackBufferFormat);

            var surfaceFormats = vkGetPhysicalDeviceSurfaceFormatsKHR(GraphicsDevice.NativePhysicalDevice, surface).ToArray();
            if ((surfaceFormats.Length != 1 || surfaceFormats[0].format != VkFormat.Undefined) &&
                !surfaceFormats.Any(x => x.format == backBufferFormat))
            {
                backBufferFormat = surfaceFormats[0].format;
            }

            // Create swapchain
            vkGetPhysicalDeviceSurfaceCapabilitiesKHR(GraphicsDevice.NativePhysicalDevice, surface, out var surfaceCapabilities);

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
            var presentModes = vkGetPhysicalDeviceSurfacePresentModesKHR(GraphicsDevice.NativePhysicalDevice, surface);
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
            GraphicsDevice.CheckResult(vkCreateSwapchainKHR(GraphicsDevice.NativeDevice, &swapchainCreateInfo, null, out var newSwapChain));

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
            if (Description.DeviceWindowHandle.Context == Games.AppContextType.DesktopSDL)
            {
                var control = Description.DeviceWindowHandle.NativeWindow as SDL.Window;
                Silk.NET.Core.Native.VkNonDispatchableHandle surfaceHandle = default;
                SDL.Window.SDL.VulkanCreateSurface((Silk.NET.SDL.Window*)control.SdlHandle, new Silk.NET.Core.Native.VkHandle(GraphicsDevice.NativeInstance.Handle), ref surfaceHandle);
                surface = new VkSurfaceKHR(surfaceHandle.Handle);
            }
            else
#endif
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
                    hinstance = Process.GetCurrentProcess().Handle,
                    hwnd = controlHandle,
                };
                GraphicsDevice.CheckResult(vkCreateWin32SurfaceKHR(GraphicsDevice.NativeInstance, &surfaceCreateInfo, null, out surface));
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
                MipLevelCount = 1,
                MultisampleCount = MultisampleCount.None,
                Usage = GraphicsResourceUsage.Default
            };
            backBuffer.InitializeWithoutResources(backBufferDescription);

            var createInfo = new VkImageViewCreateInfo
            {
                sType = VkStructureType.ImageViewCreateInfo,
                subresourceRange = new VkImageSubresourceRange(VkImageAspectFlags.Color, 0, 1, 0, 1),
                format = backBuffer.NativeFormat,
                viewType = VkImageViewType.Image2D,
            };

            // We initialize swapchain images to PresentSource, since we swap them out while in this layout.
            backBuffer.NativeAccessMask = VkAccessFlags.MemoryRead;
            backBuffer.NativeLayout = VkImageLayout.PresentSrcKHR;

            var imageMemoryBarrier = new VkImageMemoryBarrier
            {
                sType = VkStructureType.ImageMemoryBarrier,
                subresourceRange = new VkImageSubresourceRange(VkImageAspectFlags.Color, 0, 1, 0, 1),
                oldLayout = VkImageLayout.Undefined,
                newLayout = VkImageLayout.PresentSrcKHR,
                srcAccessMask = VkAccessFlags.None,
                dstAccessMask = VkAccessFlags.MemoryRead
            };

            var commandBuffer = GraphicsDevice.NativeCopyCommandPools.Value.GetObject(0);

            var beginInfo = new VkCommandBufferBeginInfo { sType = VkStructureType.CommandBufferBeginInfo };
            vkBeginCommandBuffer(commandBuffer, &beginInfo);

            var buffers = vkGetSwapchainImagesKHR(GraphicsDevice.NativeDevice, swapChain);
            swapchainImages = new SwapChainImageInfo[buffers.Length];

            for (int i = 0; i < buffers.Length; i++)
            {
                // Create image views
                swapchainImages[i].NativeImage = createInfo.image = buffers[i];
                GraphicsDevice.CheckResult(vkCreateImageView(GraphicsDevice.NativeDevice, &createInfo, null, out swapchainImages[i].NativeColorAttachmentView));

                // Transition to default layout
                imageMemoryBarrier.image = buffers[i];
                vkCmdPipelineBarrier(commandBuffer, VkPipelineStageFlags.AllCommands, VkPipelineStageFlags.AllCommands, VkDependencyFlags.None, 0, null, 0, null, 1, &imageMemoryBarrier);
            }

            // Close and submit
            GraphicsDevice.CheckResult(vkEndCommandBuffer(commandBuffer));

            lock (GraphicsDevice.QueueLock)
            {
                var submitInfo = new VkSubmitInfo
                {
                    sType = VkStructureType.SubmitInfo,
                    commandBufferCount = 1,
                    pCommandBuffers = &commandBuffer,
                };
                GraphicsDevice.CheckResult(vkQueueSubmit(GraphicsDevice.NativeCommandQueue, 1, &submitInfo, VkFence.Null));
                GraphicsDevice.CheckResult(vkQueueWaitIdle(GraphicsDevice.NativeCommandQueue));
            }

            GraphicsDevice.NativeCopyCommandPools.Value.RecycleObject(0, commandBuffer);

            // Create submit semaphores
            submitSemaphores = new VkSemaphore[buffers.Length];
            var semaphoreCreateInfo = new VkSemaphoreCreateInfo { sType = VkStructureType.SemaphoreCreateInfo };
            for (int i = 0; i < submitSemaphores.Length; ++i)
                GraphicsDevice.CheckResult(vkCreateSemaphore(GraphicsDevice.NativeDevice, &semaphoreCreateInfo, null, out submitSemaphores[i]));

            frameFences = new VkFence[kNumberOfFramesInFlight];
            acquireSemaphores = new VkSemaphore[kNumberOfFramesInFlight];
            var fenceCreateInfo = new VkFenceCreateInfo { sType = VkStructureType.FenceCreateInfo };
            for (int i = 0; i < kNumberOfFramesInFlight; i++)
            {
                GraphicsDevice.CheckResult(vkCreateSemaphore(GraphicsDevice.NativeDevice, &semaphoreCreateInfo, null, out acquireSemaphores[i]));
                // Make all fence except 0 as signaled (so that next Present()=>vkWaitForFences is not blocked when fetching secondary buffers for first time)
                fenceCreateInfo.flags = i == 0 ? VkFenceCreateFlags.None : VkFenceCreateFlags.Signaled;
                GraphicsDevice.CheckResult(vkCreateFence(GraphicsDevice.NativeDevice, &fenceCreateInfo, null, out frameFences[i]));
            }

            // Get next image
            currentFrameIndex = 0;
            vkAcquireNextImageKHR(GraphicsDevice.NativeDevice, swapChain, ulong.MaxValue, acquireSemaphores[currentFrameIndex], VkFence.Null, out currentBufferIndex);
            
            // Apply the first swap chain image to the texture
            backBuffer.SetNativeHandles(swapchainImages[currentBufferIndex].NativeImage, swapchainImages[currentBufferIndex].NativeColorAttachmentView);

            lock (GraphicsDevice.QueueLock)
            {
                var acquireSemaphore = acquireSemaphores[currentFrameIndex];
                var pipelineStageFlags = VkPipelineStageFlags.BottomOfPipe;
                var submitInfo = new VkSubmitInfo
                {
                    sType = VkStructureType.SubmitInfo,
                    waitSemaphoreCount = 1,
                    pWaitSemaphores = &acquireSemaphore,
                    pWaitDstStageMask = &pipelineStageFlags,
                };

                GraphicsDevice.CheckResult(vkQueueSubmit(GraphicsDevice.NativeCommandQueue, 1, &submitInfo, VkFence.Null));
            }
        }
    }
}
#endif
