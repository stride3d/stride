// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Storage;
using Stride.Shaders;

namespace Stride.Graphics
{
    public struct ResourceGroupDescription
    {
        public readonly DescriptorSetLayoutBuilder DescriptorSetLayout;
        public readonly EffectConstantBufferDescription ConstantBufferReflection;
        public readonly ObjectId Hash;

        public ResourceGroupDescription(DescriptorSetLayoutBuilder descriptorSetLayout, EffectConstantBufferDescription constantBufferReflection) : this()
        {
            DescriptorSetLayout = descriptorSetLayout;
            ConstantBufferReflection = constantBufferReflection;

            // We combine both hash for DescriptorSet and cbuffer itself (if it exists)
            Hash = descriptorSetLayout.Hash;
            if (constantBufferReflection != null)
                ObjectId.Combine(ref Hash, ref constantBufferReflection.Hash, out Hash);
        }
    }
}
