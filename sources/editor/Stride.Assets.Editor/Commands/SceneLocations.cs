// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Commands;

namespace Stride.Assets.Editor.Commands;

/// <summary>
/// The locations of the scene and prefab editors where commands can be placed (<see cref="CommandPlacement.Location"/>),
/// besides the editor's own (<see cref="CommandLocations"/>).
/// </summary>
public static class SceneLocations
{
    /// <summary>
    /// The context menu of the entity tree of a scene or prefab editor; the selection is the selected entities and folders.
    /// </summary>
    public const string EntityTreeContext = "EntityTree/Context";

    /// <summary>
    /// The context menu of the scene view of a scene or prefab editor, opened by a right click without a camera drag; the
    /// selection is what the click picked. Declared now; the scene view shows it in a later version.
    /// </summary>
    public const string SceneViewContext = "SceneView/Context";
}
