// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Storage;
using Stride.Shaders;

namespace Stride.Graphics
{
    public class ResourceGroupLayout
    {
        public DescriptorSetLayoutBuilder DescriptorSetLayoutBuilder;
        public DescriptorSetLayout DescriptorSetLayout;
        public int ConstantBufferSize;
        public EffectConstantBufferDescription ConstantBufferReflection;
        public ObjectId Hash;
        public ObjectId ConstantBufferHash;

        public static ResourceGroupLayout New(GraphicsDevice graphicsDevice, ResourceGroupDescription resourceGroupDescription, EffectBytecode effectBytecode)
        {
            return New<ResourceGroupLayout>(graphicsDevice, resourceGroupDescription, effectBytecode);
        }

        public static ResourceGroupLayout New<T>(GraphicsDevice graphicsDevice, ResourceGroupDescription resourceGroupDescription, EffectBytecode effectBytecode) where T : ResourceGroupLayout, new()
        {
            var result = new T
            {
                DescriptorSetLayoutBuilder = resourceGroupDescription.DescriptorSetLayout,
                DescriptorSetLayout = DescriptorSetLayout.New(graphicsDevice, resourceGroupDescription.DescriptorSetLayout),
                ConstantBufferReflection = resourceGroupDescription.ConstantBufferReflection,
                Hash = resourceGroupDescription.Hash,
            };

            if (result.ConstantBufferReflection != null)
            {
                result.ConstantBufferSize = result.ConstantBufferReflection.Size;
                result.ConstantBufferHash = result.ConstantBufferReflection.Hash;
            }

            return result;
        }
    }
}
