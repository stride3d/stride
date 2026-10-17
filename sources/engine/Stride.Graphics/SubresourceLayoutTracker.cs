// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Stride.Graphics;

/// <summary>
///   A barrier a <see cref="SubresourceLayoutTracker"/> transition needs: the subresource it moves
///   (<see cref="uint.MaxValue"/> for the whole resource) and the layout it moves it from.
/// </summary>
internal readonly record struct SubresourceTransition(uint Subresource, BarrierLayout LayoutBefore);

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
    ///   Moves a subresource, or the whole resource with <see cref="uint.MaxValue"/>, to a layout, and adds the
    ///   barriers it needs to <paramref name="transitions"/>: none if it is already there, one whole-resource
    ///   barrier if every subresource shares a layout, otherwise one per subresource that differs.
    /// </summary>
    public void Transition(uint subresource, BarrierLayout newLayout, List<SubresourceTransition> transitions)
    {
        if (!NeedsTransition(subresource, newLayout))
            return;

        if (subresource == uint.MaxValue && mixed && !TryGetCommonLayout(out _))
        {
            for (int i = 0; i < perSubresource!.Length; i++)
            {
                if (perSubresource[i] != newLayout)
                    transitions.Add(new SubresourceTransition((uint) i, perSubresource[i]));
            }
        }
        else
        {
            var layoutBefore = subresource == uint.MaxValue && mixed ? perSubresource![0] : Get(subresource);
            transitions.Add(new SubresourceTransition(subresource, layoutBefore));
        }

        Set(subresource, newLayout);
        CollapseIfUniform();
    }

    /// <summary>
    ///   Moves a range of the subresources of a texture to a layout, and adds one barrier per subresource
    ///   that changes to <paramref name="transitions"/>.
    /// </summary>
    /// <param name="range">The mip-levels and array slices to move.</param>
    /// <param name="mipLevelCount">The number of mip-levels of the texture, to number its subresources.</param>
    /// <param name="newLayout">The layout to move them to.</param>
    /// <param name="transitions">The list to add the barriers to.</param>
    public void Transition(TextureSubresourceRange range, int mipLevelCount, BarrierLayout newLayout, List<SubresourceTransition> transitions)
    {
        for (int arraySlice = range.ArraySliceStart; arraySlice < range.ArraySliceEnd; arraySlice++)
        {
            for (int mipLevel = range.MipStart; mipLevel < range.MipEnd; mipLevel++)
            {
                var subresource = (uint) (arraySlice * mipLevelCount + mipLevel);
                var layoutBefore = Get(subresource);
                if (layoutBefore == newLayout)
                    continue;

                transitions.Add(new SubresourceTransition(subresource, layoutBefore));
                Set(subresource, newLayout);
            }
        }

        CollapseIfUniform();
    }

    // Back to the single layout when every subresource is in the same one again (a mip chain moved them one by one)
    private void CollapseIfUniform()
    {
        if (mixed && TryGetCommonLayout(out var layout))
            Set(uint.MaxValue, layout);
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
