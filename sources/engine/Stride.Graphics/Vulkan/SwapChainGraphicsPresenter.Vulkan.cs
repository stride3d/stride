// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using System.Linq;
using Silk.NET.Vulkan;
using static Silk.NET.Vulkan.Vk;
using VK = Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Stride.Graphics
{
    /// <summary>
    /// Graphics presenter for SwapChain.
    /// </summary>
    public class SwapChainGraphicsPresenter : GraphicsPresenter
    {
        private SwapchainKHR swapChain;
        private KhrSwapchain _swapChain;

        private SurfaceKHR surface;

        private Texture backbuffer;
        private SwapChainImageInfo[] swapchainImages;
        private uint currentBufferIndex;
        private readonly Vk vk;

        private struct SwapChainImageInfo
        {
            public VK.Image NativeImage;
            public ImageView NativeColorAttachmentView;
        }

        public SwapChainGraphicsPresenter(GraphicsDevice device, PresentationParameters presentationParameters)
            : base(device, presentationParameters)
        {
            vk = GetApi();
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
            var presentInfo = new PresentInfoKHR
            {
                SType = StructureType.PresentInfoKhr,
                SwapchainCount = 1,
                PSwapchains = &swapChainCopy,
                PImageIndices = &currentBufferIndexCopy,
            };
            // Present
            vk.TryGetDeviceExtension(GraphicsDevice.NativeInstance, GraphicsDevice.NativeDevice, out _swapChain);

            unsafe
            {

                if (_swapChain.QueuePresent(GraphicsDevice.NativeCommandQueue, &presentInfo) == VK.Result.ErrorOutOfDateKhr)
                {
                    OnRecreated();
                    swapChainCopy = swapChain;
                    presentInfo.PSwapchains = &swapChainCopy;
                }
            }
            // Get next image
            if (_swapChain.AcquireNextImage(GraphicsDevice.NativeDevice, swapChain, ulong.MaxValue, GraphicsDevice.GetNextPresentSemaphore(), new Fence(0), ref currentBufferIndex) == Result.ErrorOutOfDateKhr)
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

            vk.TryGetDeviceExtension(GraphicsDevice.NativeInstance, GraphicsDevice.NativeDevice, out KhrSurface surf);
            surf?.DestroySurface(GraphicsDevice.NativeInstance, surface, null);
            surface = new SurfaceKHR();

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
            if (swapChain.Handle == 0)
                return;

            vk.DeviceWaitIdle(GraphicsDevice.NativeDevice);

            backbuffer.OnDestroyed();

            foreach (var swapchainImage in swapchainImages)
            {
                vk.DestroyImageView(GraphicsDevice.NativeDevice, swapchainImage.NativeColorAttachmentView, null);
            }
            swapchainImages = null;

            _swapChain.DestroySwapchain(GraphicsDevice.NativeDevice, swapChain, null);
            swapChain = new SwapchainKHR();
        }

        private unsafe void CreateSwapChain()
        {
            Span<PixelFormat> formats = [PixelFormat.B8G8R8A8_UNorm_SRgb, PixelFormat.R8G8B8A8_UNorm_SRgb, PixelFormat.B8G8R8A8_UNorm, PixelFormat.R8G8B8A8_UNorm];

            foreach (var format in formats)
            {
                var nativeFromat = VulkanConvertExtensions.ConvertPixelFormat(format);

                vk.GetPhysicalDeviceFormatProperties(GraphicsDevice.NativePhysicalDevice, nativeFromat, out var formatProperties);

                if ((formatProperties.OptimalTilingFeatures & FormatFeatureFlags.ColorAttachmentBit) != 0)
                {
                    Description.BackBufferFormat = format;
                    break;
                }
            }

            // Queue
            // TODO VULKAN: Queue family is needed when creating the Device, so here we can just do a sanity check?

            
            // uint qCount = 0;
            // vk.GetPhysicalDeviceQueueFamilyProperties(GraphicsDevice.NativePhysicalDevice,ref qCount, null);
            // var queuefp = new QueueFamilyProperties[(int)qCount];

            // vk.GetPhysicalDeviceQueueFamilyProperties(GraphicsDevice.NativePhysicalDevice,&qCount, queuefp);

            // vk.TryGetDeviceExtension(GraphicsDevice.NativeInstance, GraphicsDevice.NativeDevice, out KhrSurface surfaceKhr);

            // NOT USED ANYWHERE
            // var queueNodeIndex =
            //     queuefp
            //     .Where((properties, index) => (properties.QueueFlags & QueueFlags.GraphicsBit) != 0 
            //         && surfaceKhr.GetPhysicalDeviceSurfaceSupport(GraphicsDevice.NativePhysicalDevice, (uint)index, surface, out var supported) == Result.Success 
            //         && supported)
            //     .Select((properties, index) => index)
            //     .FirstOrDefault();

            // Surface format
            var backBufferFormat = VulkanConvertExtensions.ConvertPixelFormat(Description.BackBufferFormat);

            if (!vk.TryGetInstanceExtension(GraphicsDevice.NativeInstance, out KhrSurface khrSurface))
            {
                throw new NotSupportedException("KHR_surface extension not found.");
            }

            uint surCount = 0;
            khrSurface.GetPhysicalDeviceSurfaceFormats(GraphicsDevice.NativePhysicalDevice, surface, &surCount, null);

            var surfaceFormats = new SurfaceFormatKHR[surCount];

            khrSurface.GetPhysicalDeviceSurfaceFormats(GraphicsDevice.NativePhysicalDevice, surface, &surCount, surfaceFormats);
            
            if ((surfaceFormats.Length != 1 || surfaceFormats[0].Format != Format.Undefined) &&
                !surfaceFormats.Any(x => x.Format == backBufferFormat))
            {
                backBufferFormat = surfaceFormats[0].Format;
            }
            
            // Create swapchain
            var info = new PhysicalDeviceSurfaceInfo2KHR
            {
                Surface = surface,
                SType = StructureType.PhysicalDeviceSurfaceInfo2Khr,
                PNext = null
            };

            vk.TryGetInstanceExtension(GraphicsDevice.NativeInstance, out KhrGetSurfaceCapabilities2 sc);
            SurfaceCapabilities2KHR surf = new(){ SType = StructureType.SurfaceCapabilities2Khr };

            sc.GetPhysicalDeviceSurfaceCapabilities2(GraphicsDevice.NativePhysicalDevice, &info, &surf);
            var surfaceCapabilities = surf.SurfaceCapabilities;

            // Buffer count
            uint desiredImageCount = Math.Max(surfaceCapabilities.MinImageCount, 2);
            if (surfaceCapabilities.MaxImageCount > 0 && desiredImageCount > surfaceCapabilities.MaxImageCount)
            {
                desiredImageCount = surfaceCapabilities.MaxImageCount;
            }

            // Transform
            SurfaceTransformFlagsKHR preTransform;
            if ((surfaceCapabilities.SupportedTransforms & SurfaceTransformFlagsKHR.IdentityBitKhr) != 0)
            {
                preTransform = SurfaceTransformFlagsKHR.IdentityBitKhr;
            }
            else
            {
                preTransform = surfaceCapabilities.CurrentTransform;
            }

            // Find present mode

            uint pModesCount = 0;
            khrSurface.GetPhysicalDeviceSurfacePresentModes(GraphicsDevice.NativePhysicalDevice, surface, &pModesCount, null);

            Span<PresentModeKHR> presentModes = stackalloc PresentModeKHR[(int)pModesCount];
            khrSurface.GetPhysicalDeviceSurfacePresentModes(GraphicsDevice.NativePhysicalDevice, surface, &pModesCount, presentModes);
            var swapChainPresentMode = PresentModeKHR.FifoKhr; // Always supported
            foreach (var presentMode in presentModes)
            {
                // TODO VULKAN: Handle PresentInterval.Two
                if (Description.PresentationInterval == PresentInterval.Immediate)
                {
                    // Prefer mailbox to immediate
                    if (presentMode == PresentModeKHR.ImmediateKhr)
                    {
                        swapChainPresentMode = PresentModeKHR.ImmediateKhr;
                    }
                    else if (presentMode == PresentModeKHR.MailboxKhr)
                    {
                        swapChainPresentMode = PresentModeKHR.MailboxKhr;
                        break;
                    }
                }
            }

            // Create swapchain
            var swapchainCreateInfo = new SwapchainCreateInfoKHR
            {
                SType = StructureType.SwapchainCreateInfoKhr,
                Surface = surface,
                ImageArrayLayers = 1,
                ImageSharingMode = SharingMode.Exclusive,
                ImageExtent = new Extent2D((uint)Description.BackBufferWidth, (uint)Description.BackBufferHeight),
                ImageFormat = backBufferFormat,
                ImageColorSpace = Description.ColorSpace == ColorSpace.Gamma ? ColorSpaceKHR.SpaceSrgbNonlinearKhr : 0,
                ImageUsage = ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.TransferDstBit | (surfaceCapabilities.SupportedUsageFlags & ImageUsageFlags.TransferSrcBit), // TODO VULKAN: Use off-screen buffer to emulate
                PresentMode = swapChainPresentMode,
                CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
                MinImageCount = desiredImageCount,
                PreTransform = preTransform,
                OldSwapchain = swapChain,
                Clipped = true
            };
            vk.TryGetDeviceExtension(GraphicsDevice.NativeInstance, GraphicsDevice.NativeDevice, out KhrSwapchain swapchainKhr);
            swapchainKhr.CreateSwapchain(GraphicsDevice.NativeDevice, &swapchainCreateInfo, null, out var newSwapChain);
            
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
            surface = new SurfaceKHR(surfaceHandle.Handle);
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

            var createInfo = new ImageViewCreateInfo
            {
                SType = StructureType.ImageViewCreateInfo,
                SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1),
                Format = backbuffer.NativeFormat,
                ViewType = ImageViewType.Type2D,
            };

            // We initialize swapchain images to PresentSource, since we swap them out while in this layout.
            backbuffer.NativeAccessMask = AccessFlags.MemoryReadBit;
            backbuffer.NativeLayout = ImageLayout.PresentSrcKhr;

            var imageMemoryBarrier = new ImageMemoryBarrier
            {
                SType = StructureType.ImageMemoryBarrier,
                SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1),
                OldLayout = ImageLayout.Undefined,
                NewLayout = ImageLayout.PresentSrcKhr,
                SrcAccessMask = AccessFlags.NoneKhr,
                DstAccessMask = AccessFlags.MemoryReadBit
            };

            var commandBufferAllocationInfo = new CommandBufferAllocateInfo
            {
                SType = StructureType.CommandBufferAllocateInfo,
                Level = CommandBufferLevel.Primary,
                CommandPool = GraphicsDevice.NativeCopyCommandPools.Value,
                CommandBufferCount = 1
            };
            CommandBuffer commandBuffer;

            vk.AllocateCommandBuffers(GraphicsDevice.NativeDevice, &commandBufferAllocationInfo, &commandBuffer);

            var beginInfo = new CommandBufferBeginInfo { SType = StructureType.CommandBufferBeginInfo };
            vk.BeginCommandBuffer(commandBuffer, &beginInfo);
            vk.TryGetDeviceExtension(GraphicsDevice.NativeInstance, GraphicsDevice.NativeDevice, out KhrSwapchain khrSwapchain);
            uint countBuffs = 0;

            khrSwapchain.GetSwapchainImages(GraphicsDevice.NativeDevice, swapChain, &countBuffs, null);

            Span<VK.Image> buffers = stackalloc VK.Image[(int)countBuffs];

            khrSwapchain.GetSwapchainImages(GraphicsDevice.NativeDevice, swapChain, &countBuffs, buffers);
            swapchainImages = new SwapChainImageInfo[countBuffs];

            for (int i = 0; i < countBuffs; i++)
            {
                // Create image views
                swapchainImages[i].NativeImage = createInfo.Image = buffers[i];
                vk.CreateImageView(GraphicsDevice.NativeDevice, &createInfo, null, out swapchainImages[i].NativeColorAttachmentView);

                // Transition to default layout
                imageMemoryBarrier.Image = buffers[i];
                vk.CmdPipelineBarrier(commandBuffer, PipelineStageFlags.AllCommandsBit, PipelineStageFlags.AllCommandsBit, 0, 0, null, 0, null, 1, &imageMemoryBarrier);
            }
            // Close and submit
            vk.EndCommandBuffer(commandBuffer);

            var submitInfo = new SubmitInfo
            {
                SType = StructureType.SubmitInfo,
                CommandBufferCount = 1,
                PCommandBuffers = &commandBuffer,
            };

            lock (GraphicsDevice.QueueLock)
            {
                vk.QueueSubmit(GraphicsDevice.NativeCommandQueue, 1, &submitInfo, new Fence());
                vk.QueueWaitIdle(GraphicsDevice.NativeCommandQueue);
            }

            vk.FreeCommandBuffers(GraphicsDevice.NativeDevice, GraphicsDevice.NativeCopyCommandPools.Value, 1, &commandBuffer);

            // Get next image
            uint currentBufferIndex;
            khrSwapchain.AcquireNextImage(GraphicsDevice.NativeDevice, swapChain, ulong.MaxValue, GraphicsDevice.GetNextPresentSemaphore(), new Fence(), &currentBufferIndex);
            
            // Apply the first swap chain image to the texture
            backbuffer.SetNativeHandles(swapchainImages[currentBufferIndex].NativeImage, swapchainImages[currentBufferIndex].NativeColorAttachmentView);
        }
    }
}
#endif
