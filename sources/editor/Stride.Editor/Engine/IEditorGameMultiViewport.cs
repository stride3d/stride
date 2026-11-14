// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Stride.Graphics;

namespace Stride.Editor.Engine;

/// <summary>
/// A view of an <see cref="IEditorGameMultiViewport"/>, with its size in device pixels.
/// </summary>
/// <param name="Id">The identifier of the view, which the user interface gives it.</param>
/// <param name="Width">The width of the view, in device pixels (0 while the view is not laid out).</param>
/// <param name="Height">The height of the view, in device pixels (0 while the view is not laid out).</param>
[Experimental("STREXP009")]
public readonly record struct EditorGameViewInfo(int Id, int Width, int Height);

/// <summary>
/// A viewport that shows several views of its game, each with its own camera.
/// </summary>
/// <remarks>
/// <para>
/// One view is active: the game draws it as usual into its back buffer, with the camera of the editor, and gets the
/// pointer input of that view only. The game draws the other views into textures of their own, with a camera of their own
/// (see <c>EditorGameMultiViewService</c>), and hands them to <see cref="PresentView"/>.
/// </para>
/// <para>
/// The user interface asks for a view to become active (<see cref="RequestedActiveView"/>, when the pointer comes over it);
/// the game makes it active between two frames, once it swapped the camera, and only while no mouse button is down, so that
/// a drag stays in the view it started in.
/// </para>
/// </remarks>
[Experimental("STREXP009")]
public interface IEditorGameMultiViewport
{
    /// <summary>
    /// Gets the identifier of the primary view, the one the viewport starts with.
    /// </summary>
    const int PrimaryView = 0;

    /// <summary>
    /// Gets the view the user points at, which the game should make active (any thread).
    /// </summary>
    int RequestedActiveView { get; }

    /// <summary>
    /// Gets or sets the view the back buffer and the pointer input of the game go to (game thread).
    /// </summary>
    int ActiveView { get; set; }

    /// <summary>
    /// Fills <paramref name="views"/> with the views of the viewport and their size (game thread).
    /// </summary>
    void GetViews(List<EditorGameViewInfo> views);

    /// <summary>
    /// Shows a frame the game drew for a view other than the active one (game thread, while the game draws).
    /// </summary>
    /// <param name="viewId">The view.</param>
    /// <param name="commandList">The command list of the frame.</param>
    /// <param name="frame">The texture the view was drawn into, in the format of the back buffer.</param>
    void PresentView(int viewId, CommandList commandList, Texture frame);
}
