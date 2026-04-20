// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics;

/// <summary>
///   Tracks the <see cref="BarrierLayout"/> of a resource's subresources for barrier transitions.
///   Uses a single layout for the common case (whole-resource transitions) and lazily allocates
///   a per-subresource array when individual subresources are transitioned independently.
/// </summary>
internal struct SubresourceLayoutTracker
{
    private BarrierLayout singleLayout;
    private BarrierLayout[]? perSubresource;
    private int subresourceCount;

    /// <summary>
    ///   Initializes the tracker with the given initial layout and subresource count.
    /// </summary>
    public void Initialize(BarrierLayout initial, int subresourceCount)
    {
        singleLayout = initial;
        this.subresourceCount = subresourceCount;
        perSubresource = null;
    }

    /// <summary>
    ///   Gets the tracked layout for a subresource.
    ///   Use <see cref="uint.MaxValue"/> for the whole-resource layout.
    /// </summary>
    public readonly BarrierLayout Get(uint subresource)
    {
        if (perSubresource == null || subresource >= (uint)perSubresource.Length)
            return singleLayout;
        return perSubresource[subresource];
    }

    /// <summary>
    ///   Sets the tracked layout for a subresource.
    ///   Use <see cref="uint.MaxValue"/> for a whole-resource transition.
    /// </summary>
    public void Set(uint subresource, BarrierLayout layout)
    {
        if (subresource == uint.MaxValue || subresourceCount <= 1)
        {
            // Whole resource transition
            singleLayout = layout;
            if (perSubresource != null)
                Array.Fill(perSubresource, layout);
        }
        else
        {
            // Per-subresource — lazy allocate
            if (perSubresource == null)
            {
                perSubresource = new BarrierLayout[subresourceCount];
                Array.Fill(perSubresource, singleLayout);
            }
            perSubresource[subresource] = layout;
        }
    }

    /// <summary>
    ///   Determines if a transition is needed for the given subresource to reach the target layout.
    /// </summary>
    public readonly bool NeedsTransition(uint subresource, BarrierLayout target)
    {
        if (subresource != uint.MaxValue || perSubresource == null)
            return Get(subresource) != target;

        // Whole-resource check with per-subresource tracking:
        // any subresource that differs means a transition is needed
        for (int i = 0; i < perSubresource.Length; i++)
        {
            if (perSubresource[i] != target)
                return true;
        }
        return false;
    }

    /// <summary>
    ///   Whether per-subresource tracking is active (some subresources may have different layouts).
    /// </summary>
    internal readonly bool HasPerSubresourceTracking => perSubresource != null;

    /// <summary>
    ///   The number of subresources tracked.
    /// </summary>
    internal readonly int SubresourceCount => subresourceCount;

    /// <summary>
    ///   Gets the per-subresource array. Only valid when <see cref="HasPerSubresourceTracking"/> is true.
    /// </summary>
    internal readonly ReadOnlySpan<BarrierLayout> PerSubresourceLayouts => perSubresource;
}
