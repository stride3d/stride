// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// Headless Vulkan renderer consuming the mixer's SPIR-V directly (no SPIRV-Cross translation).
/// No window or swapchain: renders into an offscreen image and reads it back, so it also works
/// on display-less CI and mobile. Requires bytecode compiled with the unified binding scheme
/// (ShaderMixer.Options.ResourcesRegisterSeparate = false).
/// Uses Vortice.Vulkan, like the engine's own Vulkan backend.
/// </summary>
public unsafe class VulkanFrameRenderer(uint width = 800, uint height = 600, byte[] spirv = null, byte[] fallbackVertexSpirv = null) : FrameRenderer(width, height)
{
    private readonly byte[] spirv = spirv;
    private readonly byte[] fallbackVertexSpirv = fallbackVertexSpirv;

    // Shared across all tests: drivers (especially Lavapipe, which embeds LLVM) don't support
    // being repeatedly initialized and torn down in one process, and reuse is much faster.
    private static VkInstanceApi instanceApi;
    private static VkDeviceApi deviceApi;
    private static VkInstance instance;
    private static VkPhysicalDevice physicalDevice;
    private static VkDevice device;
    private static uint queueFamily;
    private static VkQueue queue;
    private static VkCommandPool commandPool;

    private VkShaderModule shaderModule;
    private VkShaderModule fallbackVertexModule;
    private SpirvProbe probe;
    private SpirvProbe fallbackVertexProbe;

    private VkDescriptorSetLayout descriptorSetLayout;
    private VkDescriptorPool descriptorPool;
    private VkDescriptorSet descriptorSet;
    private VkPipelineLayout pipelineLayout;

    // Everything created during a test, destroyed in PresentAndFinish
    private readonly List<VkBuffer> buffers = [];
    private readonly List<VkBufferView> bufferViews = [];
    private readonly List<VkImage> images = [];
    private readonly List<VkImageView> imageViews = [];
    private readonly List<VkSampler> samplers = [];
    private readonly List<VkDeviceMemory> memories = [];
    private readonly List<VkPipeline> pipelines = [];
    private readonly List<VkRenderPass> renderPasses = [];
    private readonly List<VkFramebuffer> framebuffers = [];

    private static void Check(VkResult result, [CallerArgumentExpression(nameof(result))] string call = null)
    {
        if (result != VkResult.Success)
            throw new InvalidOperationException($"{call} failed: {result}");
    }

    private static bool? available;

    /// <summary>
    /// Whether a usable Vulkan 1.2+ device exists (loader present, instance and device selection succeed).
    /// </summary>
    public static bool CheckAvailable()
    {
        if (available == null)
        {
            try
            {
                EnsureSharedContext();
                available = true;
            }
            catch
            {
                available = false;
            }
        }
        return available.Value;
    }

    private static void EnsureSharedContext()
    {
        if (instanceApi != null)
            return;

        Check(vkInitialize());
        CreateInstance();
        PickPhysicalDevice();
        CreateDevice();

        var commandPoolCreateInfo = new VkCommandPoolCreateInfo
        {
            sType = VkStructureType.CommandPoolCreateInfo,
            flags = VkCommandPoolCreateFlags.ResetCommandBuffer,
            queueFamilyIndex = queueFamily,
        };
        Check(deviceApi.vkCreateCommandPool(device, &commandPoolCreateInfo, null, out commandPool));
    }

    public override void SetupTest()
    {
        EnsureSharedContext();

        probe = new SpirvProbe(spirv);
        Check(deviceApi.vkCreateShaderModule(device, spirv, null, out shaderModule));
        if (fallbackVertexSpirv != null)
        {
            fallbackVertexProbe = new SpirvProbe(fallbackVertexSpirv);
            Check(deviceApi.vkCreateShaderModule(device, fallbackVertexSpirv, null, out fallbackVertexModule));
        }
    }

    private static void CreateInstance()
    {
        var appInfo = new VkApplicationInfo
        {
            sType = VkStructureType.ApplicationInfo,
            apiVersion = VkVersion.Version_1_3,
        };
        var createInfo = new VkInstanceCreateInfo
        {
            sType = VkStructureType.InstanceCreateInfo,
            pApplicationInfo = &appInfo,
        };
        var result = vkCreateInstance(&createInfo, out VkInstance createdInstance);
        if (result == VkResult.ErrorIncompatibleDriver)
        {
            appInfo.apiVersion = VkVersion.Version_1_2;
            result = vkCreateInstance(&createInfo, out createdInstance);
        }
        Check(result, "vkCreateInstance");
        instance = createdInstance;
        instanceApi = GetApi(instance);
    }

    private static void PickPhysicalDevice()
    {
        uint count = 0;
        Check(instanceApi.vkEnumeratePhysicalDevices(instance, &count, null));
        if (count == 0)
            throw new InvalidOperationException("No Vulkan physical device");
        var devices = new VkPhysicalDevice[count];
        fixed (VkPhysicalDevice* devicesPtr = devices)
            Check(instanceApi.vkEnumeratePhysicalDevices(instance, &count, devicesPtr));

        // SPIR-V 1.4 modules require a Vulkan 1.2+ device.
        // Software rendering (see Module) selects a CPU device (Lavapipe); otherwise prefer real GPUs.
        var softwareRendering = Environment.GetEnvironmentVariable("STRIDE_GRAPHICS_SOFTWARE_RENDERING") == "1";
        var best = -1;
        var bestScore = -1;
        for (int i = 0; i < count; i++)
        {
            instanceApi.vkGetPhysicalDeviceProperties(devices[i], out var properties);
            if (properties.apiVersion < VkVersion.Version_1_2)
                continue;
            var isCpu = properties.deviceType == VkPhysicalDeviceType.Cpu;
            var score = softwareRendering
                ? (isCpu ? 3 : 0)
                : properties.deviceType switch
                {
                    VkPhysicalDeviceType.DiscreteGpu => 3,
                    VkPhysicalDeviceType.IntegratedGpu => 2,
                    _ => 1,
                };
            if (score > bestScore)
            {
                bestScore = score;
                best = i;
            }
        }
        if (best < 0)
            throw new InvalidOperationException("No Vulkan 1.2+ physical device");
        if (softwareRendering && bestScore < 3)
            throw new InvalidOperationException("Software rendering requested but no CPU Vulkan device (Lavapipe) found");
        physicalDevice = devices[best];
    }

    private static void CreateDevice()
    {
        uint familyCount = 0;
        instanceApi.vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, &familyCount, null);
        var families = new VkQueueFamilyProperties[familyCount];
        fixed (VkQueueFamilyProperties* familiesPtr = families)
            instanceApi.vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, &familyCount, familiesPtr);
        queueFamily = uint.MaxValue;
        for (uint i = 0; i < familyCount; i++)
        {
            if ((families[i].queueFlags & (VkQueueFlags.Graphics | VkQueueFlags.Compute)) == (VkQueueFlags.Graphics | VkQueueFlags.Compute))
            {
                queueFamily = i;
                break;
            }
        }
        if (queueFamily == uint.MaxValue)
            throw new InvalidOperationException("No graphics+compute queue family");

        // The bytecode keeps UserSemantic decorations (SPV_GOOGLE_hlsl_functionality1);
        // enable the matching extension where the driver wants it declared.
        uint extensionCount = 0;
        Check(instanceApi.vkEnumerateDeviceExtensionProperties(physicalDevice, null, &extensionCount, null));
        var extensions = new VkExtensionProperties[extensionCount];
        fixed (VkExtensionProperties* extensionsPtr = extensions)
            Check(instanceApi.vkEnumerateDeviceExtensionProperties(physicalDevice, null, &extensionCount, extensionsPtr));
        var availableExtensions = new HashSet<string>();
        for (int i = 0; i < extensions.Length; i++)
        {
            fixed (VkExtensionProperties* extensionPtr = &extensions[i])
                availableExtensions.Add(Marshal.PtrToStringUTF8((nint)extensionPtr->extensionName));
        }
        var enabledExtensions = new List<string>();
        foreach (var wanted in new[] { "VK_GOOGLE_hlsl_functionality1", "VK_GOOGLE_user_type" })
        {
            if (availableExtensions.Contains(wanted))
                enabledExtensions.Add(wanted);
        }

        var priority = 1.0f;
        var queueCreateInfo = new VkDeviceQueueCreateInfo
        {
            sType = VkStructureType.DeviceQueueCreateInfo,
            queueFamilyIndex = queueFamily,
            queueCount = 1,
            pQueuePriorities = &priority,
        };
        using var ppEnabledExtensionNames = new VkStringArray(enabledExtensions);
        var createInfo = new VkDeviceCreateInfo
        {
            sType = VkStructureType.DeviceCreateInfo,
            queueCreateInfoCount = 1,
            pQueueCreateInfos = &queueCreateInfo,
            enabledExtensionCount = ppEnabledExtensionNames.Length,
            ppEnabledExtensionNames = ppEnabledExtensionNames,
        };
        Check(instanceApi.vkCreateDevice(physicalDevice, in createInfo, null, out device));
        deviceApi = new VkDeviceApi(instanceApi, in device);
        deviceApi.vkGetDeviceQueue(device, queueFamily, 0, out queue);
    }

    private static uint FindMemoryType(uint typeBits, VkMemoryPropertyFlags required)
    {
        instanceApi.vkGetPhysicalDeviceMemoryProperties(physicalDevice, out var memoryProperties);
        for (uint i = 0; i < memoryProperties.memoryTypeCount; i++)
        {
            if ((typeBits & (1u << (int)i)) != 0 && (memoryProperties.memoryTypes[(int)i].propertyFlags & required) == required)
                return i;
        }
        throw new InvalidOperationException("No suitable memory type");
    }

    private (VkBuffer Buffer, VkDeviceMemory Memory) CreateHostBuffer(ulong size, VkBufferUsageFlags usage, ReadOnlySpan<byte> data = default)
    {
        var createInfo = new VkBufferCreateInfo
        {
            sType = VkStructureType.BufferCreateInfo,
            size = size,
            usage = usage,
            sharingMode = VkSharingMode.Exclusive,
        };
        Check(deviceApi.vkCreateBuffer(device, &createInfo, null, out var buffer));
        deviceApi.vkGetBufferMemoryRequirements(device, buffer, out var requirements);
        var allocateInfo = new VkMemoryAllocateInfo
        {
            sType = VkStructureType.MemoryAllocateInfo,
            allocationSize = requirements.size,
            memoryTypeIndex = FindMemoryType(requirements.memoryTypeBits, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent),
        };
        Check(deviceApi.vkAllocateMemory(device, &allocateInfo, null, out var memory));
        Check(deviceApi.vkBindBufferMemory(device, buffer, memory, 0));
        if (!data.IsEmpty)
        {
            void* mapped;
            Check(deviceApi.vkMapMemory(device, memory, 0, size, 0, &mapped));
            data.CopyTo(new Span<byte>(mapped, (int)size));
            deviceApi.vkUnmapMemory(device, memory);
        }
        buffers.Add(buffer);
        memories.Add(memory);
        return (buffer, memory);
    }

    private (VkImage Image, VkImageView View) CreateImage2D(uint imageWidth, uint imageHeight, VkImageUsageFlags usage)
    {
        var createInfo = new VkImageCreateInfo
        {
            sType = VkStructureType.ImageCreateInfo,
            imageType = VkImageType.Image2D,
            format = VkFormat.R8G8B8A8Unorm,
            extent = new VkExtent3D(imageWidth, imageHeight, 1),
            mipLevels = 1,
            arrayLayers = 1,
            samples = VkSampleCountFlags.Count1,
            tiling = VkImageTiling.Optimal,
            usage = usage,
            initialLayout = VkImageLayout.Undefined,
            sharingMode = VkSharingMode.Exclusive,
        };
        Check(deviceApi.vkCreateImage(device, &createInfo, null, out var image));
        deviceApi.vkGetImageMemoryRequirements(device, image, out var requirements);
        var allocateInfo = new VkMemoryAllocateInfo
        {
            sType = VkStructureType.MemoryAllocateInfo,
            allocationSize = requirements.size,
            memoryTypeIndex = FindMemoryType(requirements.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal),
        };
        Check(deviceApi.vkAllocateMemory(device, &allocateInfo, null, out var memory));
        Check(deviceApi.vkBindImageMemory(device, image, memory, 0));

        var viewCreateInfo = new VkImageViewCreateInfo
        {
            sType = VkStructureType.ImageViewCreateInfo,
            image = image,
            viewType = VkImageViewType.Image2D,
            format = VkFormat.R8G8B8A8Unorm,
            subresourceRange = new VkImageSubresourceRange(VkImageAspectFlags.Color, 0, 1, 0, 1),
        };
        Check(deviceApi.vkCreateImageView(device, &viewCreateInfo, null, out var view));

        images.Add(image);
        imageViews.Add(view);
        memories.Add(memory);
        return (image, view);
    }

    private void OneTimeCommands(Action<VkCommandBuffer> record)
    {
        var allocateInfo = new VkCommandBufferAllocateInfo
        {
            sType = VkStructureType.CommandBufferAllocateInfo,
            commandPool = commandPool,
            level = VkCommandBufferLevel.Primary,
            commandBufferCount = 1,
        };
        VkCommandBuffer commandBuffer;
        Check(deviceApi.vkAllocateCommandBuffers(device, &allocateInfo, &commandBuffer));
        var beginInfo = new VkCommandBufferBeginInfo
        {
            sType = VkStructureType.CommandBufferBeginInfo,
            flags = VkCommandBufferUsageFlags.OneTimeSubmit,
        };
        Check(deviceApi.vkBeginCommandBuffer(commandBuffer, &beginInfo));
        record(commandBuffer);
        Check(deviceApi.vkEndCommandBuffer(commandBuffer));

        var fenceCreateInfo = new VkFenceCreateInfo { sType = VkStructureType.FenceCreateInfo };
        Check(deviceApi.vkCreateFence(device, &fenceCreateInfo, null, out var fence));
        var submitInfo = new VkSubmitInfo
        {
            sType = VkStructureType.SubmitInfo,
            commandBufferCount = 1,
            pCommandBuffers = &commandBuffer,
        };
        Check(deviceApi.vkQueueSubmit(queue, 1, &submitInfo, fence));
        Check(deviceApi.vkWaitForFences(device, 1, &fence, true, ulong.MaxValue));
        deviceApi.vkDestroyFence(device, fence, null);
        deviceApi.vkFreeCommandBuffers(device, commandPool, 1, &commandBuffer);
    }

    private static void TransitionImage(VkCommandBuffer commandBuffer, VkImage image, VkImageLayout oldLayout, VkImageLayout newLayout)
    {
        var barrier = new VkImageMemoryBarrier
        {
            sType = VkStructureType.ImageMemoryBarrier,
            srcAccessMask = VkAccessFlags.MemoryWrite,
            dstAccessMask = VkAccessFlags.MemoryRead | VkAccessFlags.MemoryWrite,
            oldLayout = oldLayout,
            newLayout = newLayout,
            srcQueueFamilyIndex = uint.MaxValue, // VK_QUEUE_FAMILY_IGNORED
            dstQueueFamilyIndex = uint.MaxValue,
            image = image,
            subresourceRange = new VkImageSubresourceRange(VkImageAspectFlags.Color, 0, 1, 0, 1),
        };
        deviceApi.vkCmdPipelineBarrier(commandBuffer, VkPipelineStageFlags.AllCommands, VkPipelineStageFlags.AllCommands,
            VkDependencyFlags.None, 0, null, 0, null, 1, &barrier);
    }

    /// <summary>
    /// Creates one descriptor per reflection resource binding (binding number = SlotStart, set 0),
    /// filling contents from the matching test parameter when present, defaults otherwise.
    /// </summary>
    private void SetupDescriptors()
    {
        var parameterValues = new Dictionary<int, string>();
        foreach (var (parameterBinding, _, value) in MatchResourceParameters())
            parameterValues[parameterBinding.SlotStart] = value;

        var bindings = new List<VkDescriptorSetLayoutBinding>();
        var poolSizes = new Dictionary<VkDescriptorType, uint>();
        // Deferred: descriptor writes need stable pointers, applied after the set is allocated
        var writes = new List<(uint Binding, VkDescriptorType Type, VkDescriptorBufferInfo BufferInfo, VkDescriptorImageInfo ImageInfo, VkBufferView TexelView)>();

        foreach (var binding in EffectReflection.ResourceBindings)
        {
            var descriptorType = GetDescriptorType(binding);
            bindings.Add(new VkDescriptorSetLayoutBinding
            {
                binding = (uint)binding.SlotStart,
                descriptorType = descriptorType,
                descriptorCount = 1,
                stageFlags = VkShaderStageFlags.All,
            });
            poolSizes[descriptorType] = poolSizes.GetValueOrDefault(descriptorType) + 1;

            parameterValues.TryGetValue(binding.SlotStart, out var value);
            switch (descriptorType)
            {
                case VkDescriptorType.UniformBuffer:
                {
                    var data = value != null
                        ? BuildCBufferData(binding.RawName, value)
                        : new byte[FindCBuffer(binding.RawName).Size];
                    var (buffer, _) = CreateHostBuffer((ulong)data.Length, VkBufferUsageFlags.UniformBuffer, data);
                    writes.Add(((uint)binding.SlotStart, descriptorType, new VkDescriptorBufferInfo { buffer = buffer, offset = 0, range = ulong.MaxValue }, default, default));
                    break;
                }
                case VkDescriptorType.StorageBuffer:
                {
                    var (buffer, _) = CreateHostBuffer(1024, VkBufferUsageFlags.StorageBuffer, new byte[1024]);
                    writes.Add(((uint)binding.SlotStart, descriptorType, new VkDescriptorBufferInfo { buffer = buffer, offset = 0, range = ulong.MaxValue }, default, default));
                    break;
                }
                case VkDescriptorType.UniformTexelBuffer:
                case VkDescriptorType.StorageTexelBuffer:
                {
                    var color = value != null ? ParseColor(value) : 0;
                    var (buffer, _) = CreateHostBuffer(sizeof(uint),
                        descriptorType == VkDescriptorType.UniformTexelBuffer ? VkBufferUsageFlags.UniformTexelBuffer : VkBufferUsageFlags.StorageTexelBuffer,
                        new ReadOnlySpan<byte>(&color, sizeof(uint)));
                    var viewCreateInfo = new VkBufferViewCreateInfo
                    {
                        sType = VkStructureType.BufferViewCreateInfo,
                        buffer = buffer,
                        format = VkFormat.R8G8B8A8Unorm,
                        offset = 0,
                        range = ulong.MaxValue,
                    };
                    Check(deviceApi.vkCreateBufferView(device, &viewCreateInfo, null, out var texelView));
                    bufferViews.Add(texelView);
                    writes.Add(((uint)binding.SlotStart, descriptorType, default, default, texelView));
                    break;
                }
                case VkDescriptorType.SampledImage:
                {
                    var color = value != null ? ParseColor(value) : 0;
                    var (image, view) = CreateImage2D(1, 1, VkImageUsageFlags.Sampled | VkImageUsageFlags.TransferDst);
                    OneTimeCommands(commandBuffer =>
                    {
                        TransitionImage(commandBuffer, image, VkImageLayout.Undefined, VkImageLayout.TransferDstOptimal);
                        var clearColor = new VkClearColorValue(
                            (color & 0xFF) / 255.0f,
                            ((color >> 8) & 0xFF) / 255.0f,
                            ((color >> 16) & 0xFF) / 255.0f,
                            ((color >> 24) & 0xFF) / 255.0f);
                        var range = new VkImageSubresourceRange(VkImageAspectFlags.Color, 0, 1, 0, 1);
                        deviceApi.vkCmdClearColorImage(commandBuffer, image, VkImageLayout.TransferDstOptimal, &clearColor, 1, &range);
                        TransitionImage(commandBuffer, image, VkImageLayout.TransferDstOptimal, VkImageLayout.ShaderReadOnlyOptimal);
                    });
                    writes.Add(((uint)binding.SlotStart, descriptorType, default, new VkDescriptorImageInfo { imageView = view, imageLayout = VkImageLayout.ShaderReadOnlyOptimal }, default));
                    break;
                }
                case VkDescriptorType.StorageImage:
                {
                    var (image, view) = CreateImage2D(1, 1, VkImageUsageFlags.Storage);
                    OneTimeCommands(commandBuffer => TransitionImage(commandBuffer, image, VkImageLayout.Undefined, VkImageLayout.General));
                    writes.Add(((uint)binding.SlotStart, descriptorType, default, new VkDescriptorImageInfo { imageView = view, imageLayout = VkImageLayout.General }, default));
                    break;
                }
                case VkDescriptorType.Sampler:
                {
                    var samplerCreateInfo = new VkSamplerCreateInfo
                    {
                        sType = VkStructureType.SamplerCreateInfo,
                        magFilter = VkFilter.Nearest,
                        minFilter = VkFilter.Nearest,
                        mipmapMode = VkSamplerMipmapMode.Nearest,
                        addressModeU = VkSamplerAddressMode.Repeat,
                        addressModeV = VkSamplerAddressMode.Repeat,
                        addressModeW = VkSamplerAddressMode.Repeat,
                    };
                    Check(deviceApi.vkCreateSampler(device, &samplerCreateInfo, null, out var sampler));
                    samplers.Add(sampler);
                    writes.Add(((uint)binding.SlotStart, descriptorType, default, new VkDescriptorImageInfo { sampler = sampler }, default));
                    break;
                }
            }
        }

        var bindingsArray = bindings.ToArray();
        fixed (VkDescriptorSetLayoutBinding* bindingsPtr = bindingsArray)
        {
            var layoutCreateInfo = new VkDescriptorSetLayoutCreateInfo
            {
                sType = VkStructureType.DescriptorSetLayoutCreateInfo,
                bindingCount = (uint)bindingsArray.Length,
                pBindings = bindingsPtr,
            };
            Check(deviceApi.vkCreateDescriptorSetLayout(device, &layoutCreateInfo, null, out descriptorSetLayout));
        }

        var setLayout = descriptorSetLayout;
        var layoutCreate = new VkPipelineLayoutCreateInfo
        {
            sType = VkStructureType.PipelineLayoutCreateInfo,
            setLayoutCount = 1,
            pSetLayouts = &setLayout,
        };
        Check(deviceApi.vkCreatePipelineLayout(device, &layoutCreate, null, out pipelineLayout));

        if (bindingsArray.Length > 0)
        {
            var poolSizesArray = poolSizes.Select(x => new VkDescriptorPoolSize { type = x.Key, descriptorCount = x.Value }).ToArray();
            fixed (VkDescriptorPoolSize* poolSizesPtr = poolSizesArray)
            {
                var poolCreateInfo = new VkDescriptorPoolCreateInfo
                {
                    sType = VkStructureType.DescriptorPoolCreateInfo,
                    maxSets = 1,
                    poolSizeCount = (uint)poolSizesArray.Length,
                    pPoolSizes = poolSizesPtr,
                };
                Check(deviceApi.vkCreateDescriptorPool(device, &poolCreateInfo, null, out descriptorPool));
            }

            var allocateInfo = new VkDescriptorSetAllocateInfo
            {
                sType = VkStructureType.DescriptorSetAllocateInfo,
                descriptorPool = descriptorPool,
                descriptorSetCount = 1,
                pSetLayouts = &setLayout,
            };
            VkDescriptorSet allocatedSet;
            Check(deviceApi.vkAllocateDescriptorSets(device, &allocateInfo, &allocatedSet));
            descriptorSet = allocatedSet;

            foreach (var write in writes)
            {
                var bufferInfo = write.BufferInfo;
                var imageInfo = write.ImageInfo;
                var texelView = write.TexelView;
                var descriptorWrite = new VkWriteDescriptorSet
                {
                    sType = VkStructureType.WriteDescriptorSet,
                    dstSet = descriptorSet,
                    dstBinding = write.Binding,
                    descriptorCount = 1,
                    descriptorType = write.Type,
                    pBufferInfo = &bufferInfo,
                    pImageInfo = &imageInfo,
                    pTexelBufferView = &texelView,
                };
                deviceApi.vkUpdateDescriptorSets(device, 1, &descriptorWrite, 0, null);
            }
        }
    }

    private static VkDescriptorType GetDescriptorType(EffectResourceBindingDescription binding) => binding.Class switch
    {
        EffectParameterClass.ConstantBuffer => VkDescriptorType.UniformBuffer,
        EffectParameterClass.Sampler => VkDescriptorType.Sampler,
        EffectParameterClass.ShaderResourceView => binding.Type switch
        {
            EffectParameterType.Buffer => VkDescriptorType.UniformTexelBuffer,
            EffectParameterType.StructuredBuffer or EffectParameterType.ByteAddressBuffer => VkDescriptorType.StorageBuffer,
            _ => VkDescriptorType.SampledImage,
        },
        EffectParameterClass.UnorderedAccessView => binding.Type switch
        {
            EffectParameterType.RWBuffer => VkDescriptorType.StorageTexelBuffer,
            EffectParameterType.RWStructuredBuffer or EffectParameterType.RWByteAddressBuffer => VkDescriptorType.StorageBuffer,
            _ => VkDescriptorType.StorageImage,
        },
        _ => throw new NotSupportedException($"Unsupported resource class {binding.Class}"),
    };

    public override void RenderFrame(Span<byte> result)
    {
        SetupDescriptors();

        // Quad geometry, same as the D3D11 renderer
        ReadOnlySpan<float> vertices =
        [
            1f,  1f, 0f,  1.0f, 1.0f,
            1f, -1f, 0f,  1.0f, 0.0f,
            -1f,-1f, 0f,  0.0f, 0.0f,
            -1f, 1f, 1f,  0.0f, 1.0f,
        ];
        ReadOnlySpan<uint> indices = [0, 1, 3, 1, 2, 3];
        var (vertexBuffer, _) = CreateHostBuffer((ulong)(vertices.Length * sizeof(float)), VkBufferUsageFlags.VertexBuffer, MemoryMarshal.AsBytes(vertices));
        var (indexBuffer, _) = CreateHostBuffer((ulong)(indices.Length * sizeof(uint)), VkBufferUsageFlags.IndexBuffer, MemoryMarshal.AsBytes(indices));

        // Vertex shader: from the test's bytecode when present, else the quad passthrough
        var vertexProbe = probe;
        var vertexModule = shaderModule;
        var vertexEntryPoint = probe.FindEntryPoint(SpirvProbe.ExecutionModelVertex);
        if (vertexEntryPoint == null)
        {
            vertexProbe = fallbackVertexProbe ?? throw new InvalidOperationException("No vertex entry point and no fallback vertex shader");
            vertexModule = fallbackVertexModule;
            vertexEntryPoint = vertexProbe.FindEntryPoint(SpirvProbe.ExecutionModelVertex);
        }
        var fragmentEntryPoint = probe.FindEntryPoint(SpirvProbe.ExecutionModelFragment)
            ?? throw new InvalidOperationException("No fragment entry point");

        // Vertex attributes: POSITION/TEXCOORD from the quad buffer, other semantics from
        // per-instance single-element buffers built from "stream.SEMANTIC" test parameters
        var bindingDescriptions = new List<VkVertexInputBindingDescription>
        {
            new() { binding = 0, stride = 5 * sizeof(float), inputRate = VkVertexInputRate.Vertex },
        };
        var attributeDescriptions = new List<VkVertexInputAttributeDescription>();
        var vertexBufferHandles = new List<VkBuffer> { vertexBuffer };
        foreach (var (location, semantic) in vertexProbe.GetStageInputs(vertexEntryPoint))
        {
            if (semantic.StartsWith("POSITION", StringComparison.OrdinalIgnoreCase))
            {
                attributeDescriptions.Add(new VkVertexInputAttributeDescription { location = location, binding = 0, format = VkFormat.R32G32B32Sfloat, offset = 0 });
            }
            else if (semantic.StartsWith("TEXCOORD", StringComparison.OrdinalIgnoreCase))
            {
                attributeDescriptions.Add(new VkVertexInputAttributeDescription { location = location, binding = 0, format = VkFormat.R32G32Sfloat, offset = 3 * sizeof(float) });
            }
            else
            {
                var parameterValue = Parameters[$"stream.{semantic}"];
                var floatValues = parameterValue.TrimStart('(').TrimEnd(')').Split(' ', StringSplitOptions.TrimEntries).Select(x => float.Parse(x, NumberStyles.Float, CultureInfo.InvariantCulture)).ToArray();
                var streamData = new float[4];
                floatValues.CopyTo(streamData, 0);
                var (streamBuffer, _) = CreateHostBuffer(4 * sizeof(float), VkBufferUsageFlags.VertexBuffer, MemoryMarshal.AsBytes<float>(streamData));
                var streamBinding = (uint)bindingDescriptions.Count;
                bindingDescriptions.Add(new VkVertexInputBindingDescription { binding = streamBinding, stride = 4 * sizeof(float), inputRate = VkVertexInputRate.Instance });
                attributeDescriptions.Add(new VkVertexInputAttributeDescription { location = location, binding = streamBinding, format = VkFormat.R32G32B32A32Sfloat, offset = 0 });
                vertexBufferHandles.Add(streamBuffer);
            }
        }

        // Offscreen color target + readback buffer
        var (colorImage, colorView) = CreateImage2D(width, height, VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.TransferSrc);
        var (readbackBuffer, readbackMemory) = CreateHostBuffer(width * height * 4, VkBufferUsageFlags.TransferDst);

        // Render pass: clear to black, end in transfer-src for the readback copy
        var colorAttachment = new VkAttachmentDescription
        {
            format = VkFormat.R8G8B8A8Unorm,
            samples = VkSampleCountFlags.Count1,
            loadOp = VkAttachmentLoadOp.Clear,
            storeOp = VkAttachmentStoreOp.Store,
            stencilLoadOp = VkAttachmentLoadOp.DontCare,
            stencilStoreOp = VkAttachmentStoreOp.DontCare,
            initialLayout = VkImageLayout.Undefined,
            finalLayout = VkImageLayout.TransferSrcOptimal,
        };
        var colorReference = new VkAttachmentReference { attachment = 0, layout = VkImageLayout.ColorAttachmentOptimal };
        var subpass = new VkSubpassDescription
        {
            pipelineBindPoint = VkPipelineBindPoint.Graphics,
            colorAttachmentCount = 1,
            pColorAttachments = &colorReference,
        };
        var renderPassCreateInfo = new VkRenderPassCreateInfo
        {
            sType = VkStructureType.RenderPassCreateInfo,
            attachmentCount = 1,
            pAttachments = &colorAttachment,
            subpassCount = 1,
            pSubpasses = &subpass,
        };
        Check(deviceApi.vkCreateRenderPass(device, &renderPassCreateInfo, null, out var renderPass));
        renderPasses.Add(renderPass);

        var colorViewLocal = colorView;
        var framebufferCreateInfo = new VkFramebufferCreateInfo
        {
            sType = VkStructureType.FramebufferCreateInfo,
            renderPass = renderPass,
            attachmentCount = 1,
            pAttachments = &colorViewLocal,
            width = width,
            height = height,
            layers = 1,
        };
        Check(deviceApi.vkCreateFramebuffer(device, &framebufferCreateInfo, null, out var framebuffer));
        framebuffers.Add(framebuffer);

        // Pipeline
        var vertexEntryPointName = Encoding.UTF8.GetBytes(vertexEntryPoint.Name + "\0");
        var fragmentEntryPointName = Encoding.UTF8.GetBytes(fragmentEntryPoint.Name + "\0");
        var bindingDescriptionsArray = bindingDescriptions.ToArray();
        var attributeDescriptionsArray = attributeDescriptions.ToArray();
        fixed (byte* vertexEntryPointNamePtr = vertexEntryPointName)
        fixed (byte* fragmentEntryPointNamePtr = fragmentEntryPointName)
        fixed (VkVertexInputBindingDescription* bindingDescriptionsPtr = bindingDescriptionsArray)
        fixed (VkVertexInputAttributeDescription* attributeDescriptionsPtr = attributeDescriptionsArray)
        {
            var stages = stackalloc VkPipelineShaderStageCreateInfo[2];
            stages[0] = new VkPipelineShaderStageCreateInfo
            {
                sType = VkStructureType.PipelineShaderStageCreateInfo,
                stage = VkShaderStageFlags.Vertex,
                module = vertexModule,
                pName = vertexEntryPointNamePtr,
            };
            stages[1] = new VkPipelineShaderStageCreateInfo
            {
                sType = VkStructureType.PipelineShaderStageCreateInfo,
                stage = VkShaderStageFlags.Fragment,
                module = shaderModule,
                pName = fragmentEntryPointNamePtr,
            };

            var vertexInputState = new VkPipelineVertexInputStateCreateInfo
            {
                sType = VkStructureType.PipelineVertexInputStateCreateInfo,
                vertexBindingDescriptionCount = (uint)bindingDescriptionsArray.Length,
                pVertexBindingDescriptions = bindingDescriptionsPtr,
                vertexAttributeDescriptionCount = (uint)attributeDescriptionsArray.Length,
                pVertexAttributeDescriptions = attributeDescriptionsPtr,
            };
            var inputAssemblyState = new VkPipelineInputAssemblyStateCreateInfo
            {
                sType = VkStructureType.PipelineInputAssemblyStateCreateInfo,
                topology = VkPrimitiveTopology.TriangleList,
            };
            var viewport = new VkViewport(0, 0, width, height, 0, 1);
            var scissor = new VkRect2D(0, 0, width, height);
            var viewportState = new VkPipelineViewportStateCreateInfo
            {
                sType = VkStructureType.PipelineViewportStateCreateInfo,
                viewportCount = 1,
                pViewports = &viewport,
                scissorCount = 1,
                pScissors = &scissor,
            };
            var rasterizationState = new VkPipelineRasterizationStateCreateInfo
            {
                sType = VkStructureType.PipelineRasterizationStateCreateInfo,
                polygonMode = VkPolygonMode.Fill,
                cullMode = VkCullModeFlags.None,
                frontFace = VkFrontFace.Clockwise,
                lineWidth = 1,
            };
            var multisampleState = new VkPipelineMultisampleStateCreateInfo
            {
                sType = VkStructureType.PipelineMultisampleStateCreateInfo,
                rasterizationSamples = VkSampleCountFlags.Count1,
            };
            var blendAttachment = new VkPipelineColorBlendAttachmentState
            {
                colorWriteMask = VkColorComponentFlags.All,
            };
            var colorBlendState = new VkPipelineColorBlendStateCreateInfo
            {
                sType = VkStructureType.PipelineColorBlendStateCreateInfo,
                attachmentCount = 1,
                pAttachments = &blendAttachment,
            };
            var pipelineCreateInfo = new VkGraphicsPipelineCreateInfo
            {
                sType = VkStructureType.GraphicsPipelineCreateInfo,
                stageCount = 2,
                pStages = stages,
                pVertexInputState = &vertexInputState,
                pInputAssemblyState = &inputAssemblyState,
                pViewportState = &viewportState,
                pRasterizationState = &rasterizationState,
                pMultisampleState = &multisampleState,
                pColorBlendState = &colorBlendState,
                layout = pipelineLayout,
                renderPass = renderPass,
                subpass = 0,
            };
            VkPipeline createdPipeline;
            Check(deviceApi.vkCreateGraphicsPipelines(device, VkPipelineCache.Null, 1, &pipelineCreateInfo, null, &createdPipeline));
            var pipeline = createdPipeline;
            pipelines.Add(pipeline);

            OneTimeCommands(commandBuffer =>
            {
                var clearValue = new VkClearValue { color = new VkClearColorValue(0, 0, 0, 1) };
                var renderPassBeginInfo = new VkRenderPassBeginInfo
                {
                    sType = VkStructureType.RenderPassBeginInfo,
                    renderPass = renderPass,
                    framebuffer = framebuffer,
                    renderArea = new VkRect2D(0, 0, width, height),
                    clearValueCount = 1,
                    pClearValues = &clearValue,
                };
                deviceApi.vkCmdBeginRenderPass(commandBuffer, &renderPassBeginInfo, VkSubpassContents.Inline);
                deviceApi.vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.Graphics, pipeline);
                var descriptorSetLocal = descriptorSet;
                if (descriptorSetLocal != VkDescriptorSet.Null)
                    deviceApi.vkCmdBindDescriptorSets(commandBuffer, VkPipelineBindPoint.Graphics, pipelineLayout, 0, 1, &descriptorSetLocal, 0, null);
                var vertexBuffersArray = vertexBufferHandles.ToArray();
                var offsets = new ulong[vertexBuffersArray.Length];
                fixed (VkBuffer* vertexBuffersPtr = vertexBuffersArray)
                fixed (ulong* offsetsPtr = offsets)
                    deviceApi.vkCmdBindVertexBuffers(commandBuffer, 0, (uint)vertexBuffersArray.Length, vertexBuffersPtr, offsetsPtr);
                deviceApi.vkCmdBindIndexBuffer(commandBuffer, indexBuffer, 0, VkIndexType.Uint32);
                deviceApi.vkCmdDrawIndexed(commandBuffer, 6, 1, 0, 0, 0);
                deviceApi.vkCmdEndRenderPass(commandBuffer);

                var copyRegion = new VkBufferImageCopy
                {
                    imageSubresource = new VkImageSubresourceLayers(VkImageAspectFlags.Color, 0, 0, 1),
                    imageExtent = new VkExtent3D(width, height, 1),
                };
                deviceApi.vkCmdCopyImageToBuffer(commandBuffer, colorImage, VkImageLayout.TransferSrcOptimal, readbackBuffer, 1, &copyRegion);
            });
        }

        void* mapped;
        Check(deviceApi.vkMapMemory(device, readbackMemory, 0, width * height * 4, 0, &mapped));
        new ReadOnlySpan<byte>(mapped, (int)(width * height * 4)).CopyTo(result);
        deviceApi.vkUnmapMemory(device, readbackMemory);
    }

    public override void Compute()
    {
        SetupDescriptors();

        var computeEntryPoint = probe.FindEntryPoint(SpirvProbe.ExecutionModelGLCompute)
            ?? throw new InvalidOperationException("No compute entry point");

        var computeEntryPointName = Encoding.UTF8.GetBytes(computeEntryPoint.Name + "\0");
        VkPipeline createdPipeline;
        fixed (byte* computeEntryPointNamePtr = computeEntryPointName)
        {
            var pipelineCreateInfo = new VkComputePipelineCreateInfo
            {
                sType = VkStructureType.ComputePipelineCreateInfo,
                stage = new VkPipelineShaderStageCreateInfo
                {
                    sType = VkStructureType.PipelineShaderStageCreateInfo,
                    stage = VkShaderStageFlags.Compute,
                    module = shaderModule,
                    pName = computeEntryPointNamePtr,
                },
                layout = pipelineLayout,
            };
            Check(deviceApi.vkCreateComputePipelines(device, VkPipelineCache.Null, 1, &pipelineCreateInfo, null, &createdPipeline));
        }
        var pipeline = createdPipeline;
        pipelines.Add(pipeline);

        OneTimeCommands(commandBuffer =>
        {
            deviceApi.vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.Compute, pipeline);
            var descriptorSetLocal = descriptorSet;
            if (descriptorSetLocal != VkDescriptorSet.Null)
                deviceApi.vkCmdBindDescriptorSets(commandBuffer, VkPipelineBindPoint.Compute, pipelineLayout, 0, 1, &descriptorSetLocal, 0, null);
            deviceApi.vkCmdDispatch(commandBuffer, 32, 32, 1);
        });
    }

    public override void PresentAndFinish()
    {
        // Destroy only this test's resources; the device/instance are shared for the whole run
        deviceApi.vkDeviceWaitIdle(device);

        foreach (var pipeline in pipelines) deviceApi.vkDestroyPipeline(device, pipeline, null);
        foreach (var framebuffer in framebuffers) deviceApi.vkDestroyFramebuffer(device, framebuffer, null);
        foreach (var renderPass in renderPasses) deviceApi.vkDestroyRenderPass(device, renderPass, null);
        if (pipelineLayout != VkPipelineLayout.Null) deviceApi.vkDestroyPipelineLayout(device, pipelineLayout, null);
        if (descriptorPool != VkDescriptorPool.Null) deviceApi.vkDestroyDescriptorPool(device, descriptorPool, null);
        if (descriptorSetLayout != VkDescriptorSetLayout.Null) deviceApi.vkDestroyDescriptorSetLayout(device, descriptorSetLayout, null);
        foreach (var sampler in samplers) deviceApi.vkDestroySampler(device, sampler, null);
        foreach (var bufferView in bufferViews) deviceApi.vkDestroyBufferView(device, bufferView, null);
        foreach (var imageView in imageViews) deviceApi.vkDestroyImageView(device, imageView, null);
        foreach (var image in images) deviceApi.vkDestroyImage(device, image, null);
        foreach (var buffer in buffers) deviceApi.vkDestroyBuffer(device, buffer, null);
        foreach (var memory in memories) deviceApi.vkFreeMemory(device, memory, null);
        if (fallbackVertexModule != VkShaderModule.Null) deviceApi.vkDestroyShaderModule(device, fallbackVertexModule, null);
        if (shaderModule != VkShaderModule.Null) deviceApi.vkDestroyShaderModule(device, shaderModule, null);

        // The same renderer runs once per test header: reset so the next SetupTest starts clean
        pipelines.Clear();
        framebuffers.Clear();
        renderPasses.Clear();
        samplers.Clear();
        bufferViews.Clear();
        imageViews.Clear();
        images.Clear();
        buffers.Clear();
        memories.Clear();
        pipelineLayout = default;
        descriptorPool = default;
        descriptorSetLayout = default;
        descriptorSet = default;
        shaderModule = default;
        fallbackVertexModule = default;
    }
}
