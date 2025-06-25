// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics;

[Flags]
public enum GraphicsResourceState
{
    // From D3D12_RESOURCE_STATES in d3d12.h

    Common = 0,

    Present = 0,

    VertexAndConstantBuffer = 1,

    IndexBuffer = 2,

    RenderTarget = 4,

    UnorderedAccess = 8,

    DepthWrite = 0x10,

    DepthRead = 0x20,

    NonPixelShaderResource = 0x40,

    PixelShaderResource = 0x80,

    AllShaderResource = NonPixelShaderResource | PixelShaderResource, // 0x40 | 0x80

    StreamOut = 0x100,

    IndirectArgument = 0x200,

    Predication = 0x200,

    CopyDestination = 0x400,

    CopySource = 0x800,

    GenericRead = VertexAndConstantBuffer | IndexBuffer | NonPixelShaderResource | PixelShaderResource | IndirectArgument | CopySource,  // 0x1 | 0x2 | 0x40 | 0x80 | 0x200 | 0x800

    ResolveDestination = 0x1000,

    ResolveSource = 0x2000
}
