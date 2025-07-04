// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

namespace Stride.Graphics;

public readonly unsafe partial struct MappedResource
{
    internal readonly ComPtr<ID3D12Resource> UploadResource;
    internal readonly int UploadOffset;
}

#endif
