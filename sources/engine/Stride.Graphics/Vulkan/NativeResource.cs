// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using VK = Silk.NET.Vulkan;
using static Silk.NET.Vulkan.Vk;

namespace Stride.Graphics
{
    internal struct NativeResource
    {
        public VK.DebugReportObjectTypeEXT type;

        public ulong handle;

        public NativeResource(VK.DebugReportObjectTypeEXT type, ulong handle)
        {
            this.type = type;
            this.handle = handle;
        }

        public static unsafe implicit operator NativeResource(VK.Buffer handle)
        {
            return new NativeResource(VK.DebugReportObjectTypeEXT.BufferExt, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(VK.BufferView handle)
        {
            return new NativeResource(VK.DebugReportObjectTypeEXT.BufferViewExt, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(VK.Image handle)
        {
            return new NativeResource(VK.DebugReportObjectTypeEXT.ImageExt, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(VK.ImageView handle)
        {
            return new NativeResource(VK.DebugReportObjectTypeEXT.ImageViewExt, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(VK.DeviceMemory handle)
        {
            return new NativeResource(VK.DebugReportObjectTypeEXT.DeviceMemoryExt, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(VK.Sampler handle)
        {
            return new NativeResource(VK.DebugReportObjectTypeEXT.SamplerExt, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(VK.Framebuffer handle)
        {
            return new NativeResource(VK.DebugReportObjectTypeEXT.FramebufferExt, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(VK.Semaphore handle)
        {
            return new NativeResource(VK.DebugReportObjectTypeEXT.SemaphoreExt, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(VK.Fence handle)
        {
            return new NativeResource(VK.DebugReportObjectTypeEXT.FenceExt, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(VK.QueryPool handle)
        {
            return new NativeResource(VK.DebugReportObjectTypeEXT.QueryPoolExt, *(ulong*)&handle);
        }

        public unsafe void Destroy(GraphicsDevice device)
        {
            var handleCopy = handle;

            switch (type)
            {
                case VK.DebugReportObjectTypeEXT.BufferExt:
                    GetApi().DestroyBuffer(device.NativeDevice, *(VK.Buffer*)&handleCopy, null);
                    break;
                case VK.DebugReportObjectTypeEXT.BufferViewExt:
                    GetApi().DestroyBufferView(device.NativeDevice, *(VK.BufferView*)&handleCopy, null);
                    break;
                case VK.DebugReportObjectTypeEXT.ImageExt:
                    GetApi().DestroyImage(device.NativeDevice, *(VK.Image*)&handleCopy, null);
                    break;
                case VK.DebugReportObjectTypeEXT.ImageViewExt:
                    GetApi().DestroyImageView(device.NativeDevice, *(VK.ImageView*)&handleCopy, null);
                    break;
                case VK.DebugReportObjectTypeEXT.DeviceMemoryExt:
                    GetApi().FreeMemory(device.NativeDevice, *(VK.DeviceMemory*)&handleCopy, null);
                    break;
                case VK.DebugReportObjectTypeEXT.SamplerExt:
                    GetApi().DestroySampler(device.NativeDevice, *(VK.Sampler*)&handleCopy, null);
                    break;
                case VK.DebugReportObjectTypeEXT.FramebufferExt:
                    GetApi().DestroyFramebuffer(device.NativeDevice, *(VK.Framebuffer*)&handleCopy, null);
                    break;
                case VK.DebugReportObjectTypeEXT.SemaphoreExt:
                    GetApi().DestroySemaphore(device.NativeDevice, *(VK.Semaphore*)&handleCopy, null);
                    break;
                case VK.DebugReportObjectTypeEXT.FenceExt:
                    GetApi().DestroyFence(device.NativeDevice, *(VK.Fence*)&handleCopy, null);
                    break;
                case VK.DebugReportObjectTypeEXT.QueryPoolExt:
                    GetApi().DestroyQueryPool(device.NativeDevice, *(VK.QueryPool*)&handleCopy, null);
                    break;
            }
        }
    }
}
#endif
