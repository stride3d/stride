// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_DIRECT3D12
using System.Collections.Generic;
using SharpDX.Direct3D12;

namespace Stride.Graphics
{
    public partial struct CompiledCommandList
    {
        internal CommandList Builder;
        internal GraphicsCommandList NativeCommandList;
        internal CommandAllocator NativeCommandAllocator;
        internal List<DescriptorHeap> SrvHeaps;
        internal List<DescriptorHeap> SamplerHeaps;
        internal List<GraphicsResource> StagingResources;
    }
}
#endif
