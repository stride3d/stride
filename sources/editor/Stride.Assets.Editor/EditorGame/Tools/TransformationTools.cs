// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Tools;
using Stride.Assets.Presentation.AssetEditors.GameEditor;
using Stride.Assets.Presentation.AssetEditors.Gizmos;
using Stride.Core;
using Stride.Engine;
using Stride.Input;

namespace Stride.Assets.Editor.EditorGame.Tools;

/// <summary>
/// Selects, without gizmo: the default tool of a view.
/// </summary>
[EditorViewTool(Order = 0)]
internal sealed class SelectTool : IEditorViewTool
{
    /// <inheritdoc />
    public string Name => "Select";

    /// <inheritdoc />
    public string IconPath => "F1M3,1L3,14 6.2,10.9 8.6,15.5 10.6,14.5 8.3,10 12.5,10z";

    /// <inheritdoc />
    public Keys Shortcut => Keys.Q;

    /// <inheritdoc />
    public EditorViewDefaultInputs DefaultInputs => EditorViewDefaultInputs.All;

    /// <inheritdoc />
    public void Initialize(IEditorViewToolContext context)
    {
    }
}

/// <summary>
/// Translates the selected entities with the translation gizmo.
/// </summary>
[EditorViewTool(Order = 10)]
internal sealed class TranslateTool() : TransformationTool(Transformation.Translation)
{
    /// <inheritdoc />
    public override string Name => "Translate";

    /// <inheritdoc />
    // The icon of the WPF scene editor
    public override string IconPath => "F1M9.3838,9.4995L7.5758,9.4995 7.5758,11.9565 5.7688,10.1485 5.7688,11.9565 8.4798,14.6665 11.1908,11.9565 11.1908,10.1485 9.3838,11.9565z M7.5757,5.4995L9.3837,5.4995 9.3837,2.9195 11.1907,4.7275 11.1907,2.9195 8.4797,0.2085 5.7687,2.9195 5.7687,4.7275 7.5757,2.9195z M6.542,8.3418L6.542,6.5338 3.961,6.5338 5.769,4.7268 3.961,4.7268 1.25,7.4378 3.961,10.1488 5.769,10.1488 3.961,8.3418z M12.998,4.7271L11.19,4.7271 12.998,6.5341 10.542,6.5341 10.542,8.3421 12.998,8.3421 11.19,10.1481 12.998,10.1481 15.709,7.4371z M7.576,8.342L9.384,8.342 9.384,6.534 7.576,6.534z";

    /// <inheritdoc />
    public override Keys Shortcut => Keys.W;

    /// <inheritdoc />
    protected override TransformationGizmo CreateGizmo() => new TranslationGizmo();
}

/// <summary>
/// Rotates the selected entities with the rotation gizmo.
/// </summary>
[EditorViewTool(Order = 20)]
internal sealed class RotateTool() : TransformationTool(Transformation.Rotation)
{
    /// <inheritdoc />
    public override string Name => "Rotate";

    /// <inheritdoc />
    // The icon of the WPF scene editor
    public override string IconPath => "F1M15,6.4062C15.003,5.4792 14.53,4.5532 13.6,3.7742 13.417,3.6212 13.217,3.4732 13,3.3332L13,5.9902C13.216,6.1302 13.42,6.2732 13.6,6.4252 14.171,6.9052 14.559,7.4422 14.777,8.0002 14.787,8.0282 14.804,8.0552 14.814,8.0832 14.531,8.8522 13.922,9.5832 12.967,10.1822 12.245,10.6362 11.402,10.9702 10.502,11.1832 11.062,10.5072 11.326,10.1642 11.84,9.4352 11.205,9.6952 10.514,9.8852 9.801,10.0012 9.707,10.0512 9.629,10.1832 9.555,10.2632 8.458,11.4402 7.263,12.4152 6.871,12.7352 7.857,13.7242 8.648,14.4202 9.786,15.3002 10.5,15.1832 11.19,14.9942 11.826,14.7342 11.3,14.3972 11.039,14.2222 10.494,13.8362 11.396,13.6242 12.238,13.2902 12.96,12.8362 14.313,11.9852 14.99,10.8732 14.993,9.7592L14.998,8.0002 15,7.1062C15,7.1142,14.998,7.1222,14.998,7.1302z M1.1855,8.0342C1.1895,8.0222 1.1965,8.0122 1.2015,8.0002 1.4175,7.4302 1.8145,6.8822 2.3995,6.3942 2.5815,6.2422 2.7825,6.0972 2.9995,5.9582L2.9995,3.3112C2.7815,3.4512 2.5825,3.5992 2.3995,3.7522 1.4785,4.5222 1.0095,5.4382 1.0065,6.3582L1.0025,8.0002 0.9995,9.0122C0.9995,9.0042,1.0015,8.9962,1.0015,8.9882L0.9995,9.7122C0.9965,10.8422 1.6865,11.9732 3.0675,12.8362 3.5805,13.1562 4.1535,13.4162 4.7635,13.6172L4.7695,10.9642C4.1605,10.7632 3.5875,10.5042 3.0745,10.1822 2.0935,9.5702 1.4685,8.8212 1.1855,8.0342z M8.8384,9.5605C8.9284,9.4455,9.0794,9.2525,9.3304,9.1195L9.4764,9.0405 9.6384,9.0135C10.2914,8.9075,10.9044,8.7375,11.4624,8.5095L12.0004,8.2895 12.0004,3.8335 8.0004,6.4995 4.0004,3.8335 4.0004,8.3335 7.6334,10.7555C8.0164,10.4025,8.4334,9.9975,8.8384,9.5605z M11.2236,3.1489L7.9996,0.9999 4.7766,3.1489 7.9996,5.2979z";

    /// <inheritdoc />
    public override Keys Shortcut => Keys.E;

    /// <inheritdoc />
    protected override TransformationGizmo CreateGizmo() => new RotationGizmo();
}

/// <summary>
/// Scales the selected entities with the scale gizmo.
/// </summary>
[EditorViewTool(Order = 30)]
internal sealed class ScaleTool() : TransformationTool(Transformation.Scale)
{
    /// <inheritdoc />
    public override string Name => "Scale";

    /// <inheritdoc />
    // The icon of the WPF scene editor
    public override string IconPath => "F1M11,8L11,14 2,14 2,5 7,5 8.9,5 9.9,4 1,4 1,15 12,15 12,6.143 11,7.142z M15,1L15,5.741 14,5.741 13.994,2.734 10,6.728 10,7 10,8 10,13 3,13 3,6 7,6 9,6 9.314,6 13.3,2.014 10.268,2.009 10.268,1.009z";

    /// <inheritdoc />
    public override Keys Shortcut => Keys.R;

    /// <inheritdoc />
    protected override TransformationGizmo CreateGizmo() => new ScaleGizmo();
}

/// <summary>
/// A tool that moves, rotates or scales the selected entities with a transformation gizmo, which shows on the last
/// selected entity while the tool is active. The transformation goes to the asset when the drag ends, as one undoable
/// operation.
/// </summary>
/// <remarks>
/// The tool part of the WPF <c>EditorGameEntityTransformService</c>. Ctrl+drag does not duplicate the selection yet.
/// </remarks>
internal abstract class TransformationTool(Transformation transformation) : IEditorViewTool
{
    private EditorViewToolContext? context;
    private TransformationGizmo? gizmo;
    private bool isActive;

    /// <summary>
    /// Gets the transformation of the tool.
    /// </summary>
    public Transformation Transformation { get; } = transformation;

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract string IconPath { get; }

    /// <inheritdoc />
    public abstract Keys Shortcut { get; }

    /// <inheritdoc />
    public EditorViewDefaultInputs DefaultInputs => EditorViewDefaultInputs.All;

    /// <inheritdoc />
    public bool IsUsingMouse => gizmo is { IsEnabled: true } && gizmo.IsUnderMouse();

    private EditorViewToolContext Context => context ?? throw new InvalidOperationException("The tool is not initialized.");

    private TransformationGizmo Gizmo => gizmo ?? throw new InvalidOperationException("The tool is not initialized.");

    /// <inheritdoc />
    public void Initialize(IEditorViewToolContext toolContext)
    {
        context = (EditorViewToolContext)toolContext;
        gizmo = CreateGizmo();
        gizmo.Initialize(context.Game.Services, context.Game.EditorScene);
        gizmo.IsEnabled = false;
        gizmo.TransformationEnded += TransformationEnded;
        context.Selection.SelectionUpdated += SelectionUpdated;
    }

    /// <inheritdoc />
    public void Activate()
    {
        isActive = true;
        UpdateIsEnabled();
    }

    /// <inheritdoc />
    public void Deactivate()
    {
        isActive = false;
        Gizmo.ClearTransformationAxes();
        UpdateIsEnabled();
    }

    /// <inheritdoc />
    public Task Update()
    {
        var settings = Context.Transformation;
        // The scale gizmo works in the object space
        Gizmo.Space = Transformation == Transformation.Scale ? TransformationSpace.ObjectSpace : settings.Space;
        Gizmo.SizeFactor = SmoothGizmoSize(settings.GizmoSize);
        Gizmo.SnapValue = settings.GetSnap(Transformation);
        Gizmo.UseSnap = settings.IsSnapKeyDown;
        return Gizmo.Update();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (context is not null)
            context.Selection.SelectionUpdated -= SelectionUpdated;
        if (gizmo is not null)
        {
            gizmo.TransformationEnded -= TransformationEnded;
            gizmo.Dispose();
        }
    }

    /// <summary>
    /// Creates the gizmo of the tool.
    /// </summary>
    protected abstract TransformationGizmo CreateGizmo();

    private void UpdateIsEnabled()
    {
        Gizmo.IsEnabled = isActive && Gizmo.AnchorEntity is not null;
    }

    /// <summary>
    /// Puts the gizmo on the last selected entity (game thread).
    /// </summary>
    private void SelectionUpdated(object? sender, EntitySelectionEventArgs e)
    {
        Gizmo.AnchorEntity = e.NewSelection.LastOrDefault();
        Gizmo.ModifiedEntities = e.NewSelection.ToList();
        // Reset the transformation axes if the selection is cleared
        if (Gizmo.AnchorEntity is null)
            Gizmo.ClearTransformationAxes();
        UpdateIsEnabled();
    }

    /// <summary>
    /// Writes the transformations of the selected roots to the asset at the end of a drag.
    /// </summary>
    private void TransformationEnded(object? sender, EventArgs e)
    {
        var transformations = new Dictionary<AbsoluteId, TransformationTRS>();
        foreach (var id in Context.Selection.GetSelectedRootIds())
        {
            if (Context.Controller.FindGameSidePart(id) is Entity entity)
                transformations.Add(id, new TransformationTRS(entity.Transform));
        }
        Context.UpdateTransformations(transformations);
    }

    private static float SmoothGizmoSize(float value)
    {
        return value > 1 ? 1 + (value - 1) * 0.1f : MathF.Pow(value, 0.333f);
    }
}
