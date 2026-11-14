// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Editor.EditorGame.ViewModels;

namespace Stride.Assets.Editor.AssetEditors.GameEditor.Services;

/// <summary>
/// The camera a view of the scene starts with: a perspective view, or an orthographic view along an axis.
/// </summary>
[Experimental("STREXP009")]
public enum EditorViewCamera
{
    /// <summary>A perspective view, where the primary view is.</summary>
    Perspective,

    /// <summary>An orthographic view looking down.</summary>
    Top,

    /// <summary>An orthographic view looking up.</summary>
    Bottom,

    /// <summary>An orthographic view looking along -Z.</summary>
    Front,

    /// <summary>An orthographic view looking along +Z.</summary>
    Back,

    /// <summary>An orthographic view looking along +X.</summary>
    Left,

    /// <summary>An orthographic view looking along -X.</summary>
    Right,
}

/// <summary>
/// The camera of a view, as the view models see it.
/// </summary>
/// <param name="ViewId">The view.</param>
/// <param name="IsOrthographic">Whether the camera of the view is orthographic.</param>
/// <param name="OrthographicSize">The orthographic size of the camera of the view.</param>
[Experimental("STREXP009")]
public readonly record struct EditorGameViewCameraInfo(int ViewId, bool IsOrthographic, float OrthographicSize);

/// <summary>
/// Where the camera of a view is, as the settings of a scene save it.
/// </summary>
/// <param name="Position">The position of the camera.</param>
/// <param name="PitchYaw">The pitch and the yaw of the camera.</param>
/// <param name="IsOrthographic">Whether the camera is orthographic.</param>
/// <param name="OrthographicSize">The orthographic size of the camera.</param>
[Experimental("STREXP009")]
public readonly record struct EditorGameViewCameraState(Vector3 Position, Vector2 PitchYaw, bool IsOrthographic, float OrthographicSize)
{
    /// <summary>
    /// Gets the point the camera orbits around, or <c>null</c> to keep the one of the editor camera.
    /// </summary>
    public Vector3? Target { get; init; }
}

/// <summary>
/// The several views of the game of a scene or prefab editor, for its view models. The game has one camera
/// per view; the editor camera (<see cref="IEditorGameCameraViewModelService"/>) is the camera of the active view.
/// </summary>
[Experimental("STREXP009")]
public interface IEditorGameMultiViewViewModelService : IEditorGameViewModelService
{
    /// <summary>
    /// Gets whether the viewport of the game can show several views, or only one.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Raised on the game thread when another view becomes active: the editor camera is now its camera.
    /// </summary>
    event EventHandler<EditorGameViewCameraInfo>? ActiveViewChanged;

    /// <summary>
    /// Gets the camera of the primary view while another view is active (any thread), or <c>null</c> while the primary
    /// view is active: the editor camera is then the camera of the primary view. The settings of the scene save it.
    /// </summary>
    EditorGameViewCameraState? PrimaryViewCamera { get; }

    /// <summary>
    /// Gives a view the camera <paramref name="camera"/> (UI thread; the game applies it at its next frame).
    /// </summary>
    /// <param name="viewId">The view, active or not.</param>
    /// <param name="camera">The camera to start from.</param>
    void SetViewCamera(int viewId, EditorViewCamera camera);

    /// <summary>
    /// Gets the camera of each view the game knows (any thread): the active view's as it is now, the others' as the user
    /// left them. A view whose camera the game has not set yet is missing.
    /// </summary>
    IReadOnlyDictionary<int, EditorGameViewCameraState> GetViewCameras();

    /// <summary>
    /// Moves the camera of a view where it was when the settings of the scene saved it (UI thread; the game applies it at
    /// its next frame).
    /// </summary>
    /// <param name="viewId">The view, active or not.</param>
    /// <param name="state">The camera of the view.</param>
    void SetViewCamera(int viewId, EditorGameViewCameraState state);

    /// <summary>
    /// Forgets the camera of a view that was closed (UI thread).
    /// </summary>
    void RemoveView(int viewId);
}
