// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Stride.Graphics;

/// <summary>
///   A layout change a command list recorded on a subresource of a Texture (not a View), or on the whole Texture with
///   <see cref="uint.MaxValue"/>.
/// </summary>
internal readonly record struct RecordedTransition(GraphicsResource Resource, uint Subresource, BarrierLayout LayoutBefore, BarrierLayout LayoutAfter);

/// <summary>
///   Corrects the layouts command lists assume on first touch. A command list takes the layout the last recorded one left,
///   but command lists run in submission order, not recording order. At submission, in order, the layout changes a command
///   list recorded are replayed on the layouts the earlier submissions left, and a barrier is put before it wherever they differ.
/// </summary>
internal static class SubmittedLayoutReplay
{
    /// <summary>
    ///   Records the layouts a command list assumes on its first touch of a Texture, as changes to the same layout, so that
    ///   submission corrects them even when the command list then needs no barrier.
    /// </summary>
    public static void RecordAssumed(List<RecordedTransition> transitions, GraphicsResource resource, in SubresourceLayoutTracker tracker)
    {
        if (tracker.TryGetCommonLayout(out var common))
        {
            // Undefined is met by any layout
            if (common != BarrierLayout.Undefined)
                transitions.Add(new RecordedTransition(resource, uint.MaxValue, common, common));
            return;
        }

        for (uint subresource = 0; subresource < tracker.SubresourceCount; subresource++)
        {
            var layout = tracker.Get(subresource);
            if (layout != BarrierLayout.Undefined)
                transitions.Add(new RecordedTransition(resource, subresource, layout, layout));
        }
    }

    /// <summary>
    ///   Replays the layout changes a command list recorded on the layouts the earlier submissions left, and adds to
    ///   <paramref name="corrections"/> a barrier for each subresource it found in another layout than the one it assumed.
    ///   Called under the queue lock, in submission order.
    /// </summary>
    public static void Replay(List<RecordedTransition> transitions, List<RecordedTransition> corrections)
    {
        foreach (var transition in transitions)
        {
            var resource = transition.Resource;
            var subresourceCount = Math.Max(resource.LayoutTracker.SubresourceCount, 1);
            if (resource.SubmittedLayouts?.Length != subresourceCount)
                resource.SubmittedLayouts = new BarrierLayout?[subresourceCount];

            if (transition.Subresource == uint.MaxValue)
            {
                for (uint subresource = 0; subresource < subresourceCount; subresource++)
                    Replay(resource, subresource, transition, corrections);
            }
            else
            {
                Replay(resource, transition.Subresource, transition, corrections);
            }
        }
    }

    private static void Replay(GraphicsResource resource, uint subresource, in RecordedTransition transition, List<RecordedTransition> corrections)
    {
        ref var submitted = ref resource.SubmittedLayouts[subresource];

        // Unknown before the first submission; Undefined is met by any layout, the command list discards the content
        if (submitted is { } layout && layout != transition.LayoutBefore && transition.LayoutBefore != BarrierLayout.Undefined)
            corrections.Add(new RecordedTransition(resource, subresource, layout, transition.LayoutBefore));

        submitted = transition.LayoutAfter;
    }
}
