// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Stride.Input;

namespace Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Tools;

/// <summary>
/// A tool of a view of the scene and prefab editors. Select, translate, rotate and scale are tools, and a plugin adds its
/// own (<see cref="EditorViewToolAttribute"/>, or <see cref="ScenePluginContextExtensions.SceneViewTools"/>). One tool is
/// active in a view at a time.
/// </summary>
/// <remarks>
/// The members are called on the game thread, except <see cref="Name"/>, <see cref="IconPath"/> and <see cref="Shortcut"/>,
/// which the toolbar also reads on the UI thread: they must not change. A member that throws turns the tool off: the error
/// is logged with the tool and its plugin, recorded in the plugin's state, and the view goes back to the default tool. The
/// tool of a plugin stays off in every view until the session opens again.
/// </remarks>
public interface IEditorViewTool : IDisposable
{
    /// <summary>
    /// Gets the name of the tool, shown in the toolbar.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the icon of the tool in the toolbar, as path data (the path markup of WPF and Avalonia) in a 16 by 16 box, or
    /// <c>null</c> to show the first letter of the name.
    /// </summary>
    string? IconPath { get; }

    /// <summary>
    /// Gets the key that activates the tool while the view has the keyboard and no mouse button is down, or <see cref="Keys.None"/>.
    /// </summary>
    Keys Shortcut { get; }

    /// <summary>
    /// Gets the default inputs of the view that stay on while the tool is active.
    /// </summary>
    EditorViewDefaultInputs DefaultInputs { get; }

    /// <summary>
    /// Gets whether the tool takes the mouse at the moment, for instance while its gizmo is under the cursor or dragged:
    /// a press then belongs to the tool, neither to the selection nor to the camera.
    /// </summary>
    bool IsUsingMouse => false;

    /// <summary>
    /// Prepares the tool for its view, once, when the view is created.
    /// </summary>
    /// <param name="context">What the tool works with.</param>
    void Initialize(IEditorViewToolContext context);

    /// <summary>
    /// Called when the tool becomes the active tool of its view.
    /// </summary>
    void Activate() { }

    /// <summary>
    /// Called when another tool becomes active, or before the tool is disposed.
    /// </summary>
    void Deactivate() { }

    /// <summary>
    /// Called every frame while the tool is active, before the clicks of the frame, unless the tool implements
    /// <see cref="Update(CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// The view awaits the task, on the game thread, before it handles the clicks of the frame and moves to the next one: while
    /// the task runs, the tools get no other input. Complete it within the frame, or within a few frames for a step that waits
    /// for the editor (the duplication of a Ctrl+drag waits for the copies to be selected). A task of a plugin's tool that
    /// does not complete within 30 seconds turns the tool off.
    /// </remarks>
    Task Update() => Task.CompletedTask;

    /// <summary>
    /// Called every frame while the tool is active, before the clicks of the frame. The default calls <see cref="Update()"/>.
    /// </summary>
    /// <param name="cancellationToken">Canceled when the update of a plugin's tool takes longer than 30 seconds, or when the
    /// view closes: stop waiting then. After a timeout the tool is turned off, whether its task ends or not.</param>
    /// <remarks>
    /// The view awaits the task, on the game thread, before it handles the clicks of the frame and moves to the next one.
    /// </remarks>
    [Experimental("STREXP004")]
    Task Update(CancellationToken cancellationToken) => Update();

    /// <summary>
    /// Called for every click in the view while the tool is active. A left click also selects when the tool keeps
    /// <see cref="EditorViewDefaultInputs.Selection"/> and did not take the mouse on the press.
    /// </summary>
    /// <param name="click">The click, with the entity under the cursor.</param>
    void OnClick(EditorViewClick click) { }

    /// <summary>
    /// Called when a right click without camera drag opens the context menu of the view while the tool is active: the tool
    /// can add entries, and say whether they replace the commands of the plugins.
    /// </summary>
    /// <param name="click">The right click, with the entity under the cursor.</param>
    /// <returns>The entries of the tool, or null for none (the default).</returns>
    /// <remarks>
    /// A tool that does not keep <see cref="EditorViewDefaultInputs.CameraRightDrag"/> also gets the click in
    /// <see cref="OnClick"/>, first. To keep the menu closed, return an empty menu that replaces the others.
    /// </remarks>
    [Experimental("STREXP004")]
    EditorViewContextMenu? GetContextMenu(EditorViewClick click) => null;

    /// <summary>
    /// Releases what the tool created (gizmos, render stages) when its view closes.
    /// </summary>
    void IDisposable.Dispose() { }
}
