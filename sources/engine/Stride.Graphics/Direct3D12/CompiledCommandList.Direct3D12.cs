// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using System.Collections.Generic;

using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

namespace Stride.Graphics;

public unsafe partial struct CompiledCommandList
{
    /// <summary>
    ///   The Command List used to record commands.
    /// </summary>
    internal CommandList Builder;

    /// <summary>
    ///   The internal native Direct3D 12 Command List for graphics commands.
    /// </summary>
    internal ComPtr<ID3D12GraphicsCommandList> NativeCommandList;
    /// <summary>
    ///   The internal native Direct3D 12 Command Allocator for graphics commands.
    /// </summary>
    internal ComPtr<ID3D12CommandAllocator> NativeCommandAllocator;

    /// <summary>
    ///   A list of native Direct3D 12 Descriptor Heaps for Shader Resource Views.
    /// </summary>
    internal List<ComPtr<ID3D12DescriptorHeap>> SrvHeaps;
    /// <summary>
    ///   A list of native Direct3D 12 Descriptor Heaps for Samplers.
    /// </summary>
    internal List<ComPtr<ID3D12DescriptorHeap>> SamplerHeaps;

    /// <summary>
    ///   A list of Graphics Resources that are currently being used for staging data by this Command List.
    /// </summary>
    internal List<GraphicsResource> StagingResources;
}

#endif
