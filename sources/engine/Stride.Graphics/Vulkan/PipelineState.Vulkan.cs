// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using System.Collections.Generic;
using System.Linq;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
using Stride.Core;
using Stride.Core.Collections;
using Stride.Core.Serialization;
using Stride.Shaders;
using Encoding = System.Text.Encoding;

namespace Stride.Graphics
{
    public partial class PipelineState
    {
        internal VkDescriptorSetLayout NativeDescriptorSetLayout;
        internal uint[] DescriptorTypeCounts;
        internal DescriptorSetLayout DescriptorSetLayout;

        internal VkPipelineLayout NativeLayout;
        internal VkPipeline NativePipeline;
        internal VkRenderPass NativeRenderPass;
        internal int[] ResourceGroupMapping;
        internal int ResourceGroupCount;
        internal PipelineStateDescription Description;

        // State exposed by the CommandList
        private static readonly VkDynamicState[] dynamicStates =
        {
            VkDynamicState.Viewport,
            VkDynamicState.Scissor,
            VkDynamicState.BlendConstants,
            VkDynamicState.StencilReference,
        };

        // GLSL converter always outputs entry point main()
        private static readonly byte[] defaultEntryPoint = Encoding.UTF8.GetBytes("main\0");

        internal PipelineState(GraphicsDevice graphicsDevice, PipelineStateDescription pipelineStateDescription) : base(graphicsDevice)
        {
            Description = pipelineStateDescription.Clone();
            Recreate();
        }

        private unsafe void Recreate()
        {
            if (Description.RootSignature == null)
                return;

            CreateRenderPass(Description);

            CreatePipelineLayout(Description);

            // Create shader stages
            Dictionary<int, string> inputAttributeNames;

            // Note: important to pin this so that stages[x].Name is valid during this whole function
            void* defaultEntryPointData = Core.Interop.Fixed(defaultEntryPoint);
            var stages = CreateShaderStages(Description, out inputAttributeNames);

            var inputAttributes = new VkVertexInputAttributeDescription[Description.InputElements.Length];
            int inputAttributeCount = 0;
            var inputBindings = new VkVertexInputBindingDescription[inputAttributes.Length];
            int inputBindingCount = 0;

            for (int inputElementIndex = 0; inputElementIndex < inputAttributes.Length; inputElementIndex++)
            {
                var inputElement = Description.InputElements[inputElementIndex];
                var slotIndex = inputElement.InputSlot;

                if (inputElement.InstanceDataStepRate > 1)
                {
                    throw new NotImplementedException();
                }

                VkFormat format;
                int size;
                bool isCompressed;
                VulkanConvertExtensions.ConvertPixelFormat(inputElement.Format, out format, out size, out isCompressed);

                var location = inputAttributeNames.FirstOrDefault(x => x.Value == inputElement.SemanticName && inputElement.SemanticIndex == 0 || x.Value == inputElement.SemanticName + inputElement.SemanticIndex);
                if (location.Value != null)
                {
                    inputAttributes[inputAttributeCount++] = new VkVertexInputAttributeDescription
                    {
                        format = format,
                        offset = (uint)inputElement.AlignedByteOffset,
                        binding = (uint)inputElement.InputSlot,
                        location = (uint)location.Key
                    };
                }

                inputBindings[slotIndex].binding = (uint)slotIndex;
                inputBindings[slotIndex].inputRate = inputElement.InputSlotClass == InputClassification.Vertex ? VkVertexInputRate.Vertex : VkVertexInputRate.Instance;

                // TODO VULKAN: This is currently an argument to Draw() overloads.
                if (inputBindings[slotIndex].stride < inputElement.AlignedByteOffset + size)
                    inputBindings[slotIndex].stride = (uint)(inputElement.AlignedByteOffset + size);

                if (inputElement.InputSlot >= inputBindingCount)
                    inputBindingCount = inputElement.InputSlot + 1;
            }

            var inputAssemblyState = new VkPipelineInputAssemblyStateCreateInfo
            {
                sType = VkStructureType.PipelineInputAssemblyStateCreateInfo,
                topology = VulkanConvertExtensions.ConvertPrimitiveType(Description.PrimitiveType),
                primitiveRestartEnable = VulkanConvertExtensions.ConvertPrimitiveRestart(Description.PrimitiveType),
            };

            // TODO VULKAN: Tessellation and multisampling
            var multisampleState = new VkPipelineMultisampleStateCreateInfo
            {
                sType = VkStructureType.PipelineMultisampleStateCreateInfo,
                rasterizationSamples = VkSampleCountFlags.Count1,
            };

            var tessellationState = new VkPipelineTessellationStateCreateInfo
            {
                sType = VkStructureType.PipelineTessellationStateCreateInfo,
            };

            var rasterizationState = CreateRasterizationState(Description.RasterizerState);

            var depthStencilState = CreateDepthStencilState(Description);

            var description = Description.BlendState;

            var renderTargetCount = Description.Output.RenderTargetCount;
            var colorBlendAttachments = new VkPipelineColorBlendAttachmentState[renderTargetCount];

            var renderTargetBlendState = &description.RenderTarget0;
            for (int i = 0; i < renderTargetCount; i++)
            {
                colorBlendAttachments[i] = new VkPipelineColorBlendAttachmentState
                {
                    blendEnable = renderTargetBlendState->BlendEnable,
                    alphaBlendOp = VulkanConvertExtensions.ConvertBlendFunction(renderTargetBlendState->AlphaBlendFunction),
                    colorBlendOp = VulkanConvertExtensions.ConvertBlendFunction(renderTargetBlendState->ColorBlendFunction),
                    dstAlphaBlendFactor = VulkanConvertExtensions.ConvertBlend(renderTargetBlendState->AlphaDestinationBlend),
                    dstColorBlendFactor = VulkanConvertExtensions.ConvertBlend(renderTargetBlendState->ColorDestinationBlend),
                    srcAlphaBlendFactor = VulkanConvertExtensions.ConvertBlend(renderTargetBlendState->AlphaSourceBlend),
                    srcColorBlendFactor = VulkanConvertExtensions.ConvertBlend(renderTargetBlendState->ColorSourceBlend),
                    colorWriteMask = VulkanConvertExtensions.ConvertColorWriteChannels(renderTargetBlendState->ColorWriteChannels),
                };

                if (description.IndependentBlendEnable)
                    renderTargetBlendState++;
            }

            var viewportState = new VkPipelineViewportStateCreateInfo
            {
                sType = VkStructureType.PipelineViewportStateCreateInfo,
                scissorCount = 1,
                viewportCount = 1,
            };

            fixed (VkDynamicState* dynamicStatesPointer = &dynamicStates[0])
            {
                var vertexInputState = new VkPipelineVertexInputStateCreateInfo
                {
                    sType = VkStructureType.PipelineVertexInputStateCreateInfo,
                    vertexAttributeDescriptionCount = (uint)inputAttributeCount,
                    pVertexAttributeDescriptions = inputAttributes.Length > 0 ? (VkVertexInputAttributeDescription*)Core.Interop.Fixed(inputAttributes) : null,
                    vertexBindingDescriptionCount = (uint)inputBindingCount,
                    pVertexBindingDescriptions = inputBindings.Length > 0 ? (VkVertexInputBindingDescription*)Core.Interop.Fixed(inputBindings) : null,
                };

                var colorBlendState = new VkPipelineColorBlendStateCreateInfo
                {
                    sType = VkStructureType.PipelineColorBlendStateCreateInfo,
                    attachmentCount = (uint)renderTargetCount,
                    pAttachments = colorBlendAttachments.Length > 0 ? (VkPipelineColorBlendAttachmentState*)Core.Interop.Fixed(colorBlendAttachments) : null,
                };

                var dynamicState = new VkPipelineDynamicStateCreateInfo
                {
                    sType = VkStructureType.PipelineDynamicStateCreateInfo,
                    dynamicStateCount = (uint)dynamicStates.Length,
                    pDynamicStates = dynamicStatesPointer,
                };

                var createInfo = new VkGraphicsPipelineCreateInfo
                {
                    sType = VkStructureType.GraphicsPipelineCreateInfo,
                    layout = NativeLayout,
                    stageCount = (uint)stages.Length,
                    pStages = stages.Length > 0 ? (VkPipelineShaderStageCreateInfo*)Core.Interop.Fixed(stages) : null,
                    //tessellationState = &tessellationState,
                    pVertexInputState = &vertexInputState,
                    pInputAssemblyState = &inputAssemblyState,
                    pRasterizationState = &rasterizationState,
                    pMultisampleState = &multisampleState,
                    pDepthStencilState = &depthStencilState,
                    pColorBlendState = &colorBlendState,
                    pDynamicState = &dynamicState,
                    pViewportState = &viewportState,
                    renderPass = NativeRenderPass,
                    subpass = 0,
                };
                fixed (VkPipeline* nativePipelinePtr = &NativePipeline)
                    vkCreateGraphicsPipelines(GraphicsDevice.NativeDevice, VkPipelineCache.Null, 1, &createInfo, null, nativePipelinePtr);
            }

            // Cleanup shader modules
            foreach (var stage in stages)
            {
                vkDestroyShaderModule(GraphicsDevice.NativeDevice, stage.module, null);
            }
        }

        /// <inheritdoc/>
        protected internal override bool OnRecreate()
        {
            Recreate();
            return true;
        }

        private unsafe void CreateRenderPass(PipelineStateDescription pipelineStateDescription)
        {
            bool hasDepthStencilAttachment = pipelineStateDescription.Output.DepthStencilFormat != PixelFormat.None;

            var renderTargetCount = pipelineStateDescription.Output.RenderTargetCount;

            var attachmentCount = renderTargetCount;
            if (hasDepthStencilAttachment)
                attachmentCount++;

            var attachments = new VkAttachmentDescription[attachmentCount];
            var colorAttachmentReferences = new VkAttachmentReference[renderTargetCount];

            fixed (PixelFormat* renderTargetFormat = &pipelineStateDescription.Output.RenderTargetFormat0)
            fixed (BlendStateRenderTargetDescription* blendDescription = &pipelineStateDescription.BlendState.RenderTarget0)
            {
                for (int i = 0; i < renderTargetCount; i++)
                {
                    var currentBlendDesc = pipelineStateDescription.BlendState.IndependentBlendEnable ? (blendDescription + i) : blendDescription;

                    attachments[i] = new VkAttachmentDescription
                    {
                        format = VulkanConvertExtensions.ConvertPixelFormat(*(renderTargetFormat + i)),
                        samples = VkSampleCountFlags.Count1,
                        loadOp = currentBlendDesc->BlendEnable ? VkAttachmentLoadOp.Load : VkAttachmentLoadOp.DontCare, // TODO VULKAN: Only if any destination blend?
                        storeOp = VkAttachmentStoreOp.Store,
                        stencilLoadOp = VkAttachmentLoadOp.DontCare,
                        stencilStoreOp = VkAttachmentStoreOp.DontCare,
                        initialLayout = VkImageLayout.ColorAttachmentOptimal,
                        finalLayout = VkImageLayout.ColorAttachmentOptimal,
                    };

                    colorAttachmentReferences[i] = new VkAttachmentReference
                    {
                        attachment = (uint)i,
                        layout = VkImageLayout.ColorAttachmentOptimal,
                    };
                }
            }

            if (hasDepthStencilAttachment)
            {
                attachments[attachmentCount - 1] = new VkAttachmentDescription
                {
                    format = Texture.GetFallbackDepthStencilFormat(GraphicsDevice, VulkanConvertExtensions.ConvertPixelFormat(pipelineStateDescription.Output.DepthStencilFormat)),
                    samples = VkSampleCountFlags.Count1,
                    loadOp = VkAttachmentLoadOp.Load, // TODO VULKAN: Only if depth read enabled?
                    storeOp = VkAttachmentStoreOp.Store, // TODO VULKAN: Only if depth write enabled?
                    stencilLoadOp = VkAttachmentLoadOp.DontCare, // TODO VULKAN: Handle stencil
                    stencilStoreOp = VkAttachmentStoreOp.DontCare,
                    initialLayout = VkImageLayout.DepthStencilAttachmentOptimal,
                    finalLayout = VkImageLayout.DepthStencilAttachmentOptimal,
                };
            }

            var depthAttachmentReference = new VkAttachmentReference
            {
                attachment = (uint)attachments.Length - 1,
                layout = VkImageLayout.DepthStencilAttachmentOptimal,
            };

            var subpass = new VkSubpassDescription
            {
                pipelineBindPoint = VkPipelineBindPoint.Graphics,
                colorAttachmentCount = (uint)renderTargetCount,
                pColorAttachments = colorAttachmentReferences.Length > 0 ? (VkAttachmentReference*)Core.Interop.Fixed(colorAttachmentReferences) : null,
                pDepthStencilAttachment = hasDepthStencilAttachment ? &depthAttachmentReference : null,
            };

            var renderPassCreateInfo = new VkRenderPassCreateInfo
            {
                sType = VkStructureType.RenderPassCreateInfo,
                attachmentCount = (uint)attachmentCount,
                pAttachments = attachments.Length > 0 ? (VkAttachmentDescription*)Core.Interop.Fixed(attachments) : null,
                subpassCount = 1,
                pSubpasses = &subpass,
            };
            vkCreateRenderPass(GraphicsDevice.NativeDevice, &renderPassCreateInfo, null, out NativeRenderPass);
        }

        /// <inheritdoc/>
        protected internal override unsafe void OnDestroyed()
        {
            if (NativePipeline != VkPipeline.Null)
            {
                vkDestroyRenderPass(GraphicsDevice.NativeDevice, NativeRenderPass, null);
                vkDestroyPipeline(GraphicsDevice.NativeDevice, NativePipeline, null);
                vkDestroyPipelineLayout(GraphicsDevice.NativeDevice, NativeLayout, null);

                vkDestroyDescriptorSetLayout(GraphicsDevice.NativeDevice, NativeDescriptorSetLayout, null);
            }

            base.OnDestroyed();
        }

        internal struct DescriptorSetInfo
        {
            public int SourceSet;
            public int SourceBinding;
            public int DestinationBinding;
            public VkDescriptorType DescriptorType;
            // Used for buffer/texture (to know what type to match)
            public bool ResourceElementIsInteger;
        }

        internal List<DescriptorSetInfo> DescriptorBindingMapping;

        private unsafe void CreatePipelineLayout(PipelineStateDescription pipelineStateDescription)
        {
            // Remap descriptor set indices to those in the shader. This ordering generated by the ShaderCompiler
            var resourceGroups = pipelineStateDescription.EffectBytecode.Reflection.ResourceBindings.Select(x => x.ResourceGroup ?? "Globals").Distinct().ToList();
            ResourceGroupCount = resourceGroups.Count;

            var layouts = pipelineStateDescription.RootSignature.EffectDescriptorSetReflection.Layouts;
            
            // Get binding indices used by the shader
            var destinationBindings = pipelineStateDescription.EffectBytecode.Stages
                .SelectMany(x => BinarySerialization.Read<ShaderInputBytecode>(x.Data).ResourceBindings)
                .GroupBy(x => x.Key, x => x.Value)
                .ToDictionary(x => x.Key, x => x.First());

            var maxBindingIndex = destinationBindings.Max(x => x.Value);
            var destinationEntries = new DescriptorSetLayoutBuilder.Entry[maxBindingIndex + 1];

            DescriptorBindingMapping = new List<DescriptorSetInfo>();

            for (int i = 0; i < resourceGroups.Count; i++)
            {
                var resourceGroupName = resourceGroups[i] == "Globals" ? pipelineStateDescription.RootSignature.EffectDescriptorSetReflection.DefaultSetSlot : resourceGroups[i];
                var layoutIndex = resourceGroups[i] == null ? 0 : layouts.FindIndex(x => x.Name == resourceGroupName);

                // Check if the resource group is used by the shader
                if (layoutIndex == -1)
                    continue;

                var sourceEntries = layouts[layoutIndex].Layout.Entries;

                for (int sourceBinding = 0; sourceBinding < sourceEntries.Count; sourceBinding++)
                {
                    var sourceEntry = sourceEntries[sourceBinding];

                    int destinationBinding;
                    if (destinationBindings.TryGetValue(sourceEntry.Key.Name, out destinationBinding))
                    {
                        destinationEntries[destinationBinding] = sourceEntry;

                        // No need to umpdate immutable samplers
                        if (sourceEntry.Class == EffectParameterClass.Sampler && sourceEntry.ImmutableSampler != null)
                        {
                            continue;
                        }

                        DescriptorBindingMapping.Add(new DescriptorSetInfo
                        {
                            SourceSet = layoutIndex,
                            SourceBinding = sourceBinding,
                            DestinationBinding = destinationBinding,
                            DescriptorType = VulkanConvertExtensions.ConvertDescriptorType(sourceEntry.Class, sourceEntry.Type),
                            ResourceElementIsInteger = sourceEntry.ElementType != EffectParameterType.Float && sourceEntry.ElementType != EffectParameterType.Double,
                        });
                    }
                }
            }

            // Create default sampler, used by texture and buffer loads
            destinationEntries[0] = new DescriptorSetLayoutBuilder.Entry
            {
                Class = EffectParameterClass.Sampler,
                Type = EffectParameterType.Sampler,
                ImmutableSampler = GraphicsDevice.SamplerStates.PointWrap,
                ArraySize = 1,
            };

            // Create descriptor set layout
            NativeDescriptorSetLayout = DescriptorSetLayout.CreateNativeDescriptorSetLayout(GraphicsDevice, destinationEntries, out DescriptorTypeCounts);

            // Create pipeline layout
            var nativeDescriptorSetLayout = NativeDescriptorSetLayout;
            var pipelineLayoutCreateInfo = new VkPipelineLayoutCreateInfo
            {
                sType = VkStructureType.PipelineLayoutCreateInfo,
                setLayoutCount = 1,
                pSetLayouts = &nativeDescriptorSetLayout,
            };
            vkCreatePipelineLayout(GraphicsDevice.NativeDevice, &pipelineLayoutCreateInfo, null, out NativeLayout);
        }

        private unsafe VkPipelineShaderStageCreateInfo[] CreateShaderStages(PipelineStateDescription pipelineStateDescription, out Dictionary<int, string> inputAttributeNames)
        {
            var stages = pipelineStateDescription.EffectBytecode.Stages;
            var nativeStages = new VkPipelineShaderStageCreateInfo[stages.Length];

            inputAttributeNames = null;

            for (int i = 0; i < stages.Length; i++)
            {
                var shaderBytecode = BinarySerialization.Read<ShaderInputBytecode>(stages[i].Data);
                if (stages[i].Stage == ShaderStage.Vertex)
                    inputAttributeNames = shaderBytecode.InputAttributeNames;

                fixed (byte* entryPointPointer = &defaultEntryPoint[0])
                {
                    // Create stage
                    nativeStages[i] = new VkPipelineShaderStageCreateInfo
                    {
                        sType = VkStructureType.PipelineShaderStageCreateInfo,
                        stage = VulkanConvertExtensions.Convert(stages[i].Stage),
                        pName = entryPointPointer,
                    };
                    vkCreateShaderModule(GraphicsDevice.NativeDevice, shaderBytecode.Data, null, out nativeStages[i].module);
                }
            };

            return nativeStages;
        }

        private VkPipelineRasterizationStateCreateInfo CreateRasterizationState(RasterizerStateDescription description)
        {
            return new VkPipelineRasterizationStateCreateInfo
            {
                sType = VkStructureType.PipelineRasterizationStateCreateInfo,
                cullMode = VulkanConvertExtensions.ConvertCullMode(description.CullMode),
                frontFace = description.FrontFaceCounterClockwise ? VkFrontFace.CounterClockwise : VkFrontFace.Clockwise,
                polygonMode = VulkanConvertExtensions.ConvertFillMode(description.FillMode),
                depthBiasEnable = true, // TODO VULKAN
                depthBiasConstantFactor = description.DepthBias,
                depthBiasSlopeFactor = description.SlopeScaleDepthBias,
                depthBiasClamp = description.DepthBiasClamp,
                lineWidth = 1.0f,
                depthClampEnable = !description.DepthClipEnable,
                rasterizerDiscardEnable = false,
            };
        }

        private VkPipelineDepthStencilStateCreateInfo CreateDepthStencilState(PipelineStateDescription pipelineStateDescription)
        {
            var description = pipelineStateDescription.DepthStencilState;

            return new VkPipelineDepthStencilStateCreateInfo
            {
                sType = VkStructureType.PipelineDepthStencilStateCreateInfo,
                depthTestEnable = description.DepthBufferEnable,
                stencilTestEnable = description.StencilEnable,
                depthWriteEnable = description.DepthBufferWriteEnable,

                minDepthBounds = 0.0f,
                maxDepthBounds = 1.0f,
                depthCompareOp = VulkanConvertExtensions.ConvertComparisonFunction(description.DepthBufferFunction),
                front =
                {
                    compareOp = VulkanConvertExtensions.ConvertComparisonFunction(description.FrontFace.StencilFunction),
                    depthFailOp = VulkanConvertExtensions.ConvertStencilOperation(description.FrontFace.StencilDepthBufferFail),
                    failOp = VulkanConvertExtensions.ConvertStencilOperation(description.FrontFace.StencilFail),
                    passOp = VulkanConvertExtensions.ConvertStencilOperation(description.FrontFace.StencilPass),
                    compareMask = description.StencilMask,
                    writeMask = description.StencilWriteMask
                },
                back =
                {
                    compareOp = VulkanConvertExtensions.ConvertComparisonFunction(description.BackFace.StencilFunction),
                    depthFailOp = VulkanConvertExtensions.ConvertStencilOperation(description.BackFace.StencilDepthBufferFail),
                    failOp = VulkanConvertExtensions.ConvertStencilOperation(description.BackFace.StencilFail),
                    passOp = VulkanConvertExtensions.ConvertStencilOperation(description.BackFace.StencilPass),
                    compareMask = description.StencilMask,
                    writeMask = description.StencilWriteMask
                }
            };
        }
    }
}

#endif
