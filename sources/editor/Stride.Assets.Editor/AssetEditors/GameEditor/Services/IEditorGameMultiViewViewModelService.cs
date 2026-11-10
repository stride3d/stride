// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using Stride.Core.Mathematics;
using Stride.Editor.EditorGame.ViewModels;

namespace Stride.Assets.Editor.AssetEditors.GameEditor.Services;

/// <summary>
/// The camera a view of the scene starts with: a perspective view, or an orthographic view along an axis.
/// </summary>
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
public readonly record struct EditorGameViewCameraInfo(int ViewId, bool IsOrthographic, float OrthographicSize);

/// <summary>
/// Where the camera of a view is, as the settings of a scene save it.
/// </summary>
/// <param name="Position">The position of the camera.</param>
/// <param name="PitchYaw">The pitch and the yaw of the camera.</param>
/// <param name="IsOrthographic">Whether the camera is orthographic.</param>
/// <param name="OrthographicSize">The orthographic size of the camera.</param>
public readonly record struct EditorGameViewCameraState(Vector3 Position, Vector2 PitchYaw, bool IsOrthographic, float OrthographicSize);

/// <summary>
/// The several views of the game of a scene or prefab editor, for its view models (prototype). The game has one camera
/// per view; the editor camera (<see cref="IEditorGameCameraViewModelService"/>) is the camera of the active view.
/// </summary>
public interface IEditorGameMultiViewViewModelService : IEditorGameViewModelService
{
    /// <summary>
    /// Gets whether the viewport of the game can show several views (the Avalonia editor), or only one (WPF).
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
    /// Forgets the camera of a view that was closed (UI thread).
    /// </summary>
    void RemoveView(int viewId);
}
