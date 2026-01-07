// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Storage;
using Stride.Shaders;

namespace Stride.Graphics;

/// <summary>
///   Represents a description of a resource group, including the layout of its Graphics Resources
///   and Constant Buffers.
/// </summary>
/// <remarks>
///   A resource group is a collection of resources (e.g., <see cref="Texture"/>s, <see cref="Buffer"/>s)
///   that are grouped together for use in rendering or compute operations, and that are usually updated
///   at a specific frequency.
/// </remarks>
public readonly struct ResourceGroupDescription
{
    /// <summary>
    ///   A description of the layout of the Descriptor Set for the resource group.
    /// </summary>
    public readonly DescriptorSetLayoutBuilder DescriptorSetLayout;
    /// <summary>
    ///   A description of the Constant Buffer used by this resource group, if any.
    /// </summary>
    public readonly EffectConstantBufferDescription ConstantBufferReflection;

    /// <summary>
    ///   A hash value identifying the resource group description.
    /// </summary>
    public readonly ObjectId Hash;


    /// <summary>
    ///   Initializes a new instance of the <see cref="ResourceGroupDescription"/> structure.
    /// </summary>
    /// <param name="descriptorSetLayout">
    ///   A description of the layout of the Descriptor Set for the resource group.
    /// </param>
    /// <param name="constantBufferReflection">
    ///   A desription of the Constant Buffer used by this resource group, if any.
    ///   Can be <see langword="null"/> if no Constant Buffer is used.
    /// </param>
    public ResourceGroupDescription(DescriptorSetLayoutBuilder descriptorSetLayout, EffectConstantBufferDescription constantBufferReflection) : this()
    {
        DescriptorSetLayout = descriptorSetLayout;
        ConstantBufferReflection = constantBufferReflection;

        // We combine both hashes for DescriptorSet and Constant Buffer itself (if it exists)
        Hash = descriptorSetLayout.Hash;
        if (constantBufferReflection is not null)
            ObjectId.Combine(ref Hash, ref constantBufferReflection.Hash, out Hash);
    }
}
