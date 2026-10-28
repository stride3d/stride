// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_VULKAN

using System;
using Vortice.Vulkan;

namespace Stride.Graphics;

public sealed partial class ExportableTexture
{
    private readonly GraphicsDevice device;
    private VkSemaphore readySemaphore;
    private VkSemaphore releasedSemaphore;

    private ExportableTexture(GraphicsDevice device, Texture texture)
    {
        this.device = device;
        Texture = texture;
        MemorySize = texture.ExportableMemorySize;
        readySemaphore = CreateExportableSemaphore(device);
        releasedSemaphore = CreateExportableSemaphore(device);
    }

    private static partial bool IsSupportedImpl(GraphicsDevice device) => device.HasExternalObjectsSupport;

    private static partial ExportableTexture CreateImpl(GraphicsDevice device, int width, int height)
    {
        var texture = new Texture(device) { IsMemoryExportable = true };
        texture.InitializeFrom(TextureDescription.New2D(width, height, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget | TextureFlags.ShaderResource));
        return new ExportableTexture(device, texture);
    }

    /// <summary>
    ///   Exports the memory of <see cref="Texture"/> as a new file descriptor, which the caller (or the importer) owns.
    /// </summary>
    /// <returns>The file descriptor.</returns>
    public unsafe int ExportMemory()
    {
        var getInfo = new VkMemoryGetFdInfoKHR
        {
            sType = VkStructureType.MemoryGetFDInfoKHR,
            memory = Texture.NativeMemory,
            handleType = VkExternalMemoryHandleTypeFlags.OpaqueFD,
        };
        int fd;
        device.CheckResult(device.NativeDeviceApi.vkGetMemoryFdKHR(device.NativeDevice, &getInfo, &fd));
        return fd;
    }

    /// <summary>
    ///   Exports the semaphore signaled when a frame is ready (<see cref="SignalReady"/>) as a new file descriptor,
    ///   which the caller (or the importer) owns.
    /// </summary>
    /// <returns>The file descriptor.</returns>
    public int ExportReadySemaphore() => ExportSemaphore(readySemaphore);

    /// <summary>
    ///   Exports the semaphore the importer signals when it is done with a frame (<see cref="WaitReleased"/>) as a new
    ///   file descriptor, which the caller (or the importer) owns.
    /// </summary>
    /// <returns>The file descriptor.</returns>
    public int ExportReleasedSemaphore() => ExportSemaphore(releasedSemaphore);

    /// <summary>
    ///   Signals the ready semaphore after every command submitted so far (after the command list of the copy was submitted).
    /// </summary>
    /// <remarks>
    ///   The signal of an empty batch covers every command submitted before it on the queue.
    /// </remarks>
    public unsafe void SignalReady()
    {
        var signalInfo = new VkSemaphoreSubmitInfo
        {
            sType = VkStructureType.SemaphoreSubmitInfo,
            semaphore = readySemaphore,
            stageMask = VkPipelineStageFlags2.AllCommands,
        };
        var submitInfo = new VkSubmitInfo2
        {
            sType = VkStructureType.SubmitInfo2,
            signalSemaphoreInfoCount = 1,
            pSignalSemaphoreInfos = &signalInfo,
        };
        lock (device.QueueLock)
            device.CheckResult(device.NativeDeviceApi.vkQueueSubmit2(device.NativeCommandQueue, 1, &submitInfo, VkFence.Null));
    }

    /// <summary>
    ///   Makes the commands submitted from now on wait for the importer to release the previous frame.
    /// </summary>
    /// <remarks>
    ///   The wait of an empty batch also holds the batches submitted after it on the queue. The importer must have
    ///   submitted its signal before (a binary semaphore is waited for after its signal is submitted).
    /// </remarks>
    public unsafe void WaitReleased()
    {
        var waitInfo = new VkSemaphoreSubmitInfo
        {
            sType = VkStructureType.SemaphoreSubmitInfo,
            semaphore = releasedSemaphore,
            stageMask = VkPipelineStageFlags2.AllCommands,
        };
        var submitInfo = new VkSubmitInfo2
        {
            sType = VkStructureType.SubmitInfo2,
            waitSemaphoreInfoCount = 1,
            pWaitSemaphoreInfos = &waitInfo,
        };
        lock (device.QueueLock)
            device.CheckResult(device.NativeDeviceApi.vkQueueSubmit2(device.NativeCommandQueue, 1, &submitInfo, VkFence.Null));
    }

    /// <inheritdoc />
    public unsafe void Dispose()
    {
        // The importer is done with the Texture when it is disposed: release it once the GPU is done too
        lock (device.QueueLock)
            device.NativeDeviceApi.vkQueueWaitIdle(device.NativeCommandQueue);
        if (readySemaphore != VkSemaphore.Null)
            device.NativeDeviceApi.vkDestroySemaphore(device.NativeDevice, readySemaphore, null);
        if (releasedSemaphore != VkSemaphore.Null)
            device.NativeDeviceApi.vkDestroySemaphore(device.NativeDevice, releasedSemaphore, null);
        readySemaphore = releasedSemaphore = VkSemaphore.Null;
        Texture?.Dispose();
        Texture = null;
    }

    private static unsafe VkSemaphore CreateExportableSemaphore(GraphicsDevice device)
    {
        var exportInfo = new VkExportSemaphoreCreateInfo
        {
            sType = VkStructureType.ExportSemaphoreCreateInfo,
            handleTypes = VkExternalSemaphoreHandleTypeFlags.OpaqueFD,
        };
        var createInfo = new VkSemaphoreCreateInfo
        {
            sType = VkStructureType.SemaphoreCreateInfo,
            pNext = &exportInfo,
        };
        device.CheckResult(device.NativeDeviceApi.vkCreateSemaphore(device.NativeDevice, &createInfo, null, out var semaphore));
        return semaphore;
    }

    private unsafe int ExportSemaphore(VkSemaphore semaphore)
    {
        var getInfo = new VkSemaphoreGetFdInfoKHR
        {
            sType = VkStructureType.SemaphoreGetFDInfoKHR,
            semaphore = semaphore,
            handleType = VkExternalSemaphoreHandleTypeFlags.OpaqueFD,
        };
        int fd;
        device.CheckResult(device.NativeDeviceApi.vkGetSemaphoreFdKHR(device.NativeDevice, &getInfo, &fd));
        return fd;
    }
}

#endif
