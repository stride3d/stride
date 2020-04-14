// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_GRAPHICS_API_VULKAN
using System.Collections.Generic;
using SharpVulkan;

namespace Xenko.Graphics
{
    public partial struct CompiledCommandList
    {
        internal CommandList Builder;
        internal CommandBuffer NativeCommandBuffer;
        internal List<SharpVulkan.DescriptorPool> DescriptorPools;
        internal List<Texture> StagingResources;
    }
}
#endif
