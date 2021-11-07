// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using System.Collections.Generic;
using System.Linq;
using Silk.NET.Vulkan;
using static Silk.NET.Vulkan.Vk;
using Stride.Core;
using Stride.Core.Collections;
using Stride.Core.Serialization;
using Stride.Shaders;
using Encoding = System.Text.Encoding;

namespace Stride.Graphics
{
    public partial class PipelineState
    {
        internal Silk.NET.Vulkan.DescriptorSetLayout NativeDescriptorSetLayout;
        internal uint[] DescriptorTypeCounts;
        internal DescriptorSetLayoutCreateInfo Layout;

        internal PipelineLayout NativeLayout;
        internal Pipeline NativePipeline;
        internal RenderPass NativeRenderPass;
        internal int[] ResourceGroupMapping;
        internal int ResourceGroupCount;
        internal PipelineStateDescription Description;

        // State exposed by the CommandList
        private static readonly DynamicState[] dynamicStates =
        {
            DynamicState.Viewport,
            DynamicState.Scissor,
            DynamicState.BlendConstants,
            DynamicState.StencilReference,
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

            var inputAttributes = new VertexInputAttributeDescription[Description.InputElements.Length];
            int inputAttributeCount = 0;
            var inputBindings = new VertexInputBindingDescription[inputAttributes.Length];
            int inputBindingCount = 0;

            for (int inputElementIndex = 0; inputElementIndex < inputAttributes.Length; inputElementIndex++)
            {
                var inputElement = Description.InputElements[inputElementIndex];
                var slotIndex = inputElement.InputSlot;

                if (inputElement.InstanceDataStepRate > 1)
                {
                    throw new NotImplementedException();
                }

                Format format;
                int size;
                bool isCompressed;
                VulkanConvertExtensions.ConvertPixelFormat(inputElement.Format, out format, out size, out isCompressed);

                var location = inputAttributeNames.FirstOrDefault(x => x.Value == inputElement.SemanticName && inputElement.SemanticIndex == 0 || x.Value == inputElement.SemanticName + inputElement.SemanticIndex);
                if (location.Value != null)
                {
                    inputAttributes[inputAttributeCount++] = new VertexInputAttributeDescription
                    {
                        Format = format,
                        Offset = (uint)inputElement.AlignedByteOffset,
                        Binding = (uint)inputElement.InputSlot,
                        Location = (uint)location.Key
                    };
                }

                inputBindings[slotIndex].Binding = (uint)slotIndex;
                inputBindings[slotIndex].InputRate = inputElement.InputSlotClass == InputClassification.Vertex ? VertexInputRate.Vertex : VertexInputRate.Instance;

                // TODO VULKAN: This is currently an argument to Draw() overloads.
                if (inputBindings[slotIndex].Stride < inputElement.AlignedByteOffset + size)
                    inputBindings[slotIndex].Stride = (uint)(inputElement.AlignedByteOffset + size);

                if (inputElement.InputSlot >= inputBindingCount)
                    inputBindingCount = inputElement.InputSlot + 1;
            }

            var inputAssemblyState = new PipelineInputAssemblyStateCreateInfo
            {
                SType = StructureType.PipelineInputAssemblyStateCreateInfo,
                Topology = VulkanConvertExtensions.ConvertPrimitiveType(Description.PrimitiveType),
                PrimitiveRestartEnable = VulkanConvertExtensions.ConvertPrimitiveRestart(Description.PrimitiveType),
            };

            // TODO VULKAN: Tessellation and multisampling
            var multisampleState = new PipelineMultisampleStateCreateInfo
            {
                SType = StructureType.PipelineMultisampleStateCreateInfo,
                RasterizationSamples = SampleCountFlags.SampleCount1Bit,
            };

            var tessellationState = new PipelineTessellationStateCreateInfo
            {
                SType = StructureType.PipelineTessellationStateCreateInfo,
            };

            var rasterizationState = CreateRasterizationState(Description.RasterizerState);

            var depthStencilState = CreateDepthStencilState(Description);

            var description = Description.BlendState;

            var renderTargetCount = Description.Output.RenderTargetCount;
            var colorBlendAttachments = new PipelineColorBlendAttachmentState[renderTargetCount];

            var renderTargetBlendState = &description.RenderTarget0;
            for (int i = 0; i < renderTargetCount; i++)
            {
                colorBlendAttachments[i] = new PipelineColorBlendAttachmentState
                {
                    BlendEnable = renderTargetBlendState->BlendEnable,
                    AlphaBlendOp = VulkanConvertExtensions.ConvertBlendFunction(renderTargetBlendState->AlphaBlendFunction),
                    ColorBlendOp = VulkanConvertExtensions.ConvertBlendFunction(renderTargetBlendState->ColorBlendFunction),
                    DstAlphaBlendFactor = VulkanConvertExtensions.ConvertBlend(renderTargetBlendState->AlphaDestinationBlend),
                    DstColorBlendFactor = VulkanConvertExtensions.ConvertBlend(renderTargetBlendState->ColorDestinationBlend),
                    SrcAlphaBlendFactor = VulkanConvertExtensions.ConvertBlend(renderTargetBlendState->AlphaSourceBlend),
                    SrcColorBlendFactor = VulkanConvertExtensions.ConvertBlend(renderTargetBlendState->ColorSourceBlend),
                    ColorWriteMask = VulkanConvertExtensions.ConvertColorWriteChannels(renderTargetBlendState->ColorWriteChannels),
                };

                if (description.IndependentBlendEnable)
                    renderTargetBlendState++;
            }

            var viewportState = new PipelineViewportStateCreateInfo
            {
                SType = StructureType.PipelineViewportStateCreateInfo,
                ScissorCount = 1,
                ViewportCount = 1,
            };

            fixed (DynamicState* dynamicStatesPointer = &dynamicStates[0])
            {
                var vertexInputState = new PipelineVertexInputStateCreateInfo
                {
                    SType = StructureType.PipelineVertexInputStateCreateInfo,
                    VertexAttributeDescriptionCount = (uint)inputAttributeCount,
                    PVertexAttributeDescriptions = inputAttributes.Length > 0 ? (VertexInputAttributeDescription*)Core.Interop.Fixed(inputAttributes) : null,
                    VertexBindingDescriptionCount = (uint)inputBindingCount,
                    PVertexBindingDescriptions = inputBindings.Length > 0 ? (VertexInputBindingDescription*)Core.Interop.Fixed(inputBindings) : null,
                };

                var colorBlendState = new PipelineColorBlendStateCreateInfo
                {
                    SType = StructureType.PipelineColorBlendStateCreateInfo,
                    AttachmentCount = (uint)renderTargetCount,
                    PAttachments = colorBlendAttachments.Length > 0 ? (PipelineColorBlendAttachmentState*)Core.Interop.Fixed(colorBlendAttachments) : null,
                };

                var dynamicState = new PipelineDynamicStateCreateInfo
                {
                    SType = StructureType.PipelineDynamicStateCreateInfo,
                    DynamicStateCount = (uint)dynamicStates.Length,
                    PDynamicStates = dynamicStatesPointer,
                };

                var createInfo = new GraphicsPipelineCreateInfo
                {
                    SType = StructureType.GraphicsPipelineCreateInfo,
                    Layout = NativeLayout,
                    StageCount = (uint)stages.Length,
                    PStages = stages.Length > 0 ? (PipelineShaderStageCreateInfo*)Core.Interop.Fixed(stages) : null,
                    //tessellationState = &tessellationState,
                    PVertexInputState = &vertexInputState,
                    PInputAssemblyState = &inputAssemblyState,
                    PRasterizationState = &rasterizationState,
                    PMultisampleState = &multisampleState,
                    PDepthStencilState = &depthStencilState,
                    PColorBlendState = &colorBlendState,
                    PDynamicState = &dynamicState,
                    PViewportState = &viewportState,
                    RenderPass = NativeRenderPass,
                    Subpass = 0,
                };
                fixed (Pipeline* nativePipelinePtr = &NativePipeline)
                    GetApi().CreateGraphicsPipelines(GraphicsDevice.NativeDevice, new PipelineCache(), 1, &createInfo, null, nativePipelinePtr);
            }

            // Cleanup shader modules
            foreach (var stage in stages)
            {
                GetApi().DestroyShaderModule(GraphicsDevice.NativeDevice, stage.Module, null);
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

            var attachments = new AttachmentDescription[attachmentCount];
            var colorAttachmentReferences = new AttachmentReference[renderTargetCount];

            fixed (PixelFormat* renderTargetFormat = &pipelineStateDescription.Output.RenderTargetFormat0)
            fixed (BlendStateRenderTargetDescription* blendDescription = &pipelineStateDescription.BlendState.RenderTarget0)
            {
                for (int i = 0; i < renderTargetCount; i++)
                {
                    var currentBlendDesc = pipelineStateDescription.BlendState.IndependentBlendEnable ? (blendDescription + i) : blendDescription;

                    attachments[i] = new AttachmentDescription
                    {
                        Format = VulkanConvertExtensions.ConvertPixelFormat(*(renderTargetFormat + i)),
                        Samples = SampleCountFlags.SampleCount1Bit,
                        LoadOp = currentBlendDesc->BlendEnable ? AttachmentLoadOp.Load : AttachmentLoadOp.DontCare, // TODO VULKAN: Only if any destination blend?
                        StoreOp = AttachmentStoreOp.Store,
                        StencilLoadOp = AttachmentLoadOp.DontCare,
                        StencilStoreOp = AttachmentStoreOp.DontCare,
                        InitialLayout = ImageLayout.ColorAttachmentOptimal,
                        FinalLayout = ImageLayout.ColorAttachmentOptimal,
                    };

                    colorAttachmentReferences[i] = new AttachmentReference
                    {
                        Attachment = (uint)i,
                        Layout = ImageLayout.ColorAttachmentOptimal,
                    };
                }
            }

            if (hasDepthStencilAttachment)
            {
                attachments[attachmentCount - 1] = new AttachmentDescription
                {
                    Format = Texture.GetFallbackDepthStencilFormat(GraphicsDevice, VulkanConvertExtensions.ConvertPixelFormat(pipelineStateDescription.Output.DepthStencilFormat)),
                    Samples = SampleCountFlags.SampleCount1Bit,
                    LoadOp = AttachmentLoadOp.Load, // TODO VULKAN: Only if depth read enabled?
                    StoreOp = AttachmentStoreOp.Store, // TODO VULKAN: Only if depth write enabled?
                    StencilLoadOp = AttachmentLoadOp.DontCare, // TODO VULKAN: Handle stencil
                    StencilStoreOp = AttachmentStoreOp.DontCare,
                    InitialLayout = ImageLayout.DepthStencilAttachmentOptimal,
                    FinalLayout = ImageLayout.DepthStencilAttachmentOptimal,
                };
            }

            var depthAttachmentReference = new AttachmentReference
            {
                Attachment = (uint)attachments.Length - 1,
                Layout = ImageLayout.DepthStencilAttachmentOptimal,
            };

            var subpass = new SubpassDescription
            {
                PipelineBindPoint = PipelineBindPoint.Graphics,
                ColorAttachmentCount = (uint)renderTargetCount,
                PColorAttachments = colorAttachmentReferences.Length > 0 ? (AttachmentReference*)Core.Interop.Fixed(colorAttachmentReferences) : null,
                PDepthStencilAttachment = hasDepthStencilAttachment ? &depthAttachmentReference : null,
            };

            var renderPassCreateInfo = new RenderPassCreateInfo
            {
                SType = StructureType.RenderPassCreateInfo,
                AttachmentCount = (uint)attachmentCount,
                PAttachments = attachments.Length > 0 ? (AttachmentDescription*)Core.Interop.Fixed(attachments) : null,
                SubpassCount = 1,
                PSubpasses = &subpass,
            };
            GetApi().CreateRenderPass(GraphicsDevice.NativeDevice, &renderPassCreateInfo, null, out NativeRenderPass);
        }

        /// <inheritdoc/>
        protected internal override unsafe void OnDestroyed()
        {
            if (NativePipeline.Handle != 0)
            {
                GetApi().DestroyRenderPass(GraphicsDevice.NativeDevice, NativeRenderPass, null);
                GetApi().DestroyPipeline(GraphicsDevice.NativeDevice, NativePipeline, null);
                GetApi().DestroyPipelineLayout(GraphicsDevice.NativeDevice, NativeLayout, null);

                GetApi().DestroyDescriptorSetLayout(GraphicsDevice.NativeDevice, NativeDescriptorSetLayout, null);
            }

            base.OnDestroyed();
        }

        internal struct DescriptorSetInfo
        {
            public int SourceSet;
            public int SourceBinding;
            public int DestinationBinding;
            public DescriptorType DescriptorType;
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
            var pipelineLayoutCreateInfo = new PipelineLayoutCreateInfo
            {
                SType = StructureType.PipelineLayoutCreateInfo,
                SetLayoutCount = 1,
                PSetLayouts = &nativeDescriptorSetLayout,
            };
            GetApi().CreatePipelineLayout(GraphicsDevice.NativeDevice, &pipelineLayoutCreateInfo, null, out NativeLayout);
        }

        private unsafe PipelineShaderStageCreateInfo[] CreateShaderStages(PipelineStateDescription pipelineStateDescription, out Dictionary<int, string> inputAttributeNames)
        {
            var stages = pipelineStateDescription.EffectBytecode.Stages;
            var nativeStages = new PipelineShaderStageCreateInfo[stages.Length];

            inputAttributeNames = null;

            for (int i = 0; i < stages.Length; i++)
            {
                var shaderBytecode = BinarySerialization.Read<ShaderInputBytecode>(stages[i].Data);
                if (stages[i].Stage == ShaderStage.Vertex)
                    inputAttributeNames = shaderBytecode.InputAttributeNames;

                fixed(byte* data = shaderBytecode.Data)
                fixed (byte* entryPointPointer = &defaultEntryPoint[0])
                {
                    // Create stage
                    nativeStages[i] = new PipelineShaderStageCreateInfo
                    {
                        SType = StructureType.PipelineShaderStageCreateInfo,
                        Stage = VulkanConvertExtensions.Convert(stages[i].Stage),
                        PName = entryPointPointer,
                    };
                    
                    var smc = new ShaderModuleCreateInfo
                    {
                        CodeSize = (nuint)shaderBytecode.Data.Length,
                        SType = StructureType.ShaderModuleCreateInfo,
                        PCode = (uint*)data,
                        PNext = null,
                        Flags = 0
                    };
                    
                    GetApi().CreateShaderModule(GraphicsDevice.NativeDevice, smc, null, out nativeStages[i].Module);
                }
            };

            return nativeStages;
        }

        private PipelineRasterizationStateCreateInfo CreateRasterizationState(RasterizerStateDescription description)
        {
            return new PipelineRasterizationStateCreateInfo
            {
                SType = StructureType.PipelineRasterizationStateCreateInfo,
                CullMode = VulkanConvertExtensions.ConvertCullMode(description.CullMode),
                FrontFace = description.FrontFaceCounterClockwise ? FrontFace.CounterClockwise : FrontFace.Clockwise,
                PolygonMode = VulkanConvertExtensions.ConvertFillMode(description.FillMode),
                DepthBiasEnable = true, // TODO VULKAN
                DepthBiasConstantFactor = description.DepthBias,
                DepthBiasSlopeFactor = description.SlopeScaleDepthBias,
                DepthBiasClamp = description.DepthBiasClamp,
                LineWidth = 1.0f,
                DepthClampEnable = !description.DepthClipEnable,
                RasterizerDiscardEnable = false,
            };
        }

        private PipelineDepthStencilStateCreateInfo CreateDepthStencilState(PipelineStateDescription pipelineStateDescription)
        {
            var description = pipelineStateDescription.DepthStencilState;

            return new PipelineDepthStencilStateCreateInfo
            {
                SType = StructureType.PipelineDepthStencilStateCreateInfo,
                DepthTestEnable = description.DepthBufferEnable,
                StencilTestEnable = description.StencilEnable,
                DepthWriteEnable = description.DepthBufferWriteEnable,

                MinDepthBounds = 0.0f,
                MaxDepthBounds = 1.0f,
                DepthCompareOp = VulkanConvertExtensions.ConvertComparisonFunction(description.DepthBufferFunction),
                Front =
                {
                    CompareOp = VulkanConvertExtensions.ConvertComparisonFunction(description.FrontFace.StencilFunction),
                    DepthFailOp = VulkanConvertExtensions.ConvertStencilOperation(description.FrontFace.StencilDepthBufferFail),
                    FailOp = VulkanConvertExtensions.ConvertStencilOperation(description.FrontFace.StencilFail),
                    PassOp = VulkanConvertExtensions.ConvertStencilOperation(description.FrontFace.StencilPass),
                    CompareMask = description.StencilMask,
                    WriteMask = description.StencilWriteMask
                },
                Back =
                {
                    CompareOp = VulkanConvertExtensions.ConvertComparisonFunction(description.BackFace.StencilFunction),
                    DepthFailOp = VulkanConvertExtensions.ConvertStencilOperation(description.BackFace.StencilDepthBufferFail),
                    FailOp = VulkanConvertExtensions.ConvertStencilOperation(description.BackFace.StencilFail),
                    PassOp = VulkanConvertExtensions.ConvertStencilOperation(description.BackFace.StencilPass),
                    CompareMask = description.StencilMask,
                    WriteMask = description.StencilWriteMask
                }
            };
        }
    }
}

#endif
