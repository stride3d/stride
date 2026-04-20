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

    /// <summary>
    ///   Derives the Vulkan access flags from a <see cref="BarrierLayout"/>.
    /// </summary>
    internal static VkAccessFlags ToVkAccessFlags(BarrierLayout layout) => layout switch
    {
        BarrierLayout.RenderTarget => VkAccessFlags.ColorAttachmentWrite,
        BarrierLayout.DepthStencilWrite => VkAccessFlags.DepthStencilAttachmentWrite,
        BarrierLayout.DepthStencilRead => VkAccessFlags.DepthStencilAttachmentRead,
        BarrierLayout.ShaderResource => VkAccessFlags.ShaderRead,
        BarrierLayout.UnorderedAccess => VkAccessFlags.ShaderRead | VkAccessFlags.ShaderWrite,
        BarrierLayout.CopySource => VkAccessFlags.TransferRead,
        BarrierLayout.CopyDest => VkAccessFlags.TransferWrite,
        BarrierLayout.Present => VkAccessFlags.MemoryRead,
        BarrierLayout.ResolveSource => VkAccessFlags.TransferRead,
        BarrierLayout.ResolveDest => VkAccessFlags.TransferWrite,
        _ => VkAccessFlags.None,
    };

    /// <summary>
    ///   Derives the Vulkan pipeline stage flags from a <see cref="BarrierLayout"/>.
    /// </summary>
    internal static VkPipelineStageFlags ToVkPipelineStageFlags(BarrierLayout layout) => layout switch
    {
        BarrierLayout.RenderTarget => VkPipelineStageFlags.ColorAttachmentOutput,
        BarrierLayout.DepthStencilWrite => VkPipelineStageFlags.ColorAttachmentOutput | VkPipelineStageFlags.EarlyFragmentTests | VkPipelineStageFlags.LateFragmentTests,
        BarrierLayout.DepthStencilRead => VkPipelineStageFlags.EarlyFragmentTests | VkPipelineStageFlags.LateFragmentTests,
        BarrierLayout.ShaderResource => VkPipelineStageFlags.FragmentShader | VkPipelineStageFlags.ComputeShader,
        BarrierLayout.UnorderedAccess => VkPipelineStageFlags.ComputeShader,
        BarrierLayout.CopySource => VkPipelineStageFlags.Transfer,
        BarrierLayout.CopyDest => VkPipelineStageFlags.Transfer,
        BarrierLayout.Present => VkPipelineStageFlags.BottomOfPipe,
        BarrierLayout.ResolveSource => VkPipelineStageFlags.Transfer,
        BarrierLayout.ResolveDest => VkPipelineStageFlags.Transfer,
        _ => VkPipelineStageFlags.TopOfPipe,
    };
}

#endif
