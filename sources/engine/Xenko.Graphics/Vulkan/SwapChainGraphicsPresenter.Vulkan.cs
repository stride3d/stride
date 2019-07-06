// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_GRAPHICS_API_VULKAN
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using SharpVulkan;
using ImageLayout = SharpVulkan.ImageLayout;

namespace Xenko.Graphics
{
    /// <summary>
    /// Graphics presenter for SwapChain.
    /// </summary>
    public class SwapChainGraphicsPresenter : GraphicsPresenter
    {
        private Swapchain swapChain;
        private Surface surface;

        private Texture backbuffer;
        private SwapChainImageInfo[] swapchainImages;
        private uint currentBufferIndex;

        private struct SwapChainImageInfo
        {
            public SharpVulkan.Image NativeImage;
            public ImageView NativeColorAttachmentView;
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
//#if !XENKO_PLATFORM_UWP
//                if (swapChain == null)
//                    return;

//                var outputIndex = Description.PreferredFullScreenOutputIndex;

//                // no outputs connected to the current graphics adapter
//                var output = GraphicsDevice.Adapter != null && outputIndex < GraphicsDevice.Adapter.Outputs.Length ? GraphicsDevice.Adapter.Outputs[outputIndex] : null;

//                Output currentOutput = null;

//                try
//                {
//                    RawBool isCurrentlyFullscreen;
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
            try
            {
                var swapChainCopy = swapChain;
                var currentBufferIndexCopy = currentBufferIndex;
                var presentInfo = new PresentInfo
                {
                    StructureType = StructureType.PresentInfo,
                    SwapchainCount = 1,
                    Swapchains = new IntPtr(&swapChainCopy),
                    ImageIndices = new IntPtr(&currentBufferIndexCopy),
                };

                // Present
                GraphicsDevice.NativeCommandQueue.Present(ref presentInfo);

                // Get next image
                currentBufferIndex = GraphicsDevice.NativeDevice.AcquireNextImage(swapChain, ulong.MaxValue, GraphicsDevice.GetNextPresentSemaphore(), Fence.Null);

                // Flip render targets
                backbuffer.SetNativeHandles(swapchainImages[currentBufferIndex].NativeImage, swapchainImages[currentBufferIndex].NativeColorAttachmentView);
            }
            catch (SharpVulkanException e) when (e.Result == Result.ErrorOutOfDate)
            {
                // TODO VULKAN 
            }
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

            GraphicsDevice.NativeInstance.DestroySurface(surface);
            surface = Surface.Null;

            base.OnDestroyed();
        }

        /// <inheritdoc/>
        public override void OnRecreated()
        {
            // TODO VULKAN: Violent driver crashes when recreating device and swapchain
            throw new NotImplementedException();

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
            if (swapChain == Swapchain.Null)
                return;

            GraphicsDevice.NativeDevice.WaitIdle();

            backbuffer.OnDestroyed();

            foreach (var swapchainImage in swapchainImages)
            {
                GraphicsDevice.NativeDevice.DestroyImageView(swapchainImage.NativeColorAttachmentView);
            }
            swapchainImages = null;

            GraphicsDevice.NativeDevice.DestroySwapchain(swapChain);
            swapChain = Swapchain.Null;
        }

        private unsafe void CreateSwapChain()
        {
            var formats = new[] { PixelFormat.B8G8R8A8_UNorm_SRgb, PixelFormat.R8G8B8A8_UNorm_SRgb, PixelFormat.B8G8R8A8_UNorm, PixelFormat.R8G8B8A8_UNorm };

            foreach (var format in formats)
            {
                var nativeFromat = VulkanConvertExtensions.ConvertPixelFormat(format);

                FormatProperties formatProperties;
                GraphicsDevice.NativePhysicalDevice.GetFormatProperties(nativeFromat, out formatProperties);

                if ((formatProperties.OptimalTilingFeatures & FormatFeatureFlags.ColorAttachment) != 0)
                {
                    Description.BackBufferFormat = format;
                    break;
                }
            }

            // Queue
            // TODO VULKAN: Queue family is needed when creating the Device, so here we can just do a sanity check?
            var queueNodeIndex = GraphicsDevice.NativePhysicalDevice.QueueFamilyProperties.
                Where((properties, index) => (properties.QueueFlags & QueueFlags.Graphics) != 0 && GraphicsDevice.NativePhysicalDevice.GetSurfaceSupport((uint)index, surface)).
                Select((properties, index) => index).First();

            // Surface format
            var backBufferFormat = VulkanConvertExtensions.ConvertPixelFormat(Description.BackBufferFormat);

            var surfaceFormats = GraphicsDevice.NativePhysicalDevice.GetSurfaceFormats(surface);
            if ((surfaceFormats.Length != 1 || surfaceFormats[0].Format != Format.Undefined) &&
                !surfaceFormats.Any(x => x.Format == backBufferFormat))
            {
                backBufferFormat = surfaceFormats[0].Format;
            }

            // Create swapchain
            SurfaceCapabilities surfaceCapabilities;
            GraphicsDevice.NativePhysicalDevice.GetSurfaceCapabilities(surface, out surfaceCapabilities);

            // Buffer count
            uint desiredImageCount = Math.Max(surfaceCapabilities.MinImageCount, 2);
            if (surfaceCapabilities.MaxImageCount > 0 && desiredImageCount > surfaceCapabilities.MaxImageCount)
            {
                desiredImageCount = surfaceCapabilities.MaxImageCount;
            }

            // Transform
            SurfaceTransformFlags preTransform;
            if ((surfaceCapabilities.SupportedTransforms & SurfaceTransformFlags.Identity) != 0)
            {
                preTransform = SurfaceTransformFlags.Identity;
            }
            else
            {
                preTransform = surfaceCapabilities.CurrentTransform;
            }

            // Find present mode
            var presentModes = GraphicsDevice.NativePhysicalDevice.GetSurfacePresentModes(surface);
            var swapChainPresentMode = PresentMode.Fifo; // Always supported
            foreach (var presentMode in presentModes)
            {
                // TODO VULKAN: Handle PresentInterval.Two
                if (Description.PresentationInterval == PresentInterval.Immediate)
                {
                    // Prefer mailbox to immediate
                    if (presentMode == PresentMode.Immediate)
                    {
                        swapChainPresentMode = PresentMode.Immediate;
                    }
                    else if (presentMode == PresentMode.Mailbox)
                    {
                        swapChainPresentMode = PresentMode.Mailbox;
                        break;
                    }
                }
            }

            // Create swapchain
            var swapchainCreateInfo = new SwapchainCreateInfo
            {
                StructureType = StructureType.SwapchainCreateInfo,
                Surface = surface,
                ImageArrayLayers = 1,
                ImageSharingMode = SharingMode.Exclusive,
                ImageExtent = new Extent2D((uint)Description.BackBufferWidth, (uint)Description.BackBufferHeight),
                ImageFormat = backBufferFormat,
                ImageColorSpace = Description.ColorSpace == ColorSpace.Gamma ? SharpVulkan.ColorSpace.SRgbNonlinear : 0,
                ImageUsage = ImageUsageFlags.ColorAttachment | ImageUsageFlags.TransferDestination | (surfaceCapabilities.SupportedUsageFlags & ImageUsageFlags.TransferSource), // TODO VULKAN: Use off-screen buffer to emulate
                PresentMode = swapChainPresentMode,
                CompositeAlpha = CompositeAlphaFlags.Opaque,
                MinImageCount = desiredImageCount,
                PreTransform = preTransform,
                OldSwapchain = swapChain,
                Clipped = true
            };
            var newSwapChain = GraphicsDevice.NativeDevice.CreateSwapchain(ref swapchainCreateInfo);

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
#if XENKO_UI_SDL
            var control = Description.DeviceWindowHandle.NativeWindow as SDL.Window;
            SDL2.SDL.SDL_Vulkan_CreateSurface(control.SdlHandle, GraphicsDevice.NativeInstance.NativeHandle, out IntPtr surfacePtr);
            surface = new Surface(surfacePtr);
#elif XENKO_PLATFORM_WINDOWS
            var controlHandle = Description.DeviceWindowHandle.Handle;
            if (controlHandle == IntPtr.Zero)
            {
                throw new NotSupportedException($"Form of type [{Description.DeviceWindowHandle.GetType().Name}] is not supported. Only System.Windows.Control are supported");
            }

            var surfaceCreateInfo = new Win32SurfaceCreateInfo
            {
                StructureType = StructureType.Win32SurfaceCreateInfo,
                InstanceHandle = Process.GetCurrentProcess().Handle,
                WindowHandle = controlHandle,
            };
            surface = GraphicsDevice.NativeInstance.CreateWin32Surface(surfaceCreateInfo);
#elif XENKO_PLATFORM_ANDROID
            throw new NotImplementedException();
#elif XENKO_PLATFORM_LINUX
            throw new NotSupportedException("Only SDL is supported for the time being on Linux");
#else
            throw new NotSupportedException();
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

            var createInfo = new ImageViewCreateInfo
            {
                StructureType = StructureType.ImageViewCreateInfo,
                SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.Color, 0, 1, 0, 1),
                Format = backbuffer.NativeFormat,
                ViewType = ImageViewType.Image2D,
            };

            // We initialize swapchain images to PresentSource, since we swap them out while in this layout.
            backbuffer.NativeAccessMask = AccessFlags.MemoryRead;
            backbuffer.NativeLayout = ImageLayout.PresentSource;

            var imageMemoryBarrier = new ImageMemoryBarrier
            {
                StructureType = StructureType.ImageMemoryBarrier,
                SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.Color, 0, 1, 0, 1),
                OldLayout = ImageLayout.Undefined,
                NewLayout = ImageLayout.PresentSource,
                SourceAccessMask = AccessFlags.None,
                DestinationAccessMask = AccessFlags.MemoryRead
            };

            var commandBuffer = GraphicsDevice.NativeCopyCommandBuffer;
            var beginInfo = new CommandBufferBeginInfo { StructureType = StructureType.CommandBufferBeginInfo };
            commandBuffer.Begin(ref beginInfo);

            var buffers = GraphicsDevice.NativeDevice.GetSwapchainImages(swapChain);
            swapchainImages = new SwapChainImageInfo[buffers.Length];

            for (int i = 0; i < buffers.Length; i++)
            {
                // Create image views
                swapchainImages[i].NativeImage = createInfo.Image = buffers[i];
                swapchainImages[i].NativeColorAttachmentView = GraphicsDevice.NativeDevice.CreateImageView(ref createInfo);

                // Transition to default layout
                imageMemoryBarrier.Image = buffers[i];
                commandBuffer.PipelineBarrier(PipelineStageFlags.AllCommands, PipelineStageFlags.AllCommands, DependencyFlags.None, 0, null, 0, null, 1, &imageMemoryBarrier);
            }

            // Close and submit
            commandBuffer.End();

            var submitInfo = new SubmitInfo
            {
                StructureType = StructureType.SubmitInfo,
                CommandBufferCount = 1,
                CommandBuffers = new IntPtr(&commandBuffer),
            };
            GraphicsDevice.NativeCommandQueue.Submit(1, &submitInfo, Fence.Null);
            GraphicsDevice.NativeCommandQueue.WaitIdle();
            commandBuffer.Reset(CommandBufferResetFlags.None);

            // Get next image
            currentBufferIndex = GraphicsDevice.NativeDevice.AcquireNextImage(swapChain, ulong.MaxValue, GraphicsDevice.GetNextPresentSemaphore(), Fence.Null);
            
            // Apply the first swap chain image to the texture
            backbuffer.SetNativeHandles(swapchainImages[currentBufferIndex].NativeImage, swapchainImages[currentBufferIndex].NativeColorAttachmentView);
        }
    }
}
#endif
