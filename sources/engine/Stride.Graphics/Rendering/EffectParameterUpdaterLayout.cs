// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;

using Stride.Graphics;
using Stride.Shaders;

namespace Stride.Rendering
{
    /// <summary>
    ///   Defines the layout used by <see cref="EffectParameterUpdater"/> to update several <see cref="ResourceGroup"/>s
    ///   from a <see cref="ParameterCollection"/>.
    /// </summary>
    public class EffectParameterUpdaterLayout
    {
        /// <summary>
        ///   An array of descriptions of the layouts for the resource groups used by the Effect.
        /// </summary>
        internal ResourceGroupLayout[] ResourceGroupLayouts;

        /// <summary>
        ///   An array of <see cref="DescriptorSetLayoutBuilder"/>s that define the structure
        ///   of resource bindings for the Effect.
        /// </summary>
        internal DescriptorSetLayoutBuilder[] Layouts;
        /// <summary>
        ///   A description of how a collection of parameters map to binding slots and Constant Buffer data.
        /// </summary>
        internal ParameterCollectionLayout ParameterCollectionLayout = new();


        /// <summary>
        ///   Initializes a new instance of the <see cref="EffectParameterUpdaterLayout"/> class.
        /// </summary>
        /// <param name="graphicsDevice">
        ///   The Graphics Device used to create resource group layouts. Cannot be <see langword="null"/>.
        /// </param>
        /// <param name="effect">
        ///   The effect whose parameters and constant buffers are processed. Cannot be <see langword="null"/>.
        /// </param>
        /// <param name="layouts">
        ///   An array of <see cref="DescriptorSetLayoutBuilder"/> instances that define the layouts
        ///   for resource groups. Each layout is processed to extract resources and Constant Buffers.
        ///   Cannot be <see langword="null"/> or contain <see langword="null"/> elements.
        /// </param>
        /// <remarks>
        ///   This constructor processes the provided Descriptor Set layouts to create resource group layouts
        ///   and associates them with the Effect's parameters.
        ///   It identifies Constant Buffers within the layouts and processes them to ensure they are compatible
        ///   with the Effect's bytecode.
        /// </remarks>
        public EffectParameterUpdaterLayout(GraphicsDevice graphicsDevice,
                                            EffectBytecode effectBytecode, DescriptorSetLayoutBuilder[] layouts)
        {
            Layouts = layouts;

            ResourceGroupLayouts = new ResourceGroupLayout[layouts.Length];

            // Process Constant Buffers
            for (int layoutIndex = 0; layoutIndex < layouts.Length; layoutIndex++)
            {
                var layout = layouts[layoutIndex];
                if (layout is null)
                    continue;

                ParameterCollectionLayout.ProcessResources(layout);

                EffectConstantBufferDescription cbuffer = null;

                for (int entryIndex = 0; entryIndex < layout.Entries.Count; ++entryIndex)
                {
                    var layoutEntry = layout.Entries[entryIndex];
                    if (layoutEntry.Class == EffectParameterClass.ConstantBuffer)
                    {
                        // For now we assume first Constant Buffer will be the main one
                        if (cbuffer is null)
                        {
                            cbuffer = effectBytecode.Reflection.ConstantBuffers.First(x => x.Name == layoutEntry.Key.Name);
                            ParameterCollectionLayout.ProcessConstantBuffer(cbuffer);
                        }
                    }
                }

                var resourceGroupDescription = new ResourceGroupDescription(layout, cbuffer);

                ResourceGroupLayouts[layoutIndex] = ResourceGroupLayout.New(graphicsDevice, resourceGroupDescription);
            }
        }
    }
}
