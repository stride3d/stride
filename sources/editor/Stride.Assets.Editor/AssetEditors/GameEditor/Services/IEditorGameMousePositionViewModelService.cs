// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using Stride.Core.Mathematics;
using Stride.Editor.EditorGame.ViewModels;

namespace Stride.Assets.Editor.AssetEditors.GameEditor.Services;

/// <summary>
/// The game service that knows where the mouse is over the view of the game, for the entities created at the mouse (the
/// entity menu of the view, the assets dropped on the view).
/// </summary>
public interface IEditorGameMousePositionViewModelService : IEditorGameViewModelService
{
    /// <summary>
    /// Gets the point of the scene under the mouse, or under the last right click (UI thread).
    /// </summary>
    /// <param name="lastRightClick"><c>true</c> for the point of the last right click, which opened the menu of the view.</param>
    /// <returns>The point in the scene, on the ground plane when nothing is under the mouse.</returns>
    Vector3 GetMousePositionInScene(bool lastRightClick);
}
