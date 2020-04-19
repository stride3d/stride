// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Rendering;
using Stride.Shaders;

namespace Stride.Graphics
{
    // D3D11 version
    /// <summary>
    /// Defines a list of descriptor layout. This is used to allocate a <see cref="DescriptorSet"/>.
    /// </summary>
    public partial class DescriptorSetLayout : GraphicsResourceBase
    {
        public static DescriptorSetLayout New(GraphicsDevice device, DescriptorSetLayoutBuilder builder)
        {
            return new DescriptorSetLayout(device, builder);
        }

#if STRIDE_GRAPHICS_API_DIRECT3D11 || STRIDE_GRAPHICS_API_OPENGL || (STRIDE_GRAPHICS_API_VULKAN && STRIDE_GRAPHICS_NO_DESCRIPTOR_COPIES)
        internal readonly int ElementCount;
        internal readonly DescriptorSetLayoutBuilder.Entry[] Entries;

        private DescriptorSetLayout(GraphicsDevice device, DescriptorSetLayoutBuilder builder)
        {
            ElementCount = builder.ElementCount;
            Entries = builder.Entries.ToArray();
        }
#endif
    }
}
