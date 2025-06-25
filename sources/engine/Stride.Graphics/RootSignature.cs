// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

public class RootSignature : GraphicsResourceBase
{
    /// <summary>
    /// Describes how <see cref="DescriptorSet"/> will be bound together.
    /// </summary>
    internal EffectDescriptorSetReflection EffectDescriptorSetReflection { get; }


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
