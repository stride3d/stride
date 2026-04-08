// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   Describes a resource barrier transition for GPU synchronization.
///   This is a cross-platform abstraction over D3D12 Enhanced Barriers and Vulkan pipeline barriers.
/// </summary>
public struct ResourceBarrierDescription
{
    /// <summary>
    ///   The resource to transition.
    /// </summary>
    public GraphicsResource Resource;

    /// <summary>
    ///   GPU pipeline stages that must complete before the barrier takes effect.
    /// </summary>
    public BarrierSync SyncBefore;

    /// <summary>
    ///   GPU pipeline stages that are blocked until the barrier completes.
    /// </summary>
    public BarrierSync SyncAfter;

    /// <summary>
    ///   The type of access performed before the barrier.
    /// </summary>
    public BarrierAccess AccessBefore;

    /// <summary>
    ///   The type of access performed after the barrier.
    /// </summary>
    public BarrierAccess AccessAfter;

    /// <summary>
    ///   The resource layout before the barrier.
    /// </summary>
    public BarrierLayout LayoutBefore;

    /// <summary>
    ///   The resource layout after the barrier.
    /// </summary>
    public BarrierLayout LayoutAfter;

    /// <summary>
    ///   The subresource index, or <see cref="uint.MaxValue"/> for all subresources.
    /// </summary>
    public uint Subresource;

    /// <summary>
    ///   Creates a barrier for all subresources of a resource.
    /// </summary>
    public ResourceBarrierDescription(
        GraphicsResource resource,
        BarrierLayout layoutBefore,
        BarrierLayout layoutAfter,
        BarrierAccess accessBefore = BarrierAccess.None,
        BarrierAccess accessAfter = BarrierAccess.None,
        BarrierSync syncBefore = BarrierSync.All,
        BarrierSync syncAfter = BarrierSync.All)
    {
        Resource = resource;
        LayoutBefore = layoutBefore;
        LayoutAfter = layoutAfter;
        AccessBefore = accessBefore;
        AccessAfter = accessAfter;
        SyncBefore = syncBefore;
        SyncAfter = syncAfter;
        Subresource = uint.MaxValue;
    }
}
