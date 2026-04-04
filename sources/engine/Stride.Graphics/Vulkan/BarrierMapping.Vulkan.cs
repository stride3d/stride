// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_VULKAN

using Vortice.Vulkan;

namespace Stride.Graphics;

/// <summary>
///   Provides mapping between cross-platform barrier enums and Vulkan-specific types.
/// </summary>
internal static class BarrierMapping
{
    /// <summary>
    ///   Converts a <see cref="BarrierLayout"/> to a <see cref="VkImageLayout"/>.
    /// </summary>
    internal static VkImageLayout ToVkImageLayout(BarrierLayout layout) => layout switch
    {
        BarrierLayout.Undefined => VkImageLayout.Undefined,
        BarrierLayout.Common => VkImageLayout.General,
        BarrierLayout.RenderTarget => VkImageLayout.ColorAttachmentOptimal,
        BarrierLayout.DepthStencilWrite => VkImageLayout.DepthStencilAttachmentOptimal,
        BarrierLayout.DepthStencilRead => VkImageLayout.DepthStencilReadOnlyOptimal,
        BarrierLayout.ShaderResource => VkImageLayout.ShaderReadOnlyOptimal,
        BarrierLayout.UnorderedAccess => VkImageLayout.General,
        BarrierLayout.CopySource => VkImageLayout.TransferSrcOptimal,
        BarrierLayout.CopyDest => VkImageLayout.TransferDstOptimal,
        BarrierLayout.Present => VkImageLayout.PresentSrcKHR,
        BarrierLayout.ResolveSource => VkImageLayout.TransferSrcOptimal,
        BarrierLayout.ResolveDest => VkImageLayout.TransferDstOptimal,
        _ => VkImageLayout.General,
    };

    /// <summary>
    ///   Converts a <see cref="VkImageLayout"/> to a <see cref="BarrierLayout"/>.
    /// </summary>
    internal static BarrierLayout ToBarrierLayout(VkImageLayout layout) => layout switch
    {
        VkImageLayout.Undefined => BarrierLayout.Undefined,
        VkImageLayout.General => BarrierLayout.Common,
        VkImageLayout.ColorAttachmentOptimal => BarrierLayout.RenderTarget,
        VkImageLayout.DepthStencilAttachmentOptimal => BarrierLayout.DepthStencilWrite,
        VkImageLayout.DepthStencilReadOnlyOptimal => BarrierLayout.DepthStencilRead,
        VkImageLayout.ShaderReadOnlyOptimal => BarrierLayout.ShaderResource,
        VkImageLayout.TransferSrcOptimal => BarrierLayout.CopySource,
        VkImageLayout.TransferDstOptimal => BarrierLayout.CopyDest,
        VkImageLayout.PresentSrcKHR => BarrierLayout.Present,
        _ => BarrierLayout.Common,
    };
}

#endif
