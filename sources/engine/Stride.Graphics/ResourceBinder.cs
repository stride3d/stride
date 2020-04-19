// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_DIRECT3D11 || STRIDE_GRAPHICS_API_OPENGL
using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Shaders;

namespace Stride.Graphics
{
    internal struct ResourceBinder
    {
        private BindingOperation[][] descriptorSetBindings;

        public void Compile(GraphicsDevice graphicsDevice, EffectDescriptorSetReflection descriptorSetLayouts, EffectBytecode effectBytecode)
        {
            descriptorSetBindings = new BindingOperation[descriptorSetLayouts.Layouts.Count][];
            for (int setIndex = 0; setIndex < descriptorSetLayouts.Layouts.Count; setIndex++)
            {
                var layout = descriptorSetLayouts.Layouts[setIndex].Layout;
                if (layout == null)
                    continue;

                var bindingOperations = new List<BindingOperation>();

                for (int resourceIndex = 0; resourceIndex < layout.Entries.Count; resourceIndex++)
                {
                    var layoutEntry = layout.Entries[resourceIndex];

                    // Find it in shader reflection
                    foreach (var resourceBinding in effectBytecode.Reflection.ResourceBindings)
                    {
                        if (resourceBinding.Stage == ShaderStage.None)
                            continue;

                        if (resourceBinding.KeyInfo.Key == layoutEntry.Key)
                        {
                            bindingOperations.Add(new BindingOperation
                            {
                                EntryIndex = resourceIndex,
                                Class = resourceBinding.Class,
                                Stage = resourceBinding.Stage,
                                SlotStart = resourceBinding.SlotStart,
                                ImmutableSampler = layoutEntry.ImmutableSampler,
                            });
                        }
                    }
                }

                descriptorSetBindings[setIndex] = bindingOperations.Count > 0 ? bindingOperations.ToArray() : null;
            }
        }
        public void BindResources(CommandList commandList, DescriptorSet[] descriptorSets)
        {
            for (int setIndex = 0; setIndex < descriptorSetBindings.Length; setIndex++)
            {
                var bindingOperations = descriptorSetBindings[setIndex];
                if (bindingOperations == null)
                    continue;

                var descriptorSet = descriptorSets[setIndex];

                var bindingOperation = Interop.Pin(ref bindingOperations[0]);
                for (int index = 0; index < bindingOperations.Length; index++, bindingOperation = Interop.IncrementPinned(bindingOperation))
                {
                    var value = descriptorSet.HeapObjects[descriptorSet.DescriptorStartOffset + bindingOperation.EntryIndex];
                    switch (bindingOperation.Class)
                    {
                        case EffectParameterClass.ConstantBuffer:
                            {
                                commandList.SetConstantBuffer(bindingOperation.Stage, bindingOperation.SlotStart, (Buffer)value.Value);
                                break;
                            }
                        case EffectParameterClass.Sampler:
                            {
                                commandList.SetSamplerState(bindingOperation.Stage, bindingOperation.SlotStart, bindingOperation.ImmutableSampler ?? (SamplerState)value.Value);
                                break;
                            }
                        case EffectParameterClass.ShaderResourceView:
                            {
                                commandList.UnsetUnorderedAccessView(value.Value as GraphicsResource);
                                commandList.SetShaderResourceView(bindingOperation.Stage, bindingOperation.SlotStart, (GraphicsResource)value.Value);
                                break;
                            }
                        case EffectParameterClass.UnorderedAccessView:
                            {
                                commandList.SetUnorderedAccessView(bindingOperation.Stage, bindingOperation.SlotStart, (GraphicsResource)value.Value, value.Offset);
                                break;
                            }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        internal struct BindingOperation
        {
            public int EntryIndex;
            public EffectParameterClass Class;
            public ShaderStage Stage;
            public int SlotStart;
            public SamplerState ImmutableSampler;
        }
    }
}
#endif
