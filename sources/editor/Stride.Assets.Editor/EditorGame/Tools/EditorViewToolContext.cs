// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Tools;
using Stride.Assets.Presentation.AssetEditors.GameEditor;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Core;

namespace Stride.Assets.Editor.EditorGame.Tools;

/// <summary>
/// What the tools of a view work with, and the transformation settings of the view, which the transformation tools read.
/// </summary>
internal sealed class EditorViewToolContext(
    EntityHierarchyEditorGame game,
    IEditorGameController controller,
    IEditorGameEntitySelectionService selection,
    IEditorGameCameraService camera,
    Action<IReadOnlyDictionary<AbsoluteId, TransformationTRS>> updateTransformations) : IEditorViewToolContext
{
    /// <inheritdoc />
    public EntityHierarchyEditorGame Game { get; } = game;

    /// <inheritdoc />
    public IEditorGameController Controller { get; } = controller;

    /// <inheritdoc />
    public IEditorGameEntitySelectionService Selection { get; } = selection;

    /// <inheritdoc />
    public IEditorGameCameraService Camera { get; } = camera;

    /// <summary>
    /// Gets the transformation settings of the view.
    /// </summary>
    public TransformationSettings Transformation { get; } = new();

    /// <inheritdoc />
    public void UpdateTransformations(IReadOnlyDictionary<AbsoluteId, TransformationTRS> transformations)
    {
        updateTransformations(transformations);
    }
}

/// <summary>
/// The transformation settings of a view (game thread): the default values of the WPF scene settings, until the settings
/// of the scene editor are ported.
/// </summary>
internal sealed class TransformationSettings
{
    /// <summary>
    /// Gets or sets the space of the translation and rotation gizmos; the scale gizmo works in the object space.
    /// </summary>
    public TransformationSpace Space { get; set; } = TransformationSpace.WorldSpace;

    /// <summary>
    /// Gets or sets the size of the transformation gizmos.
    /// </summary>
    public float GizmoSize { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets whether the snap key is down: the gizmos snap while it is, even when their snap is not active.
    /// </summary>
    public bool IsSnapKeyDown { get; set; }

    /// <summary>
    /// Gets the snap of the translations, in scene units.
    /// </summary>
    public float TranslationSnap { get; set; } = 1.0f;

    /// <summary>
    /// Gets the snap of the rotations, in degrees.
    /// </summary>
    public float RotationSnap { get; set; } = 22.5f;

    /// <summary>
    /// Gets the snap of the scales, as a factor.
    /// </summary>
    public float ScaleSnap { get; set; } = 1.1f;

    /// <summary>
    /// Gets or sets whether the translations snap, without the snap key.
    /// </summary>
    public bool TranslationSnapActive { get; set; }

    /// <summary>
    /// Gets or sets whether the rotations snap, without the snap key.
    /// </summary>
    public bool RotationSnapActive { get; set; }

    /// <summary>
    /// Gets or sets whether the scales snap, without the snap key.
    /// </summary>
    public bool ScaleSnapActive { get; set; }

    /// <summary>
    /// Gets whether a transformation snaps, without the snap key.
    /// </summary>
    public bool IsSnapActive(Transformation transformation) => transformation switch
    {
        Transformation.Translation => TranslationSnapActive,
        Transformation.Rotation => RotationSnapActive,
        Transformation.Scale => ScaleSnapActive,
        _ => throw new ArgumentOutOfRangeException(nameof(transformation)),
    };

    /// <summary>
    /// Sets the snap of a transformation, and whether it snaps without the snap key.
    /// </summary>
    public void SetSnap(Transformation transformation, float value, bool isActive)
    {
        switch (transformation)
        {
            case Transformation.Translation:
                TranslationSnap = value;
                TranslationSnapActive = isActive;
                break;
            case Transformation.Rotation:
                RotationSnap = value;
                RotationSnapActive = isActive;
                break;
            case Transformation.Scale:
                ScaleSnap = value;
                ScaleSnapActive = isActive;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(transformation));
        }
    }

    /// <summary>
    /// Gets the snap of a transformation.
    /// </summary>
    public float GetSnap(Transformation transformation) => transformation switch
    {
        Transformation.Translation => TranslationSnap,
        Transformation.Rotation => RotationSnap,
        Transformation.Scale => ScaleSnap,
        _ => throw new ArgumentOutOfRangeException(nameof(transformation)),
    };
}
