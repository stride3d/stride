// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.AssetEditors.GameEditor;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Core;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Tools;

/// <summary>
/// What a tool of a view of the scene and prefab editors works with (game thread, unless said otherwise).
/// </summary>
public interface IEditorViewToolContext
{
    /// <summary>
    /// Gets the game of the editor: its scene, the editor scene where gizmos go, its input and services.
    /// </summary>
    EntityHierarchyEditorGame Game { get; }

    /// <summary>
    /// Gets the controller of the game: the game-side entities of the asset (<see cref="IEditorGameController.FindGameSidePart"/>).
    /// </summary>
    IEditorGameController Controller { get; }

    /// <summary>
    /// Gets the selection of the editor.
    /// </summary>
    IEditorGameEntitySelectionService Selection { get; }

    /// <summary>
    /// Gets the camera of the view.
    /// </summary>
    IEditorGameCameraService Camera { get; }

    /// <summary>
    /// Writes the transformations of entities the tool moved in the game back to the asset, as one undoable operation
    /// (any thread).
    /// </summary>
    /// <param name="transformations">The new transformations, by entity.</param>
    void UpdateTransformations(IReadOnlyDictionary<AbsoluteId, TransformationTRS> transformations);
}
