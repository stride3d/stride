// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using Stride.Editor.EditorGame.Game;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Editor.AssetEditors.GameEditor.Game;
using Stride.Assets.Editor.AssetEditors.Gizmos;

namespace Stride.Assets.Editor.EditorGame;

/// <summary>
/// The grid of the scene and prefab editors, on the ground plane (perpendicular to Y) as the WPF scene editor opens it.
/// </summary>
[EditorGameService(typeof(IEntityHierarchyEditorController), Order = 130)]
public sealed class EditorGameViewportGridService : EditorGameGridService<ViewportGridGizmo>
{
    /// <summary>
    /// The axis flag of the ground plane in <see cref="EditorGameGridService{TGridGizmo}.AxisIndex"/> (X = 1, Y = 2, Z = 4).
    /// </summary>
    private const int GroundAxis = 2;

    public EditorGameViewportGridService()
    {
        AxisIndex = GroundAxis;
    }
}
