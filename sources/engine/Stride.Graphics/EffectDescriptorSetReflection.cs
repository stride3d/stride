// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using Stride.Graphics;
using Stride.Shaders;

namespace Stride.Graphics
{
    public class EffectDescriptorSetReflection
    {
        public static EffectDescriptorSetReflection New(GraphicsDevice graphicsDevice, EffectBytecode effectBytecode, List<string> effectDescriptorSetSlots, string defaultSetSlot)
        {
            // Find resource groups
            // TODO: We should precompute most of that at compile time in BytecodeReflection
            // just waiting for format to be more stable
            var descriptorSetLayouts = new EffectDescriptorSetReflection { DefaultSetSlot = defaultSetSlot };
            foreach (var effectDescriptorSetSlot in effectDescriptorSetSlots)
            {
                // Find all resources related to this slot name
                // NOTE: Ordering is mirrored by GLSL layout in Vulkan
                var descriptorSetLayoutBuilder = new DescriptorSetLayoutBuilder();
                bool hasBindings = false;
                foreach (var resourceBinding in effectBytecode.Reflection.ResourceBindings
                    .Where(x => x.ResourceGroup == effectDescriptorSetSlot || (effectDescriptorSetSlot == defaultSetSlot && (x.ResourceGroup == null || x.ResourceGroup == "Globals")))
                    .GroupBy(x => new { Key = x.KeyInfo.Key, Class = x.Class, Type = x.Type, ElementType = x.ElementType.Type, SlotCount = x.SlotCount, LogicalGroup = x.LogicalGroup })
                    .OrderBy(x => x.Key.Class == EffectParameterClass.ConstantBuffer ? 0 : 1)) // Note: Putting cbuffer first for now
                {
                    SamplerState samplerState = null;
                    if (resourceBinding.Key.Class == EffectParameterClass.Sampler)
                    {
                        var matchingSamplerState = effectBytecode.Reflection.SamplerStates.FirstOrDefault(x => x.Key == resourceBinding.Key.Key);
                        if (matchingSamplerState != null)
                            samplerState = SamplerState.New(graphicsDevice, matchingSamplerState.Description);
                    }
                    hasBindings = true;

                    descriptorSetLayoutBuilder.AddBinding(resourceBinding.Key.Key, resourceBinding.Key.LogicalGroup, resourceBinding.Key.Class, resourceBinding.Key.Type, resourceBinding.Key.ElementType, resourceBinding.Key.SlotCount, samplerState);
                }

                descriptorSetLayouts.AddLayout(effectDescriptorSetSlot, hasBindings ? descriptorSetLayoutBuilder : null);
            }

            return descriptorSetLayouts;
        }

        internal string DefaultSetSlot { get; private set; }

        internal List<LayoutEntry> Layouts { get; } = new List<LayoutEntry>();

        public DescriptorSetLayoutBuilder GetLayout(string name)
        {
            foreach (var entry in Layouts)
            {
                if (entry.Name == name)
                    return entry.Layout;
            }

            return null;
        }

        public int GetLayoutIndex(string name)
        {
            for (int index = 0; index < Layouts.Count; index++)
            {
                if (Layouts[index].Name == name)
                    return index;
            }

            return -1;
        }

        public void AddLayout(string descriptorSetName, DescriptorSetLayoutBuilder descriptorSetLayoutBuilder)
        {
            Layouts.Add(new LayoutEntry(descriptorSetName, descriptorSetLayoutBuilder));
        }

        internal struct LayoutEntry
        {
            public string Name;
            public DescriptorSetLayoutBuilder Layout;

            public LayoutEntry(string name, DescriptorSetLayoutBuilder layout)
            {
                Name = name;
                Layout = layout;
            }
        }
    }
}
