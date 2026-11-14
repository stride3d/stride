// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;

namespace Stride.Assets.Editor.EditorGame;

/// <summary>
/// When the game draws the views of a scene editor other than the active one: together, at a fixed rate lower than the
/// game's, which <see cref="FramesPerSecond"/> sets.
/// </summary>
/// <remarks>
/// The active view is drawn at every frame of the game. A view that was never drawn, whose size or camera changed, is drawn
/// at the next frame whatever the schedule (<see cref="EditorGameMultiViewService"/>).
/// </remarks>
internal sealed class InactiveViewSchedule
{
    /// <summary>
    /// How many times per second the views other than the active one are drawn.
    /// </summary>
    public const double FramesPerSecond = 20;

    /// <summary>
    /// The time between two draws of the views other than the active one.
    /// </summary>
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(1 / FramesPerSecond);

    // A frame of the game that comes a little early still draws: the frames of a game at 60 fps don't fall exactly on the
    // interval
    private static readonly TimeSpan Tolerance = TimeSpan.FromMilliseconds(2);

    private TimeSpan? next;

    /// <summary>
    /// Gets whether the views other than the active one are drawn at the frame of the game at <paramref name="time"/>, and
    /// if so schedules the next draw.
    /// </summary>
    /// <param name="time">The total time of the game at this frame.</param>
    public bool IsDue(TimeSpan time)
    {
        if (next is { } due && time + Tolerance < due)
            return false;

        // Keep the rhythm, unless the game fell behind (a frame took longer than the interval)
        next = next is { } previous && time - previous < Interval ? previous + Interval : time + Interval;
        return true;
    }
}
