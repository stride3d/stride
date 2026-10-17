// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics;

/// <summary>
///   Tracks the <see cref="BarrierLayout"/> of a resource's subresources for barrier transitions.
///   Uses a single layout for the common case (whole-resource transitions) and a per-subresource array
///   when individual subresources are transitioned independently. A whole-resource transition goes back
///   to the single layout, and keeps the array for the next per-subresource transition.
/// </summary>
internal struct SubresourceLayoutTracker
{
    private BarrierLayout singleLayout;
    // Only valid while `mixed` is set; kept allocated otherwise
    private BarrierLayout[]? perSubresource;
    private int subresourceCount;
    private bool mixed;

    /// <summary>
    ///   Initializes the tracker with the given initial layout and subresource count.
    /// </summary>
    public void Initialize(BarrierLayout initial, int subresourceCount)
    {
        singleLayout = initial;
        this.subresourceCount = subresourceCount;
        perSubresource = null;
        mixed = false;
    }

    /// <summary>
    ///   Gets the tracked layout for a subresource.
    ///   Use <see cref="uint.MaxValue"/> for the whole-resource layout.
    /// </summary>
    public readonly BarrierLayout Get(uint subresource)
    {
        if (!mixed || subresource >= (uint)perSubresource!.Length)
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
            mixed = false;
            return;
        }

        if (!mixed)
        {
            perSubresource ??= new BarrierLayout[subresourceCount];
            Array.Fill(perSubresource, singleLayout);
            mixed = true;
        }
        perSubresource![subresource] = layout;
    }

    /// <summary>
    ///   Determines if a transition is needed for the given subresource to reach the target layout.
    /// </summary>
    public readonly bool NeedsTransition(uint subresource, BarrierLayout target)
    {
        if (subresource != uint.MaxValue || !mixed)
            return Get(subresource) != target;

        // Whole-resource check with per-subresource tracking:
        // any subresource that differs means a transition is needed
        for (int i = 0; i < perSubresource!.Length; i++)
        {
            if (perSubresource[i] != target)
                return true;
        }
        return false;
    }

    /// <summary>
    ///   Gets the layout every subresource is in, if they all share one.
    /// </summary>
    /// <returns><see langword="true"/> if all subresources have the same layout; otherwise, <see langword="false"/>.</returns>
    public readonly bool TryGetCommonLayout(out BarrierLayout layout)
    {
        layout = mixed ? perSubresource![0] : singleLayout;
        if (!mixed)
            return true;

        for (int i = 1; i < perSubresource!.Length; i++)
        {
            if (perSubresource[i] != layout)
                return false;
        }
        return true;
    }

    /// <summary>
    ///   Whether per-subresource tracking is active (some subresources may have different layouts).
    /// </summary>
    internal readonly bool HasPerSubresourceTracking => mixed;

    /// <summary>
    ///   The number of subresources tracked.
    /// </summary>
    internal readonly int SubresourceCount => subresourceCount;

    /// <summary>
    ///   Gets the per-subresource array. Only valid when <see cref="HasPerSubresourceTracking"/> is true.
    /// </summary>
    internal readonly ReadOnlySpan<BarrierLayout> PerSubresourceLayouts => mixed ? perSubresource : default;

    /// <summary>
    ///   Returns an independent copy (a plain struct copy aliases the <see cref="perSubresource"/> array).
    ///   A copy of a tracker with a single layout does not allocate.
    /// </summary>
    public readonly SubresourceLayoutTracker Clone()
    {
        var clone = this;
        clone.perSubresource = mixed ? (BarrierLayout[])perSubresource!.Clone() : null;
        return clone;
    }
}
