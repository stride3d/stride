// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Processors;

namespace Stride.Assets.Editor.EditorGame;

/// <summary>
/// The camera of a view the game is about to draw, as the gizmos and the grid need it to fit that view.
/// </summary>
/// <remarks>
/// A game with one view has one: the editor camera, with the height of the back buffer. A game with several views has one
/// per view; the active view's is the editor camera.
/// </remarks>
public readonly struct EditorGameViewCamera
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EditorGameViewCamera"/> structure.
    /// </summary>
    /// <param name="component">The camera of the view; its view matrix and projection are those of the view.</param>
    /// <param name="height">The height of the view, in pixels.</param>
    /// <param name="isActive">Whether the view is the active one, the view the pointer and the editor camera work in.</param>
    public EditorGameViewCamera(CameraComponent component, int height, bool isActive)
    {
        Component = component ?? throw new ArgumentNullException(nameof(component));
        Height = Math.Max(1, height);
        IsActive = isActive;
    }

    /// <summary>
    /// Gets the camera of the view.
    /// </summary>
    public CameraComponent Component { get; }

    /// <summary>
    /// Gets the view matrix of the view.
    /// </summary>
    public Matrix ViewMatrix => Component.ViewMatrix;

    /// <summary>
    /// Gets whether the projection of the view is orthographic.
    /// </summary>
    public bool IsOrthographic => Component.Projection == CameraProjectionMode.Orthographic;

    /// <summary>
    /// Gets the orthographic size of the view.
    /// </summary>
    public float OrthographicSize => Component.OrthographicSize;

    /// <summary>
    /// Gets the vertical field of view of the view, in degrees.
    /// </summary>
    public float VerticalFieldOfView => Component.VerticalFieldOfView;

    /// <summary>
    /// Gets the height of the view, in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets whether the view is the active one: the view the pointer and the editor camera work in.
    /// </summary>
    public bool IsActive { get; }
}

/// <summary>
/// A game service or a scene view tool that draws something fitted to the camera of the view, such as gizmos sized for it,
/// the grid on its plane or the orientation cube.
/// </summary>
/// <remarks>
/// A game with several views draws each view other than the active one after the frame: just before, it calls
/// <see cref="PrepareView"/> with that view's camera, and once they are drawn, with the active view's camera again, so
/// that what the pointer works with matches the active view. A game with one view never calls it.
/// </remarks>
public interface IEditorGameViewDependent
{
    /// <summary>
    /// Fits what it draws to the camera of <paramref name="view"/> (game thread, while the game draws).
    /// </summary>
    /// <param name="view">The camera of the view the game draws next.</param>
    void PrepareView(EditorGameViewCamera view);
}
