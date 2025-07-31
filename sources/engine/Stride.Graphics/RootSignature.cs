// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   Represents a <strong>Root Signature</strong>, used to specify how Graphics Resources, such as Textures and Buffers,
///   are bound to the graphics pipeline (i.e. how <see cref="DescriptorSet"/> will be bound together).
/// </summary>
/// <remarks>
///   <para>
///     A Root Signature links Command Lists to the Graphics Resources the Shaders require.
///     It is similar to a function signature, it determines the types of data the Shaders should expect, but does not define
///     the actual memory or data.
///   </para>
///   <para>
///     The Graphics Resources are described by <strong>Descriptors</strong> and bundled in <see cref="DescriptorSet"/>s.
///     An <see cref="Graphics.EffectDescriptorSetReflection"/> defines the layout of the Graphics Resources, bind slot names,
///     and other metadata.
///   </para>
/// </remarks>
public class RootSignature : GraphicsResourceBase
{
    /// <summary>
    ///   Gets a description of the layout, types, etc. of the Graphics Resources to bind.
    /// </summary>
    internal EffectDescriptorSetReflection EffectDescriptorSetReflection { get; }


    /// <summary>
    ///   Creates a new Root Signature from the given <see cref="Graphics.EffectDescriptorSetReflection"/>.
    /// </summary>
    /// <param name="graphicsDevice">The <see cref="GraphicsDevice"/>.</param>
    /// <param name="effectDescriptorSetReflection">A description of the layout of the Graphics Resources to bind.</param>
    /// <returns>The new Root Signature.</returns>
    public static RootSignature New(GraphicsDevice graphicsDevice, EffectDescriptorSetReflection effectDescriptorSetReflection)
    {
        return new RootSignature(graphicsDevice, effectDescriptorSetReflection);
    }

    private RootSignature(GraphicsDevice graphicsDevice, EffectDescriptorSetReflection effectDescriptorSetReflection)
        : base(graphicsDevice)
    {
        EffectDescriptorSetReflection = effectDescriptorSetReflection;
    }


    /// <inheritdoc/>
    protected internal override bool OnRecreate()
    {
        return true;
    }
}
