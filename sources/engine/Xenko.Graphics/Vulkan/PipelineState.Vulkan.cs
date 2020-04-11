// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_GRAPHICS_API_VULKAN
using System;
using System.Collections.Generic;
using System.Linq;
using SharpVulkan;
using Xenko.Core;
using Xenko.Core.Collections;
using Xenko.Core.Serialization;
using Xenko.Shaders;
using Encoding = System.Text.Encoding;

namespace Xenko.Graphics
{
    public partial class PipelineState
    {
        internal SharpVulkan.DescriptorSetLayout NativeDescriptorSetLayout;
        internal uint[] DescriptorTypeCounts;
        internal DescriptorSetLayout DescriptorSetLayout;

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
            void* defaultEntryPointData = Interop.Fixed(defaultEntryPoint);
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
                StructureType = StructureType.PipelineInputAssemblyStateCreateInfo,
                Topology = VulkanConvertExtensions.ConvertPrimitiveType(Description.PrimitiveType),
                PrimitiveRestartEnable = VulkanConvertExtensions.ConvertPrimitiveRestart(Description.PrimitiveType),
            };

            // TODO VULKAN: Tessellation and multisampling
            var multisampleState = new PipelineMultisampleStateCreateInfo
            {
                StructureType = StructureType.PipelineMultisampleStateCreateInfo,
                RasterizationSamples = SampleCountFlags.Sample1
            };

            var tessellationState = new PipelineTessellationStateCreateInfo();

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
                    AlphaBlendOperation = VulkanConvertExtensions.ConvertBlendFunction(renderTargetBlendState->AlphaBlendFunction),
                    ColorBlendOperation = VulkanConvertExtensions.ConvertBlendFunction(renderTargetBlendState->ColorBlendFunction),
                    DestinationAlphaBlendFactor = VulkanConvertExtensions.ConvertBlend(renderTargetBlendState->AlphaDestinationBlend),
                    DestinationColorBlendFactor = VulkanConvertExtensions.ConvertBlend(renderTargetBlendState->ColorDestinationBlend),
                    SourceAlphaBlendFactor = VulkanConvertExtensions.ConvertBlend(renderTargetBlendState->AlphaSourceBlend),
                    SourceColorBlendFactor = VulkanConvertExtensions.ConvertBlend(renderTargetBlendState->ColorSourceBlend),
                    ColorWriteMask = VulkanConvertExtensions.ConvertColorWriteChannels(renderTargetBlendState->ColorWriteChannels),
                };

                if (description.IndependentBlendEnable)
                    renderTargetBlendState++;
            }

            var viewportState = new PipelineViewportStateCreateInfo
            {
                StructureType = StructureType.PipelineViewportStateCreateInfo,
                ScissorCount = 1,
                ViewportCount = 1,
            };

            fixed (DynamicState* dynamicStatesPointer = &dynamicStates[0])
            {
                var vertexInputState = new PipelineVertexInputStateCreateInfo
                {
                    StructureType = StructureType.PipelineVertexInputStateCreateInfo,
                    VertexAttributeDescriptionCount = (uint)inputAttributeCount,
                    VertexAttributeDescriptions = inputAttributes.Length > 0 ? new IntPtr(Interop.Fixed(inputAttributes)) : IntPtr.Zero,
                    VertexBindingDescriptionCount = (uint)inputBindingCount,
                    VertexBindingDescriptions = inputBindings.Length > 0 ? new IntPtr(Interop.Fixed(inputBindings)) : IntPtr.Zero,
                };

                var colorBlendState = new PipelineColorBlendStateCreateInfo
                {
                    StructureType = StructureType.PipelineColorBlendStateCreateInfo,
                    AttachmentCount = (uint)renderTargetCount,
                    Attachments = colorBlendAttachments.Length > 0 ? new IntPtr(Interop.Fixed(colorBlendAttachments)) : IntPtr.Zero,
                };

                var dynamicState = new PipelineDynamicStateCreateInfo
                {
                    StructureType = StructureType.PipelineDynamicStateCreateInfo,
                    DynamicStateCount = (uint)dynamicStates.Length,
                    DynamicStates = new IntPtr(dynamicStatesPointer)
                };

                var createInfo = new GraphicsPipelineCreateInfo
                {
                    StructureType = StructureType.GraphicsPipelineCreateInfo,
                    Layout = NativeLayout,
                    StageCount = (uint)stages.Length,
                    Stages = stages.Length > 0 ? new IntPtr(Interop.Fixed(stages)) : IntPtr.Zero,
                    //TessellationState = new IntPtr(&tessellationState),
                    VertexInputState = new IntPtr(&vertexInputState),
                    InputAssemblyState = new IntPtr(&inputAssemblyState),
                    RasterizationState = new IntPtr(&rasterizationState),
                    MultisampleState = new IntPtr(&multisampleState),
                    DepthStencilState = new IntPtr(&depthStencilState),
                    ColorBlendState = new IntPtr(&colorBlendState),
                    DynamicState = new IntPtr(&dynamicState),
                    ViewportState = new IntPtr(&viewportState),
                    RenderPass = NativeRenderPass,
                    Subpass = 0,
                };
                NativePipeline = GraphicsDevice.NativeDevice.CreateGraphicsPipelines(PipelineCache.Null, 1, &createInfo);
            }

            // Cleanup shader modules
            foreach (var stage in stages)
            {
                GraphicsDevice.NativeDevice.DestroyShaderModule(stage.Module);
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
                        Samples = SampleCountFlags.Sample1,
                        LoadOperation = currentBlendDesc->BlendEnable ? AttachmentLoadOperation.Load : AttachmentLoadOperation.DontCare, // TODO VULKAN: Only if any destination blend?
                        StoreOperation = AttachmentStoreOperation.Store,
                        StencilLoadOperation = AttachmentLoadOperation.DontCare,
                        StencilStoreOperation = AttachmentStoreOperation.DontCare,
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
                    Samples = SampleCountFlags.Sample1,
                    LoadOperation = AttachmentLoadOperation.Load, // TODO VULKAN: Only if depth read enabled?
                    StoreOperation = AttachmentStoreOperation.Store, // TODO VULKAN: Only if depth write enabled?
                    StencilLoadOperation = AttachmentLoadOperation.DontCare, // TODO VULKAN: Handle stencil
                    StencilStoreOperation = AttachmentStoreOperation.DontCare,
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
                ColorAttachments = colorAttachmentReferences.Length > 0 ? new IntPtr(Interop.Fixed(colorAttachmentReferences)) : IntPtr.Zero,
                DepthStencilAttachment = hasDepthStencilAttachment ? new IntPtr(&depthAttachmentReference) : IntPtr.Zero,
            };

            var renderPassCreateInfo = new RenderPassCreateInfo
            {
                StructureType = StructureType.RenderPassCreateInfo,
                AttachmentCount = (uint)attachmentCount,
                Attachments = attachments.Length > 0 ? new IntPtr(Interop.Fixed(attachments)) : IntPtr.Zero,
                SubpassCount = 1,
                Subpasses = new IntPtr(&subpass)
            };
            NativeRenderPass = GraphicsDevice.NativeDevice.CreateRenderPass(ref renderPassCreateInfo);
        }

        /// <inheritdoc/>
        protected internal override unsafe void OnDestroyed()
        {
            if (NativePipeline != Pipeline.Null)
            {
                GraphicsDevice.NativeDevice.DestroyRenderPass(NativeRenderPass);
                GraphicsDevice.NativeDevice.DestroyPipeline(NativePipeline);
                GraphicsDevice.NativeDevice.DestroyPipelineLayout(NativeLayout);

                GraphicsDevice.NativeDevice.DestroyDescriptorSetLayout(NativeDescriptorSetLayout);
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
                StructureType = StructureType.PipelineLayoutCreateInfo,
                SetLayoutCount = 1,
                SetLayouts = new IntPtr(&nativeDescriptorSetLayout)
            };
            NativeLayout = GraphicsDevice.NativeDevice.CreatePipelineLayout(ref pipelineLayoutCreateInfo);
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

                fixed (byte* entryPointPointer = &defaultEntryPoint[0])
                fixed (byte* codePointer = &shaderBytecode.Data[0])
                {
                    // Create shader module
                    var moduleCreateInfo = new ShaderModuleCreateInfo
                    {
                        StructureType = StructureType.ShaderModuleCreateInfo,
                        Code = new IntPtr(codePointer),
                        CodeSize = shaderBytecode.Data.Length
                    };

                    // Create stage
                    nativeStages[i] = new PipelineShaderStageCreateInfo
                    {
                        StructureType = StructureType.PipelineShaderStageCreateInfo,
                        Stage = VulkanConvertExtensions.Convert(stages[i].Stage),
                        Name = new IntPtr(entryPointPointer),
                        Module = GraphicsDevice.NativeDevice.CreateShaderModule(ref moduleCreateInfo)
                    };
                }
            };

            return nativeStages;
        }

        private PipelineRasterizationStateCreateInfo CreateRasterizationState(RasterizerStateDescription description)
        {
            return new PipelineRasterizationStateCreateInfo
            {
                StructureType = StructureType.PipelineRasterizationStateCreateInfo,
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
                StructureType = StructureType.PipelineDepthStencilStateCreateInfo,
                DepthTestEnable = description.DepthBufferEnable,
                StencilTestEnable = description.StencilEnable,
                DepthWriteEnable = description.DepthBufferWriteEnable,

                MinDepthBounds = 0.0f,
                MaxDepthBounds = 1.0f,
                DepthCompareOperation = VulkanConvertExtensions.ConvertComparisonFunction(description.DepthBufferFunction),
                Front = new StencilOperationState
                {
                    CompareOperation = VulkanConvertExtensions.ConvertComparisonFunction(description.FrontFace.StencilFunction),
                    DepthFailOperation = VulkanConvertExtensions.ConvertStencilOperation(description.FrontFace.StencilDepthBufferFail),
                    FailOperation = VulkanConvertExtensions.ConvertStencilOperation(description.FrontFace.StencilFail),
                    PassOperation = VulkanConvertExtensions.ConvertStencilOperation(description.FrontFace.StencilPass),
                    CompareMask = description.StencilMask,
                    WriteMask = description.StencilWriteMask
                },
                Back = new StencilOperationState
                {
                    CompareOperation = VulkanConvertExtensions.ConvertComparisonFunction(description.BackFace.StencilFunction),
                    DepthFailOperation = VulkanConvertExtensions.ConvertStencilOperation(description.BackFace.StencilDepthBufferFail),
                    FailOperation = VulkanConvertExtensions.ConvertStencilOperation(description.BackFace.StencilFail),
                    PassOperation = VulkanConvertExtensions.ConvertStencilOperation(description.BackFace.StencilPass),
                    CompareMask = description.StencilMask,
                    WriteMask = description.StencilWriteMask
                }
            };
        }
    }
}

#endif
