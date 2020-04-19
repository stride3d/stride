// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_NULL

namespace Stride.Graphics
{
    /// <summary>
    /// Defines a list of descriptor layout. This is used to allocate a <see cref="DescriptorSet"/>.
    /// </summary>
    public partial class DescriptorSetLayout : GraphicsResourceBase
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DescriptorSetLayout"/> for <param name="device"/> using the 
        /// <see cref="DescriptorSetLayoutBuilder"/> <param name="builder"/>.
        /// </summary>
        /// <param name="device">The graphics device.</param>
        /// <param name="builder">The descriptor set layout builder.</param>
        private DescriptorSetLayout(GraphicsDevice device, DescriptorSetLayoutBuilder builder)
        {
        }
    }
}

#endif
