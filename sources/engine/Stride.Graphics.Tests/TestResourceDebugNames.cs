// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12
using System;
using Silk.NET.Direct3D12;
using Xunit;

namespace Stride.Graphics.Tests;

public class TestResourceDebugNames : GraphicTestGameBase
{
    // WKPDID_D3DDebugObjectNameW, where ID3D12Object::SetName stores the name
    private static readonly Guid DebugObjectNameW = new(0x4cca5fd8, 0x921f, 0x42c8, 0x85, 0x66, 0x70, 0xca, 0xf2, 0xa9, 0xb7, 0x41);

    /// <summary>
    /// A name given to a resource after it is created reaches the native object, where debuggers read it.
    /// </summary>
    [Fact]
    public void NameGivenAfterCreationReachesTheNativeObject()
    {
        PerformTest(game =>
        {
            using var texture = Texture.New2D(game.GraphicsDevice, 4, 4, PixelFormat.R8G8B8A8_UNorm);
            using var buffer = Buffer.Vertex.New(game.GraphicsDevice, 64);

            texture.Name = "RenamedTexture";
            buffer.Name = "RenamedBuffer";

            Assert.Contains("RenamedTexture", GetNativeName(texture));
            Assert.Contains("RenamedBuffer", GetNativeName(buffer));
        });
    }

    /// <summary>
    /// Renaming a texture view leaves the name of its parent, whose native resource the view shares.
    /// </summary>
    [Fact]
    public void NameGivenToAViewKeepsTheParentName()
    {
        PerformTest(game =>
        {
            using var texture = Texture.New2D(game.GraphicsDevice, 4, 4, mipCount: 2, PixelFormat.R8G8B8A8_UNorm);
            texture.Name = "RenamedTexture";
            using var view = texture.ToTextureView(new TextureViewDescription { Type = ViewType.Single, MipLevel = 1, Format = texture.Format, Flags = TextureFlags.ShaderResource });

            view.Name = "RenamedView";

            // The view's own name starts with its parent's: only the parent's name is followed by its handle
            Assert.StartsWith("RenamedTexture (", GetNativeName(texture));
        });
    }

    private static unsafe string GetNativeName(GraphicsResource resource)
    {
        var guid = DebugObjectNameW;
        uint size = 0;
        resource.NativeDeviceChild.GetPrivateData(ref guid, ref size, null);
        if (size == 0)
            return string.Empty;

        var name = new char[size / sizeof(char)];
        fixed (char* namePointer = name)
            resource.NativeDeviceChild.GetPrivateData(ref guid, ref size, namePointer);
        return new string(name).TrimEnd('\0');
    }
}
#elif STRIDE_GRAPHICS_API_VULKAN
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Stride.Graphics.Regression;
using Vortice.Vulkan;
using Xunit;

namespace Stride.Graphics.Tests;

public class TestResourceDebugNames : GraphicTestGameBase
{
    private const string ImageLayoutError = "VUID-vkGetImageSubresourceLayout-image-07790";
    private const string TexelBufferUsageError = "VUID-VkBufferViewCreateInfo-buffer-00932";

    private static readonly List<string> reportedObjectNames = [];
    private static int reportCount;

    /// <summary>
    /// A name given to a texture after it is created reaches the validation layer, which reports it with the objects of its messages.
    /// </summary>
    [SkippableFact]
    [AllowGpuValidationError(GraphicsPlatform.Vulkan, ImageLayoutError)]
    public void NameGivenAfterCreationReachesTheValidationLayer()
    {
        RunGameTest(CreateGame(device =>
        {
            using var texture = Texture.New2D(device, 4, 4, PixelFormat.R8G8B8A8_UNorm);
            texture.Name = "RenamedTexture";

            Assert.Contains("RenamedTexture", ReportObjectNames(device, () => QueryLayout(device, texture)));
        }));
    }

    /// <summary>
    /// The debug name <see cref="Texture.New(GraphicsDevice, TextureDescription, DataBox[])"/> gives a texture when it creates it reaches the validation layer.
    /// </summary>
    [SkippableFact]
    [AllowGpuValidationError(GraphicsPlatform.Vulkan, ImageLayoutError)]
    public void NameGivenAtCreationReachesTheValidationLayer()
    {
        RunGameTest(CreateGame(device =>
        {
            using var texture = Texture.New(device, TextureDescription.New2D(4, 4, PixelFormat.R8G8B8A8_UNorm));

            Assert.Contains(texture.Name, ReportObjectNames(device, () => QueryLayout(device, texture)));
        }));
    }

    /// <summary>
    /// The name of a staging texture reaches the validation layer through the buffer that holds the texture.
    /// </summary>
    [SkippableFact]
    [AllowGpuValidationError(GraphicsPlatform.Vulkan, TexelBufferUsageError)]
    public void NameOfAStagingTextureReachesTheValidationLayer()
    {
        RunGameTest(CreateGame(device =>
        {
            using var texture = Texture.New2D(device, 4, 4, PixelFormat.R8G8B8A8_UNorm, TextureFlags.None, usage: GraphicsResourceUsage.Staging);
            texture.Name = "RenamedStagingTexture";

            Assert.Contains("RenamedStagingTexture", ReportObjectNames(device, () => CreateTexelView(device, texture)));
        }));
    }

    // RunGameTest, not PerformTest: the allowed error is looked up on the method that calls RunGameTest
    private static TestResourceDebugNames CreateGame(Action<GraphicsDevice> draw)
    {
        var game = new TestResourceDebugNames();
        game.FrameGameSystem.IsUnitTestFeeding = true;
        game.FrameGameSystem.Draw(() =>
        {
            Skip.IfNot(game.GraphicsDevice.IsProfilingSupported, "VK_EXT_debug_utils is not available.");
            draw(game.GraphicsDevice);
        });
        return game;
    }

    private static List<string> ReportObjectNames(GraphicsDevice device, Action provokeError)
    {
        reportedObjectNames.Clear();
        reportCount = 0;
        var messenger = CreateMessenger(device);
        // Detached, the device's own messenger logs the provoked error without attributing it to the frame's scopes
        GraphicsDevice.DebugMessengerDevice = null;
        try
        {
            provokeError();
        }
        finally
        {
            GraphicsDevice.DebugMessengerDevice = device;
            DestroyMessenger(device, messenger);
        }

        Skip.If(reportCount == 0, "The Vulkan validation layer is not available.");
        return reportedObjectNames;
    }

    // An optimally tiled image has no queryable layout: the layer reports it, with the image among its objects
    private static unsafe void QueryLayout(GraphicsDevice device, Texture texture)
    {
        var subresource = new VkImageSubresource { aspectMask = VkImageAspectFlags.Color };
        device.NativeDeviceApi.vkGetImageSubresourceLayout(device.NativeDevice, texture.NativeImage, &subresource, out _);
    }

    // The buffer of a staging texture is not a texel buffer: the layer reports a view of it, with the buffer among its objects
    private static unsafe void CreateTexelView(GraphicsDevice device, Texture texture)
    {
        var createInfo = new VkBufferViewCreateInfo
        {
            sType = VkStructureType.BufferViewCreateInfo,
            buffer = texture.NativeBuffer,
            format = VkFormat.R8G8B8A8Unorm,
            range = (ulong) texture.SizeInBytes,
        };
        device.NativeDeviceApi.vkCreateBufferView(device.NativeDevice, &createInfo, null, out var view);
        if (view != VkBufferView.Null)
            device.NativeDeviceApi.vkDestroyBufferView(device.NativeDevice, view, null);
    }

    private static unsafe VkDebugUtilsMessengerEXT CreateMessenger(GraphicsDevice device)
    {
        var createInfo = new VkDebugUtilsMessengerCreateInfoEXT
        {
            sType = VkStructureType.DebugUtilsMessengerCreateInfoEXT,
            messageSeverity = VkDebugUtilsMessageSeverityFlagsEXT.Error,
            messageType = VkDebugUtilsMessageTypeFlagsEXT.Validation,
            pfnUserCallback = &CollectObjectNames,
        };
        var instance = GraphicsAdapterFactory.GetInstance(enableValidation: true).NativeInstance;
        device.NativeInstanceApi.vkCreateDebugUtilsMessengerEXT(instance, &createInfo, null, out var messenger);
        return messenger;
    }

    private static unsafe void DestroyMessenger(GraphicsDevice device, VkDebugUtilsMessengerEXT messenger)
    {
        var instance = GraphicsAdapterFactory.GetInstance(enableValidation: true).NativeInstance;
        device.NativeInstanceApi.vkDestroyDebugUtilsMessengerEXT(instance, messenger, null);
    }

    [UnmanagedCallersOnly]
    private static unsafe uint CollectObjectNames(VkDebugUtilsMessageSeverityFlagsEXT severity, VkDebugUtilsMessageTypeFlagsEXT types, VkDebugUtilsMessengerCallbackDataEXT* data, void* userData)
    {
        reportCount++;
        for (var i = 0u; i < data->objectCount; i++)
        {
            if (data->pObjects[i].pObjectName != null)
                reportedObjectNames.Add(Marshal.PtrToStringUTF8((nint) data->pObjects[i].pObjectName));
        }
        return 0;
    }
}
#endif
