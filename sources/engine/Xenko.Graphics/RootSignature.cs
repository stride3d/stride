// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Shaders;

namespace Xenko.Graphics
{
    /// <summary>
    /// Describes how <see cref="DescriptorSet"/> will be bound together.
    /// </summary>
    public class RootSignature : GraphicsResourceBase
    {
        internal readonly EffectDescriptorSetReflection EffectDescriptorSetReflection;

        public static RootSignature New(GraphicsDevice graphicsDevice, EffectDescriptorSetReflection effectDescriptorSetReflection)
        {
            return new RootSignature(graphicsDevice, effectDescriptorSetReflection);
        }

        private RootSignature(GraphicsDevice graphicsDevice, EffectDescriptorSetReflection effectDescriptorSetReflection)
            : base(graphicsDevice)
        {
            this.EffectDescriptorSetReflection = effectDescriptorSetReflection;
        }

        protected internal override bool OnRecreate()
        {
            return true;
        }
    }
}
