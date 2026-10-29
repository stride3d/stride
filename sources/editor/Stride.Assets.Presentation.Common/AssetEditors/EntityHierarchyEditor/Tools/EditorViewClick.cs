// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using Stride.Assets.Presentation.SceneEditor;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Tools;

/// <summary>
/// A click in a view of the scene and prefab editors: a button released less than 8 pixels away from where it was
/// pressed. A drag (of the camera, of a gizmo) is never a click.
/// </summary>
public sealed record EditorViewClick
{
    /// <summary>
    /// Gets the clicked button.
    /// </summary>
    public required MouseButton Button { get; init; }

    /// <summary>
    /// Gets the position of the cursor in the view, from (0, 0) at the top left corner to (1, 1) at the bottom right one.
    /// </summary>
    public required Vector2 Position { get; init; }

    /// <summary>
    /// Gets whether Ctrl was down.
    /// </summary>
    public bool IsCtrlDown { get; init; }

    /// <summary>
    /// Gets whether Shift was down.
    /// </summary>
    public bool IsShiftDown { get; init; }

    /// <summary>
    /// Gets whether Alt was down.
    /// </summary>
    public bool IsAltDown { get; init; }

    /// <summary>
    /// Gets the entity under the cursor: the entity of a component gizmo (light, camera...) first, else the entity of the
    /// picked mesh or sprite (the instance of an instancing entity), or <c>null</c>.
    /// </summary>
    public Entity? Entity { get; init; }

    /// <summary>
    /// Gets the picking of the meshes and sprites under the cursor: entity, component, mesh, material and instance.
    /// </summary>
    public EntityPickingResult Picking { get; init; }

    /// <summary>
    /// Gets the ray from the camera through the cursor, in world space, to act where the user clicked (place an entity on
    /// the ground, for instance).
    /// </summary>
    public required Ray Ray { get; init; }
}
