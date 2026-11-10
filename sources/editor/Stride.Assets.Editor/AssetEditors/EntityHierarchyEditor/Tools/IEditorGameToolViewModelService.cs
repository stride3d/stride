// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.AssetEditors.GameEditor;
using Stride.Editor.EditorGame.ViewModels;

namespace Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Tools;

/// <summary>
/// The tools of the view of a scene or prefab editor, as the toolbar of each user interface drives them: the active tool
/// (select, translate, rotate, scale and the tools of the plugins), the space, snaps and size of the transformation gizmos.
/// </summary>
/// <remarks>
/// The members can be called from any thread: they run on the game thread, and the events are raised there.
/// </remarks>
public interface IEditorGameToolViewModelService : IEditorGameViewModelService
{
    /// <summary>
    /// Gets the tools of the view, in toolbar order, once the game is initialized: the select tool comes first.
    /// </summary>
    IReadOnlyList<IEditorViewTool> Tools { get; }

    /// <summary>
    /// Raised with the new active tool (game thread), when a key or a call changes it.
    /// </summary>
    event EventHandler<IEditorViewTool>? ActiveToolChanged;

    /// <summary>
    /// Raised with a tool turned off after an error (game thread).
    /// </summary>
    event EventHandler<IEditorViewTool>? ToolFailed;

    /// <summary>
    /// Gets the active tool and the tools turned off after an error.
    /// </summary>
    Task<(IEditorViewTool ActiveTool, IReadOnlyList<IEditorViewTool> FailedTools)> GetStateAsync();

    /// <summary>
    /// Makes a tool the active one.
    /// </summary>
    /// <returns><c>false</c> when the tool was turned off.</returns>
    Task<bool> ActivateAsync(IEditorViewTool tool);

    /// <summary>
    /// Sets the space of the translation and rotation gizmos; the scale gizmo works in the object space.
    /// </summary>
    Task SetSpaceAsync(TransformationSpace space);

    /// <summary>
    /// Sets the size of the transformation gizmos, as the factor of the size sliders.
    /// </summary>
    Task SetGizmoSizeAsync(float size);

    /// <summary>
    /// Sets the snap of a transformation, and whether it snaps without the snap key.
    /// </summary>
    /// <param name="transformation">The transformation.</param>
    /// <param name="value">The snap: a distance in scene units, an angle in degrees or a scale factor.</param>
    /// <param name="isActive">Whether the gizmo of the transformation always snaps.</param>
    Task SetSnapAsync(Transformation transformation, float value, bool isActive);
}
