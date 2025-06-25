// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

using Stride.Shaders;

namespace Stride.Graphics;

public class EffectDescriptorSetReflection
{
    internal string DefaultSetSlot { get; }

    internal List<LayoutEntry> Layouts { get; } = [];


    public static EffectDescriptorSetReflection New(GraphicsDevice graphicsDevice, EffectBytecode effectBytecode, List<string> effectDescriptorSetSlots, string defaultSetSlot)
    {
        var descriptorSetLayouts = new EffectDescriptorSetReflection(defaultSetSlot);

        // Find resource groups
        // TODO: We should precompute most of that at compile time in BytecodeReflection. Jjust waiting for format to be more stable
        foreach (var effectDescriptorSetSlot in effectDescriptorSetSlots)
        {
            // Find all resources related to this slot name
            // NOTE: Ordering is mirrored by GLSL layout in Vulkan
            var descriptorSetLayoutBuilder = new DescriptorSetLayoutBuilder();
            bool hasBindings = false;

            var resourceBindingsBySlot = effectBytecode.Reflection.ResourceBindings
                // Resource bindings of a group with the same name as the slot,
                // or to no group/to Globals group if default slot is used
                .Where(x => x.ResourceGroup == effectDescriptorSetSlot ||
                            (effectDescriptorSetSlot == defaultSetSlot && (x.ResourceGroup is null or "Globals")))
                .GroupBy(x => (x.KeyInfo.Key, x.Class, x.Type, ElementType: x.ElementType.Type, x.SlotCount, x.LogicalGroup))
                // NOTE: Putting Constant Buffers first for now
                .OrderBy(x => x.Key.Class == EffectParameterClass.ConstantBuffer ? 0 : 1);

            foreach (var resourceBinding in resourceBindingsBySlot)
            {
                SamplerState samplerState = null;
                if (resourceBinding.Key.Class == EffectParameterClass.Sampler)
                {
                    var matchingSamplerState = effectBytecode.Reflection.SamplerStates.FirstOrDefault(x => x.Key == resourceBinding.Key.Key);
                    if (matchingSamplerState is not null)
                        samplerState = SamplerState.New(graphicsDevice, matchingSamplerState.Description);
                }
                hasBindings = true;

                descriptorSetLayoutBuilder.AddBinding(resourceBinding.Key.Key, resourceBinding.Key.LogicalGroup, resourceBinding.Key.Class, resourceBinding.Key.Type, resourceBinding.Key.ElementType, resourceBinding.Key.SlotCount, samplerState);
            }

            descriptorSetLayouts.AddLayout(effectDescriptorSetSlot, hasBindings ? descriptorSetLayoutBuilder : null);
        }

        return descriptorSetLayouts;
    }

    private EffectDescriptorSetReflection(string defaultSetSlot)
    {
        DefaultSetSlot = defaultSetSlot;
    }


    public DescriptorSetLayoutBuilder? GetLayout(string name)
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


    internal readonly record struct LayoutEntry(string Name, DescriptorSetLayoutBuilder Layout);
}
