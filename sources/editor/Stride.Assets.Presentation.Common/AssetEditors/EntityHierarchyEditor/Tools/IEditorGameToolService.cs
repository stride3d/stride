// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Editor.EditorGame.Game;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Tools;

/// <summary>
/// The tools of the view of a scene or prefab editor (game thread). The services that handle the default inputs (the
/// selection, the camera) ask whether the active tool keeps them on.
/// </summary>
public interface IEditorGameToolService : IEditorGameService
{
    /// <summary>
    /// Gets the active tool of the view.
    /// </summary>
    IEditorViewTool ActiveTool { get; }

    /// <summary>
    /// Indicates whether the given default inputs stay on with the active tool.
    /// </summary>
    /// <param name="inputs">The default inputs.</param>
    /// <returns><c>true</c> when the active tool keeps all of them on.</returns>
    bool IsDefaultInputOn(EditorViewDefaultInputs inputs);
}
