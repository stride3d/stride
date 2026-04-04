// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using Silk.NET.Direct3D12;

namespace Stride.Graphics;

/// <summary>
///   Provides mapping between cross-platform barrier enums and D3D12-specific types.
/// </summary>
internal static class BarrierMapping
{
    /// <summary>
    ///   Converts a <see cref="BarrierLayout"/> to a legacy D3D12 <see cref="ResourceStates"/>.
    /// </summary>
    internal static ResourceStates ToResourceStates(BarrierLayout layout) => layout switch
    {
        BarrierLayout.Undefined => ResourceStates.Common,
        BarrierLayout.Common => ResourceStates.Common,
        BarrierLayout.RenderTarget => ResourceStates.RenderTarget,
        BarrierLayout.DepthStencilWrite => ResourceStates.DepthWrite,
        BarrierLayout.DepthStencilRead => ResourceStates.DepthRead,
        BarrierLayout.ShaderResource => ResourceStates.PixelShaderResource | ResourceStates.NonPixelShaderResource,
        BarrierLayout.UnorderedAccess => ResourceStates.UnorderedAccess,
        BarrierLayout.CopySource => ResourceStates.CopySource,
        BarrierLayout.CopyDest => ResourceStates.CopyDest,
        BarrierLayout.Present => ResourceStates.Common, // Present == Common in D3D12
        BarrierLayout.ResolveSource => ResourceStates.ResolveSource,
        BarrierLayout.ResolveDest => ResourceStates.ResolveDest,
        _ => ResourceStates.Common,
    };

    /// <summary>
    ///   Converts a legacy D3D12 <see cref="ResourceStates"/> to a <see cref="BarrierLayout"/>.
    /// </summary>
    internal static BarrierLayout ToBarrierLayout(ResourceStates state) => state switch
    {
        ResourceStates.RenderTarget => BarrierLayout.RenderTarget,
        ResourceStates.DepthWrite => BarrierLayout.DepthStencilWrite,
        ResourceStates.DepthRead => BarrierLayout.DepthStencilRead,
        ResourceStates.UnorderedAccess => BarrierLayout.UnorderedAccess,
        ResourceStates.CopySource => BarrierLayout.CopySource,
        ResourceStates.CopyDest => BarrierLayout.CopyDest,
        ResourceStates.ResolveSource => BarrierLayout.ResolveSource,
        ResourceStates.ResolveDest => BarrierLayout.ResolveDest,
        ResourceStates.PixelShaderResource | ResourceStates.NonPixelShaderResource => BarrierLayout.ShaderResource,
        ResourceStates.PixelShaderResource => BarrierLayout.ShaderResource,
        ResourceStates.NonPixelShaderResource => BarrierLayout.ShaderResource,
        _ => BarrierLayout.Common,
    };
}

#endif
