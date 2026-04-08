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

    /// <summary>
    ///   Converts a <see cref="BarrierLayout"/> to a D3D12 Enhanced <see cref="Silk.NET.Direct3D12.BarrierLayout"/>.
    /// </summary>
    internal static Silk.NET.Direct3D12.BarrierLayout ToEnhancedLayout(BarrierLayout layout) => layout switch
    {
        BarrierLayout.Undefined => Silk.NET.Direct3D12.BarrierLayout.BarrierLayoutUndefined,
        BarrierLayout.Common => Silk.NET.Direct3D12.BarrierLayout.BarrierLayoutCommon,
        BarrierLayout.RenderTarget => Silk.NET.Direct3D12.BarrierLayout.BarrierLayoutRenderTarget,
        BarrierLayout.DepthStencilWrite => Silk.NET.Direct3D12.BarrierLayout.BarrierLayoutDepthStencilWrite,
        BarrierLayout.DepthStencilRead => Silk.NET.Direct3D12.BarrierLayout.BarrierLayoutDepthStencilRead,
        BarrierLayout.ShaderResource => Silk.NET.Direct3D12.BarrierLayout.BarrierLayoutShaderResource,
        BarrierLayout.UnorderedAccess => Silk.NET.Direct3D12.BarrierLayout.BarrierLayoutUnorderedAccess,
        BarrierLayout.CopySource => Silk.NET.Direct3D12.BarrierLayout.BarrierLayoutCopySource,
        BarrierLayout.CopyDest => Silk.NET.Direct3D12.BarrierLayout.BarrierLayoutCopyDest,
        BarrierLayout.Present => Silk.NET.Direct3D12.BarrierLayout.BarrierLayoutPresent,
        BarrierLayout.ResolveSource => Silk.NET.Direct3D12.BarrierLayout.BarrierLayoutResolveSource,
        BarrierLayout.ResolveDest => Silk.NET.Direct3D12.BarrierLayout.BarrierLayoutResolveDest,
        _ => Silk.NET.Direct3D12.BarrierLayout.BarrierLayoutCommon,
    };

    /// <summary>
    ///   Derives the D3D12 Enhanced Barriers access flags from a <see cref="BarrierLayout"/>.
    /// </summary>
    internal static D3D12BarrierAccess ToEnhancedAccess(BarrierLayout layout) => layout switch
    {
        BarrierLayout.RenderTarget => D3D12BarrierAccess.RenderTarget,
        BarrierLayout.DepthStencilWrite => D3D12BarrierAccess.DepthStencilWrite,
        BarrierLayout.DepthStencilRead => D3D12BarrierAccess.DepthStencilRead,
        BarrierLayout.ShaderResource => D3D12BarrierAccess.ShaderResource,
        BarrierLayout.UnorderedAccess => D3D12BarrierAccess.UnorderedAccess,
        BarrierLayout.CopySource => D3D12BarrierAccess.CopySource,
        BarrierLayout.CopyDest => D3D12BarrierAccess.CopyDest,
        BarrierLayout.ResolveSource => D3D12BarrierAccess.ResolveSource,
        BarrierLayout.ResolveDest => D3D12BarrierAccess.ResolveDest,
        _ => D3D12BarrierAccess.Common,
    };

    /// <summary>
    ///   Derives the D3D12 Enhanced Barriers sync flags from a <see cref="BarrierLayout"/>.
    /// </summary>
    internal static D3D12BarrierSync ToEnhancedSync(BarrierLayout layout) => layout switch
    {
        BarrierLayout.RenderTarget => D3D12BarrierSync.RenderTarget,
        BarrierLayout.DepthStencilWrite => D3D12BarrierSync.DepthStencil,
        BarrierLayout.DepthStencilRead => D3D12BarrierSync.DepthStencil,
        BarrierLayout.ShaderResource => D3D12BarrierSync.AllShading,
        BarrierLayout.UnorderedAccess => D3D12BarrierSync.AllShading,
        BarrierLayout.CopySource => D3D12BarrierSync.Copy,
        BarrierLayout.CopyDest => D3D12BarrierSync.Copy,
        BarrierLayout.ResolveSource => D3D12BarrierSync.Resolve,
        BarrierLayout.ResolveDest => D3D12BarrierSync.Resolve,
        _ => D3D12BarrierSync.All,
    };
}

#endif
