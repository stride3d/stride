// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

using Stride.Shaders;

namespace Stride.Graphics;

/// <summary>
///   A reflection object that describes the Descriptor Sets and their layouts for an Effect / Shader
///   based on reflection data extracted from its bytecode.
///   <br/>
///   This includes the bindings for Graphics Resources such as Textures, Buffers, and Sampler States.
/// </summary>
public class EffectDescriptorSetReflection
{
    /// <summary>
    ///   Gets the default Descriptor Set slot name used for the Graphics Resources in this Effect / Shader when no slot name is specified.
    /// </summary>
    internal string DefaultSetSlot { get; }

    /// <summary>
    ///   Gets a list of Descriptor Set layouts that describe the bindings for Graphics Resources.
    /// </summary>
    internal List<LayoutEntry> Layouts { get; } = [];


    /// <summary>
    ///   Creates a new Effect Descriptor Set reflection object.
    /// </summary>
    /// <param name="graphicsDevice">The Graphics Device used to create Sampler States and manage bindings.</param>
    /// <param name="effectBytecode">The Effect / Shader bytecode containing reflection data for resource bindings and Sampler States.</param>
    /// <param name="effectDescriptorSetSlots">
    ///   A list of Descriptor Set slot names to be processed.
    ///   This usually comes from the resource groups (<c>rgroup</c>s) in the Effects / Shaders, plus a <c>"Globals"</c> slot
    ///   for those Descriptor Set layouts that have no specified a slot / group name.
    /// </param>
    /// <param name="defaultSetSlot">
    ///   The default Descriptor Set slot name used for the Graphics Resources in this Effect / Shader when no slot name is specified.
    ///   Usually, this is <c>"Globals"</c>.
    /// </param>
    /// <returns>
    ///   The new instance of <see cref="EffectDescriptorSetReflection"/> containing the Descriptor Set layouts and default set
    ///   slot information.
    /// </returns>
    public static EffectDescriptorSetReflection New(GraphicsDevice graphicsDevice, EffectBytecode effectBytecode, List<string> effectDescriptorSetSlots, string defaultSetSlot)
    {
        var descriptorSetLayouts = new EffectDescriptorSetReflection(defaultSetSlot);
        var reflection = effectBytecode.Reflection;

        // ResourceGroups are pre-grouped and pre-ordered at compile time
        foreach (var effectDescriptorSetSlot in effectDescriptorSetSlots)
        {
            var group = reflection.FindResourceGroup(effectDescriptorSetSlot, defaultSetSlot);
            if (group == null)
            {
                descriptorSetLayouts.AddLayout(effectDescriptorSetSlot, null);
                continue;
            }

            var descriptorSetLayoutBuilder = new DescriptorSetLayoutBuilder();
            bool hasBindings = false;

            AddGroupEntries(graphicsDevice, group, descriptorSetLayoutBuilder, ref hasBindings);

            // When building the default set slot, also include entries from unnamed/Globals groups
            // (resources without an explicit resource group). This avoids mutating
            // EffectBytecode.Reflection while ensuring those resources are bound.
            if (effectDescriptorSetSlot == defaultSetSlot)
            {
                foreach (var fallbackGroup in reflection.ResourceGroups)
                {
                    if (fallbackGroup != group && fallbackGroup.Name is null or "Globals")
                        AddGroupEntries(graphicsDevice, fallbackGroup, descriptorSetLayoutBuilder, ref hasBindings);
                }
            }

            descriptorSetLayouts.AddLayout(effectDescriptorSetSlot, hasBindings ? descriptorSetLayoutBuilder : null);
        }

        return descriptorSetLayouts;
    }

    private static void AddGroupEntries(GraphicsDevice graphicsDevice, EffectResourceGroupDescription group, DescriptorSetLayoutBuilder builder, ref bool hasBindings)
    {
        foreach (var entry in group.Entries)
        {
            // Note: we do NOT skip entries with Stages == None here.
            // Unused resources must still occupy their slot in the descriptor set layout
            // to preserve logical group offsets used by render features.
            // The ResourceBinder handles this correctly — it simply won't create
            // binding operations for entries with no matching stage.

            SamplerState samplerState = null;
            if (entry.Class == EffectParameterClass.Sampler && entry.SamplerStateDescription.HasValue)
                samplerState = SamplerState.New(graphicsDevice, entry.SamplerStateDescription.Value);

            hasBindings = true;
            builder.AddBinding(entry.KeyInfo.Key, entry.LogicalGroup, entry.Class, entry.Type, entry.ElementType.Type, entry.SlotCount, samplerState);
        }
    }

    private EffectDescriptorSetReflection(string defaultSetSlot)
    {
        DefaultSetSlot = defaultSetSlot;
    }


    /// <summary>
    ///   Gets the layout of the Descriptor Set with the given name.
    /// </summary>
    /// <param name="name">The name of the Descriptor Set.</param>
    /// <returns>
    ///   A <see cref="DescriptorSetLayoutBuilder"/> object that describes the Graphics Resource bindings
    ///   and their layout in the Descriptor Set with the provided <paramref name="name"/>, or
    ///   <see langword="null"/> if no Descriptor Set with that name exists.
    /// </returns>
    public DescriptorSetLayoutBuilder? GetLayout(string name)
    {
        foreach (var entry in Layouts)
        {
            if (entry.Name == name)
                return entry.Layout;
        }

        return null;
    }

    /// <summary>
    ///   Gets the index of the Descriptor Set with the given name.
    /// </summary>
    /// <param name="name">The name of the Descriptor Set.</param>
    /// <returns>
    ///   The zero-based index of the Descriptor Set with the provided <paramref name="name"/>, or
    ///   <c>-1</c> if no Descriptor Set with that name exists.
    /// </returns>
    public int GetLayoutIndex(string name)
    {
        for (int index = 0; index < Layouts.Count; index++)
        {
            if (Layouts[index].Name == name)
                return index;
        }

        return -1;
    }

    /// <summary>
    ///   Adds a new Descriptor Set layout for the given name.
    /// </summary>
    /// <param name="descriptorSetName">The slot name given to the Descriptor Set layout in the Effect / Shader.</param>
    /// <param name="descriptorSetLayoutBuilder">
    ///   A <see cref="DescriptorSetLayoutBuilder"/> that describes the Graphics Resource bindings and their associated
    ///   metadata and layout.
    /// </param>
    public void AddLayout(string descriptorSetName, DescriptorSetLayoutBuilder descriptorSetLayoutBuilder)
    {
        Layouts.Add(new LayoutEntry(descriptorSetName, descriptorSetLayoutBuilder));
    }


    /// <summary>
    ///   A structure associating a Descriptor Set layout with its name.
    /// </summary>
    /// <param name="Name">The Descriptor Set name.</param>
    /// <param name="Layout">
    ///   A <see cref="DescriptorSetLayoutBuilder"/> that describes the Graphics Resource bindings and their associated
    ///   metadata and layout.
    /// </param>
    internal readonly record struct LayoutEntry(string Name, DescriptorSetLayoutBuilder Layout);
}
