// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_NULL

namespace Stride.Graphics
{
    /// <summary>
    /// Contains a list descriptors (such as textures) that can be bound together to the graphics pipeline.
    /// </summary>
    public partial struct DescriptorSet
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DescriptorSet"/> for <param name="graphicsDevice"/> using
        /// the <see cref="DescriptorPool"/> <param name="pool"/> and <see cref="DescriptorSetLayout"/> <param name="desc"/>.
        /// </summary>
        /// <param name="graphicsDevice">The graphics device.</param>
        /// <param name="pool">The descriptor pool.</param>
        /// <param name="desc">The descriptor set layout.</param>
        private DescriptorSet(GraphicsDevice graphicsDevice, DescriptorPool pool, DescriptorSetLayout desc)
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        /// Is set valid?
        /// </summary>
        public bool IsValid
        {
            get
            {
                NullHelper.ToImplement();
                return false;
            }
        }

        /// <summary>
        /// Sets a descriptor.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="value">The descriptor.</param>
        public void SetValue(int slot, object value)
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        /// Sets a shader resource view descriptor.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="shaderResourceView">The shader resource view.</param>
        public void SetShaderResourceView(int slot, GraphicsResource shaderResourceView)
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        /// Sets a sampler state descriptor.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="samplerState">The sampler state.</param>
        public void SetSamplerState(int slot, SamplerState samplerState)
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        /// Sets a constant buffer view descriptor.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="buffer">The constant buffer.</param>
        /// <param name="offset">The constant buffer view start offset.</param>
        /// <param name="size">The constant buffer view size.</param>
        public void SetConstantBuffer(int slot, Buffer buffer, int offset, int size)
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        /// Sets an unordered access view descriptor.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="unorderedAccessView">The unordered access view.</param>
        public void SetUnorderedAccessView(int slot, GraphicsResource unorderedAccessView)
        {
            NullHelper.ToImplement();
        }
    }
}

#endif
