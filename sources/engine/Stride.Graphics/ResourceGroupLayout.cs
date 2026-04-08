// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Storage;
using Stride.Shaders;

namespace Stride.Graphics;

/// <summary>
///   Represents the layout of the Graphics Resources and parameter values which form a resource group.
/// </summary>
public class ResourceGroupLayout
{
    /// <summary>
    ///   The Descriptor Set layout builder used to describe the Descriptors for the resource group.
    /// </summary>
    public DescriptorSetLayoutBuilder DescriptorSetLayoutBuilder;
    /// <summary>
    ///   The Descriptor Set layout that defines the structure of the resource group.
    /// </summary>
    public DescriptorSetLayout DescriptorSetLayout;

    /// <summary>
    ///   The size of the Constant Buffer associated with the resource group layout.
    /// </summary>
    public int ConstantBufferSize;
    /// <summary>
    ///   A description of the Constant Buffer associated with the resource group layout,
    ///   including the values and types of its parameters.
    /// </summary>
    public EffectConstantBufferDescription ConstantBufferReflection;

    /// <summary>
    ///   A hash value identifying the resource group layout.
    /// </summary>
    public ObjectId Hash;
    /// <summary>
    ///   A hash value identifying the Constant Buffer, used to track changes and updates.
    /// </summary>
    public ObjectId ConstantBufferHash;


    /// <summary>
    ///   Creates a new instance of a <see cref="ResourceGroupLayout"/>.
    /// </summary>
    /// <param name="graphicsDevice">
    ///   The Graphics Device to use to manage GPU resources. Cannot be <see langword="null"/>.
    /// </param>
    /// <param name="resourceGroupDescription">
    ///   A description of the resource group, specifying its layout and resources. Cannot be <see langword="null"/>.
    /// </param>
    /// <returns>A new instance of <see cref="ResourceGroupLayout"/>.</returns>
    public static ResourceGroupLayout New(GraphicsDevice graphicsDevice, ResourceGroupDescription resourceGroupDescription)
    {
        return New<ResourceGroupLayout>(graphicsDevice, resourceGroupDescription);
    }

    /// <summary>
    ///   Creates a new instance of a <see cref="ResourceGroupLayout"/>.
    /// </summary>
    /// <typeparam name="TLayout">The type of resource group layout to create.</typeparam>
    /// <param name="graphicsDevice">
    ///   The Graphics Device to use to manage GPU resources. Cannot be <see langword="null"/>.
    /// </param>
    /// <param name="resourceGroupDescription">
    ///   A description of the resource group, specifying its layout and resources. Cannot be <see langword="null"/>.
    /// </param>
    /// <returns>A new instance of <see cref="ResourceGroupLayout"/>.</returns>
    public static ResourceGroupLayout New<TLayout>(GraphicsDevice graphicsDevice, ResourceGroupDescription resourceGroupDescription)
        where TLayout : ResourceGroupLayout, new()
    {
        var result = new TLayout
        {
            DescriptorSetLayoutBuilder = resourceGroupDescription.DescriptorSetLayout,
            DescriptorSetLayout = DescriptorSetLayout.New(graphicsDevice, resourceGroupDescription.DescriptorSetLayout),
            ConstantBufferReflection = resourceGroupDescription.ConstantBufferReflection,
            Hash = resourceGroupDescription.Hash
        };

        if (result.ConstantBufferReflection is not null)
        {
            result.ConstantBufferSize = result.ConstantBufferReflection.Size;
            result.ConstantBufferHash = result.ConstantBufferReflection.Hash;
        }

        return result;
    }
}
