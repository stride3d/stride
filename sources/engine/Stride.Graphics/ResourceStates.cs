// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Xenko.Graphics
{
    [Flags]
    public enum GraphicsResourceState
    {
        Common = 0,
        Present = 0,
        VertexAndConstantBuffer = 1,
        IndexBuffer = 2,
        RenderTarget = 4,
        UnorderedAccess = 8,
        DepthWrite = 16,
        DepthRead = 32,
        NonPixelShaderResource = 64,
        PixelShaderResource = 128,
        StreamOut = 256,
        IndirectArgument = 512,
        Predication = 512,
        CopyDestination = 1024,
        CopySource = 2048,
        GenericRead = 2755,
        ResolveDestination = 4096,
        ResolveSource = 8192,
    }
}
