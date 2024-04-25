// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System.Collections.Generic;
using VK = Silk.NET.Vulkan;

namespace Stride.Graphics
{
    public partial struct CompiledCommandList
    {
        internal CommandList Builder;
        internal VK.CommandBuffer NativeCommandBuffer;
        internal List<VK.DescriptorPool> DescriptorPools;
        internal List<Texture> StagingResources;
    }
}
#endif
