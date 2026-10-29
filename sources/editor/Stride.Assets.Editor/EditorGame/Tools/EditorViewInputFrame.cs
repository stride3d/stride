// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Input;

namespace Stride.Assets.Editor.EditorGame.Tools;

/// <summary>
/// The input of a view during one frame, as the tools of the view see it.
/// </summary>
internal sealed record EditorViewInputFrame
{
    /// <summary>
    /// Gets the buttons pressed during the frame.
    /// </summary>
    public IReadOnlySet<MouseButton> PressedButtons { get; init; } = new HashSet<MouseButton>();

    /// <summary>
    /// Gets the buttons released during the frame.
    /// </summary>
    public IReadOnlySet<MouseButton> ReleasedButtons { get; init; } = new HashSet<MouseButton>();

    /// <summary>
    /// Gets the buttons down at the end of the frame.
    /// </summary>
    public IReadOnlySet<MouseButton> DownButtons { get; init; } = new HashSet<MouseButton>();

    /// <summary>
    /// Gets the keys pressed during the frame.
    /// </summary>
    public IReadOnlySet<Keys> PressedKeys { get; init; } = new HashSet<Keys>();

    /// <summary>
    /// Gets the move of the mouse during the frame, in pixels.
    /// </summary>
    public Vector2 MouseDelta { get; init; }

    /// <summary>
    /// Gets whether no other service (the camera during a drag) has the mouse.
    /// </summary>
    public bool IsMouseAvailable { get; init; } = true;
}
