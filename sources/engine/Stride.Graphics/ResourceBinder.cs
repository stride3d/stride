// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11 || STRIDE_GRAPHICS_API_OPENGL

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Stride.Shaders;

namespace Stride.Graphics
{
    /// <summary>
    ///   Provides functionality for compiling and binding Shader resource bindings to Descriptor Sets.
    /// </summary>
    /// <remarks>
    ///   The <see cref="ResourceBinder"/> structure is used to process Descriptor Set layouts and Effect bytecode
    ///   to generate binding operations that map Shader resources to Descriptor Set entries.
    ///   These binding operations can then be used to bind Graphics Resources to the graphics pipeline during rendering.
    /// </remarks>
    internal struct ResourceBinder
    {
        // Binding operations organized by Descriptor Sets:
        //   Each element corresponds to a Descriptor Set, and each Descriptor Set contains an array of binding operations.
        private BindingOperation[][] descriptorSetBindings;


        /// <summary>
        ///   Processes the Descriptor Set layouts and an <see cref="EffectBytecode"/> object to generate
        ///   binding operations that map Shader Resource bindings to Descriptor Set entries.
        ///   <br/>
        ///   The resulting bindings are stored internally and can be used for rendering operations.
        /// </summary>
        /// <param name="descriptorSetLayouts">
        ///   An <see cref="EffectDescriptorSetReflection"/> object containing reflection data for the Descriptor Set layouts,
        ///   which provides information about the layout structure.
        /// </param>
        /// <param name="effectBytecode">The bytecode of the Effect, including reflection data for resource bindings.</param>
        public void Compile(EffectDescriptorSetReflection descriptorSetLayouts, EffectBytecode effectBytecode)
        {
            descriptorSetBindings = new BindingOperation[descriptorSetLayouts.Layouts.Count][];

            for (int setIndex = 0; setIndex < descriptorSetLayouts.Layouts.Count; setIndex++)
            {
                var layout = descriptorSetLayouts.Layouts[setIndex].Layout;
                if (layout is null)
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
                            // Create a binding operation for this resource (Descriptor)
                            bindingOperations.Add(new BindingOperation
                            {
                                EntryIndex = resourceIndex,
                                Class = resourceBinding.Class,
                                Stage = resourceBinding.Stage,
                                SlotStart = resourceBinding.SlotStart,
                                ImmutableSampler = layoutEntry.ImmutableSampler
                            });
                        }
                    }
                }

                // Store the binding operations for this Descriptor Set
                descriptorSetBindings[setIndex] = bindingOperations.Count > 0 ? bindingOperations.ToArray() : null;
            }
        }

        /// <summary>
        ///   Binds the resources from the specified Descriptor Sets to the graphics pipeline.
        /// </summary>
        /// <param name="commandList">The Command List.</param>
        /// <param name="descriptorSets">
        ///   An array of Descriptor Sets containing the Graphics Resources to bind.
        ///   Each Descriptor Set corresponds to a set of binding operations. They must have been compiled
        ///   previously using <see cref="Compile(EffectDescriptorSetReflection, EffectBytecode)"/>.
        /// </param>
        /// <exception cref="NotSupportedException">
        ///   Thrown if a binding operation specifies an unsupported effect parameter type.
        /// </exception>
        /// <remarks>
        ///   This method iterates through the Descriptor Sets and performs the binding operations needed
        ///   to bind the Descriptors (like Constant Buffers, Samplers, Shader Resource Views, and Unordered Access Views)
        ///   to the graphics pipeline.
        /// </remarks>
        public readonly void BindResources(CommandList commandList, DescriptorSet[] descriptorSets)
        {
            for (int setIndex = 0; setIndex < descriptorSetBindings.Length; setIndex++)
            {
                var bindingOperations = descriptorSetBindings[setIndex];
                if (bindingOperations is null)
                    continue;

                var descriptorSet = descriptorSets[setIndex];

                ref var bindingOperation = ref MemoryMarshal.GetArrayDataReference(bindingOperations);
                ref var end = ref Unsafe.Add(ref bindingOperation, bindingOperations.Length);

                for (; Unsafe.IsAddressLessThan(ref bindingOperation, ref end); bindingOperation = ref Unsafe.Add(ref bindingOperation, 1))
                {
                    var value = descriptorSet.HeapObjects[descriptorSet.DescriptorStartOffset + bindingOperation.EntryIndex];

                    switch (bindingOperation.Class)
                    {
                        case EffectParameterClass.ConstantBuffer:
                            commandList.SetConstantBuffer(bindingOperation.Stage, bindingOperation.SlotStart, (Buffer) value.Value);
                            break;

                        case EffectParameterClass.Sampler:
                            commandList.SetSamplerState(bindingOperation.Stage, bindingOperation.SlotStart, bindingOperation.ImmutableSampler ?? (SamplerState) value.Value);
                            break;

                        case EffectParameterClass.ShaderResourceView:
                            commandList.UnsetUnorderedAccessView(value.Value as GraphicsResource);
                            commandList.SetShaderResourceView(bindingOperation.Stage, bindingOperation.SlotStart, (GraphicsResource) value.Value);
                            break;

                        case EffectParameterClass.UnorderedAccessView:
                            commandList.SetUnorderedAccessView(bindingOperation.Stage, bindingOperation.SlotStart, (GraphicsResource) value.Value, value.Offset);
                            break;

                        default:
                            throw new NotSupportedException($"Binding operation on an unsupported Effect parameter type [{bindingOperation.Class}]");
                    }
                }
            }
        }


        /// <summary>
        ///   Represents a binding operation used to configure shader parameters and resources.
        /// </summary>
        private struct BindingOperation
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
