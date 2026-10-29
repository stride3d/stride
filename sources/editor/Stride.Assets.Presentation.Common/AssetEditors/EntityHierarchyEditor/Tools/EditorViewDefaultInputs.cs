// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Tools;

/// <summary>
/// The inputs of a view of the scene and prefab editors that work with any tool: a tool says which of them stay on while
/// it is active (<see cref="IEditorViewTool.DefaultInputs"/>).
/// </summary>
[Flags]
public enum EditorViewDefaultInputs
{
    /// <summary>
    /// The tool takes every click and drag of the left, middle and right buttons. The wheel still zooms.
    /// </summary>
    None = 0,

    /// <summary>
    /// A left click selects the entity under the cursor, and Ctrl+click adds it to the selection or removes it.
    /// </summary>
    Selection = 1,

    /// <summary>
    /// A middle drag pans the camera, and a middle and right drag moves it.
    /// </summary>
    CameraMiddleDrag = 2,

    /// <summary>
    /// A right drag rotates the camera (Alt+right drag zooms), and the camera keys move it while the button is down.
    /// </summary>
    CameraRightDrag = 4,

    /// <summary>
    /// The selection and the camera drags stay on: the tool only adds to them.
    /// </summary>
    All = Selection | CameraMiddleDrag | CameraRightDrag,
}
