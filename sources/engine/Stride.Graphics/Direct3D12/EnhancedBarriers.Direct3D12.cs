// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// Manual definitions for D3D12 Enhanced Barriers structs not yet in Silk.NET 2.22.0.
// These match the native D3D12 ABI and can be removed when Silk.NET is updated.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using System;
using System.Runtime.InteropServices;

using Silk.NET.Direct3D12;

using NativeBarrierLayout = Silk.NET.Direct3D12.BarrierLayout;

namespace Stride.Graphics;

/// <summary>
///   D3D12 Enhanced Barriers synchronization flags.
/// </summary>
[Flags]
internal enum D3D12BarrierSync : uint
{
    None = 0,
    All = 0x1,
    Draw = 0x2,
    InputAssembler = 0x4,
    VertexShading = 0x8,
    PixelShading = 0x10,
    DepthStencil = 0x20,
    RenderTarget = 0x40,
    ComputeShading = 0x80,
    Raytracing = 0x100,
    Copy = 0x200,
    Resolve = 0x400,
    ExecuteIndirect = 0x800,
    AllShading = 0x1000,
    NonPixelShading = 0x2000,
    BuildRaytracingAccelerationStructure = 0x8000,
    CopyRaytracingAccelerationStructure = 0x10000,
}

/// <summary>
///   D3D12 Enhanced Barriers access flags.
/// </summary>
[Flags]
internal enum D3D12BarrierAccess : uint
{
    Common = 0,
    VertexBuffer = 0x1,
    ConstantBuffer = 0x2,
    IndexBuffer = 0x4,
    RenderTarget = 0x8,
    UnorderedAccess = 0x10,
    DepthStencilWrite = 0x20,
    DepthStencilRead = 0x40,
    ShaderResource = 0x80,
    StreamOutput = 0x100,
    IndirectArgument = 0x200,
    CopyDest = 0x400,
    CopySource = 0x800,
    ResolveDest = 0x1000,
    ResolveSource = 0x2000,
    NoAccess = 0x80000000,
}

/// <summary>
///   D3D12 Enhanced Barriers barrier type.
/// </summary>
internal enum D3D12BarrierType : uint
{
    Global = 0,
    Texture = 1,
    Buffer = 2,
}

/// <summary>
///   D3D12 texture barrier for Enhanced Barriers.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct D3D12TextureBarrier
{
    public D3D12BarrierSync SyncBefore;
    public D3D12BarrierSync SyncAfter;
    public D3D12BarrierAccess AccessBefore;
    public D3D12BarrierAccess AccessAfter;
    public NativeBarrierLayout LayoutBefore;
    public NativeBarrierLayout LayoutAfter;
    public ID3D12Resource* PResource;
    public D3D12SubresourceRange Subresources;
    public uint Flags; // D3D12_TEXTURE_BARRIER_FLAGS
}

/// <summary>
///   D3D12 buffer barrier for Enhanced Barriers.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct D3D12BufferBarrier
{
    public D3D12BarrierSync SyncBefore;
    public D3D12BarrierSync SyncAfter;
    public D3D12BarrierAccess AccessBefore;
    public D3D12BarrierAccess AccessAfter;
    public ID3D12Resource* PResource;
    public ulong Offset;
    public ulong Size;
}

/// <summary>
///   D3D12 subresource range for Enhanced Barriers.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct D3D12SubresourceRange
{
    public uint IndexOrFirstMipLevel;
    public uint NumMipLevels;
    public uint FirstArraySlice;
    public uint NumArraySlices;
    public uint FirstPlane;
    public uint NumPlanes;

    public static readonly D3D12SubresourceRange All = new()
    {
        IndexOrFirstMipLevel = 0xFFFFFFFF,
        NumMipLevels = 0,
        FirstArraySlice = 0,
        NumArraySlices = 0,
        FirstPlane = 0,
        NumPlanes = 0,
    };
}

/// <summary>
///   D3D12 barrier group for Enhanced Barriers — matches the native D3D12_BARRIER_GROUP layout.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct D3D12BarrierGroup
{
    public D3D12BarrierType Type;
    public uint NumBarriers;
    // Union: pointer to TextureBarrier[], BufferBarrier[], or GlobalBarrier[]
    public void* PBarriers;
}

#endif
