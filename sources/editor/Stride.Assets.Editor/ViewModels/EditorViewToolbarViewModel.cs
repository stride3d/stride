// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.EditorGame;
using Stride.Assets.Editor.EditorGame.Tools;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Tools;
using Stride.Assets.Presentation.AssetEditors.GameEditor;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Core.Extensions;
using Stride.Core.Presentation.ViewModels;
using Stride.Input;

namespace Stride.Assets.Editor.ViewModels;

/// <summary>
/// The toolbar of the view of a scene or prefab editor: the tools of the view, the space of the transformation gizmos, the
/// material selection mode and the selection mask, as the transformation toolbar of the WPF scene editor.
/// </summary>
public sealed class EditorViewToolbarViewModel : DispatcherViewModel
{
    // The icons of the WPF scene editor
    private const string WorldSpaceIcon = "F1M9.3838,13.5752C9.9298,12.4292 10.3508,11.3602 10.6218,10.3822 11.5158,10.2662 12.4668,10.0392 13.5078,9.6472 12.9298,11.5842 11.3578,13.0872 9.3838,13.5752 M2.4868,9.6252C3.2838,9.8692 4.1978,10.1032 5.2308,10.2692 5.4368,11.3162 5.7728,12.3862 6.2118,13.4632 4.4218,12.8772 3.0208,11.4382 2.4868,9.6252 M2.2498,8.0002C2.2498,7.1802 2.4238,6.4032 2.7328,5.6972 3.4178,5.8992 4.1808,6.0912 5.0318,6.2362 4.9898,6.6412 4.9598,7.0562 4.9548,7.4942 4.9488,8.0292 4.9888,8.5772 5.0538,9.1312 3.9778,8.9352 3.0478,8.6672 2.2708,8.4032 2.2608,8.2692 2.2498,8.1372 2.2498,8.0002 M5.9168,2.6452C5.6268,3.3682 5.3618,4.1992 5.1848,5.1532 4.4928,5.0332 3.8608,4.8812 3.2818,4.7182 3.9298,3.7882 4.8458,3.0622 5.9168,2.6452 M7.9998,2.2502C8.1648,2.2502 8.3268,2.2612 8.4878,2.2742 8.9558,3.0862 9.4318,4.1202 9.7088,5.3822 9.1378,5.4212 8.5758,5.4262 8.0058,5.4212 7.4058,5.4142 6.8248,5.3732 6.2668,5.3102 6.4998,4.0952 6.8848,3.0962 7.2678,2.3022 7.5078,2.2712 7.7508,2.2502 7.9998,2.2502 M8.0058,9.4212C7.3698,9.4142 6.7578,9.3672 6.1678,9.2982 6.0838,8.6882 6.0388,8.0892 6.0448,7.5062 6.0488,7.1192 6.0758,6.7502 6.1128,6.3902 6.7148,6.4592 7.3428,6.5042 7.9938,6.5112 8.1288,6.5122 8.2628,6.5132 8.3978,6.5132 8.8858,6.5132 9.3798,6.4982 9.8848,6.4592 9.9228,6.7992 9.9508,7.1442 9.9548,7.5062 9.9608,8.0832 9.8868,8.7082 9.7458,9.3752 9.1618,9.4162 8.5878,9.4242 8.0058,9.4212 M7.9998,13.7502C7.8298,13.7502 7.6618,13.7392 7.4958,13.7242 6.9928,12.6082 6.6088,11.5002 6.3678,10.4192 6.8918,10.4712 7.4348,10.5052 7.9938,10.5112 8.1288,10.5122 8.2628,10.5122 8.3978,10.5122 8.7448,10.5122 9.0968,10.5032 9.4508,10.4852 9.1458,11.4762 8.6928,12.5622 8.0988,13.7452 8.0658,13.7462 8.0328,13.7502 7.9998,13.7502 M12.7488,4.7602C12.0628,4.9962 11.4148,5.1602 10.7978,5.2562 10.5838,4.2242 10.2468,3.3372 9.8808,2.5712 11.0568,2.9782 12.0598,3.7512 12.7488,4.7602 M10.9688,6.3382C11.6958,6.2242 12.4628,6.0302 13.2848,5.7342 13.5838,6.4292 13.7498,7.1952 13.7498,8.0002 13.7498,8.1262 13.7398,8.2502 13.7318,8.3742 12.6938,8.8262 11.7598,9.0962 10.8838,9.2372 10.9938,8.6252 11.0508,8.0432 11.0448,7.4942 11.0408,7.0942 11.0098,6.7122 10.9688,6.3382 M14.9998,8.0002C14.9998,7.9202 14.9898,7.8432 14.9888,7.7632 14.8628,4.0082 11.7858,1.0002 7.9998,1.0002 4.1338,1.0002 0.9998,4.1342 0.9998,8.0002 0.9998,8.2042 1.0118,8.4052 1.0298,8.6032 1.0458,8.7832 1.0708,8.9612 1.1008,9.1362 1.1018,9.1412 1.1018,9.1472 1.1028,9.1532 1.5918,12.0962 3.9088,14.4112 6.8528,14.8982 6.8598,14.8992 6.8658,14.9002 6.8728,14.9012 7.0468,14.9302 7.2218,14.9542 7.3998,14.9702 7.5978,14.9882 7.7968,15.0002 7.9998,15.0002 8.2318,15.0002 8.4598,14.9872 8.6848,14.9662 12.2298,14.6212 14.9998,11.6352 14.9998,8.0002";
    private const string ObjectSpaceIcon = "F1M8,8L4,8 4,7 8,7z M6,10L4,10 4,9 6,9z M13,10.586L11,12.586 11,5.707 13,3.707z M10.293,5L6,5 6,3 12.293,3z M10,13L3,13 3,6 10,6z M5,3.414L5,5 3.414,5z M5,2L2,5 2,14 10,14 11,14 14,11 14,2z";
    private const string ViewSpaceIcon = "F1M10,8C10,6.897 9.103,6 8,6 6.897,6 6,6.897 6,8 6,9.103 6.897,10 8,10 9.103,10 10,9.103 10,8 M11,8C11,9.654 9.654,11 8,11 6.346,11 5,9.654 5,8 5,6.346 6.346,5 8,5 9.654,5 11,6.346 11,8 M15,9L14,9C14,5.691 11.309,3 8,3 4.691,3 2,5.691 2,9L1,9C1,5.14 4.14,2 8,2 11.859,2 15,5.14 15,9";

    private readonly EditorGameToolService service;
    private readonly IEditorGameSelectionViewModelService? selection;
    private readonly EditorGameMaterialHighlightService? materialHighlight;
    private EditorViewToolViewModel? activeTool;
    private EditorViewSpaceViewModel space;
    private bool displaySelectionMask;

    internal EditorViewToolbarViewModel(IViewModelServiceProvider serviceProvider, EditorGameToolService service, IEditorGameSelectionViewModelService? selection, EditorGameMaterialHighlightService? materialHighlight,
        EditorGameComponentGizmoService? componentGizmos = null, IEnumerable<IEditorGameOverlayService>? overlays = null, EditorGameLightProbeGizmoService? lightProbes = null)
        : base(serviceProvider)
    {
        this.service = service;
        this.selection = selection;
        this.materialHighlight = materialHighlight;
        Gizmos = componentGizmos is not null ? new EditorViewGizmosViewModel(serviceProvider, componentGizmos, service.SetGizmoSizeAsync, lightProbes) : null;
        Overlays = new EditorViewOverlaysViewModel(serviceProvider, overlays ?? []);
        Tools = service.Tools.Select(x => new EditorViewToolViewModel(serviceProvider, x)).ToList();
        Spaces =
        [
            new EditorViewSpaceViewModel(serviceProvider, TransformationSpace.WorldSpace, "Use world coordinates for transformations", WorldSpaceIcon),
            new EditorViewSpaceViewModel(serviceProvider, TransformationSpace.ObjectSpace, "Use local coordinates for transformations", ObjectSpaceIcon),
            new EditorViewSpaceViewModel(serviceProvider, TransformationSpace.ViewSpace, "Use current camera projection coordinates for transformations", ViewSpaceIcon),
        ];
        space = Spaces[0];
        // The snaps of a new scene in the WPF scene editor (SceneSettingsData)
        Snaps =
        [
            new EditorViewSnapViewModel(serviceProvider, service, Transformation.Translation, 1.0f, "Snap translations to this value"),
            new EditorViewSnapViewModel(serviceProvider, service, Transformation.Rotation, 22.5f, "Snap rotations to this value"),
            new EditorViewSnapViewModel(serviceProvider, service, Transformation.Scale, 1.1f, "Snap scale to this factor"),
        ];
        displaySelectionMask = selection?.DisplaySelectionMask ?? false;
        service.ActiveToolChanged += ServiceActiveToolChanged;
        service.ToolFailed += ServiceToolFailed;
    }

    /// <summary>
    /// Gets the tools of the view, in toolbar order.
    /// </summary>
    public IReadOnlyList<EditorViewToolViewModel> Tools { get; }

    /// <summary>
    /// Gets or sets the active tool of the view.
    /// </summary>
    public EditorViewToolViewModel? ActiveTool
    {
        get => activeTool;
        set
        {
            // The list of the toolbar clears its selection while it changes
            if (value is null || value == activeTool || !value.IsEnabled)
                return;

            UpdateActiveTool(value);
            _ = ActivateAsync(value);
        }
    }

    /// <summary>
    /// Gets the spaces of the transformation gizmos.
    /// </summary>
    public IReadOnlyList<EditorViewSpaceViewModel> Spaces { get; }

    /// <summary>
    /// Gets or sets the space of the transformation gizmos: the scale gizmo always works in the object space.
    /// </summary>
    public EditorViewSpaceViewModel? Space
    {
        get => IsScaleActive ? Spaces[1] : space;
        set
        {
            if (value is null || !value.IsEnabled || IsScaleActive)
                return;

            SetValue(ref space, value, () => service.SetSpaceAsync(value.Space));
        }
    }

    /// <summary>
    /// Gets the snaps of the transformation gizmos; the snap of the active tool shows, as in the WPF scene editor.
    /// </summary>
    public IReadOnlyList<EditorViewSnapViewModel> Snaps { get; }

    /// <summary>
    /// Gets or sets whether the material selection mode is on: the material of a selected entity under the mouse is
    /// highlighted, and a click on it selects the material asset.
    /// </summary>
    public bool MaterialSelectionMode
    {
        get => materialHighlight?.IsActive ?? false;
        set => SetValue(MaterialSelectionMode != value && materialHighlight is not null, () => materialHighlight!.IsActive = value);
    }

    /// <summary>
    /// Gets or sets whether the wireframe of the selected models stays shown, or fades out after the selection changes.
    /// </summary>
    public bool DisplaySelectionMask
    {
        get => displaySelectionMask;
        set => SetValue(ref displaySelectionMask, value, () =>
        {
            if (selection is not null)
                selection.DisplaySelectionMask = value;
        });
    }

    /// <summary>
    /// Gets the gizmo options of the view (sizes, and the gizmos of each type of component), or <c>null</c> when the game
    /// shows no component gizmo.
    /// </summary>
    public EditorViewGizmosViewModel? Gizmos { get; }

    /// <summary>
    /// Gets the overlays the services of the game can draw (the navigation meshes, for instance).
    /// </summary>
    public EditorViewOverlaysViewModel Overlays { get; }

    private bool IsScaleActive => activeTool?.Tool is TransformationTool { Transformation: Transformation.Scale };

    /// <inheritdoc/>
    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(EditorViewToolbarViewModel));

        service.ActiveToolChanged -= ServiceActiveToolChanged;
        service.ToolFailed -= ServiceToolFailed;
        Overlays.Destroy();
        base.Destroy();
    }

    /// <summary>
    /// Shows the gizmo and overlay options saved with the scene.
    /// </summary>
    /// <remarks>
    /// The other members of the settings (camera, snaps, grid, ...) are not used by the Avalonia editor yet and stay as read.
    /// </remarks>
    internal void LoadSettings(SceneSettingsData settings)
    {
        Gizmos?.LoadSettings(settings);
        Overlays.LoadSettings(settings);
    }

    /// <summary>
    /// Writes the gizmo and overlay options in the settings of the scene.
    /// </summary>
    internal void SaveSettings(SceneSettingsData settings)
    {
        Gizmos?.SaveSettings(settings);
        Overlays.SaveSettings(settings);
    }

    /// <summary>
    /// Reads the active tool and the tools turned off from the game.
    /// </summary>
    internal async Task InitializeAsync()
    {
        foreach (var snap in Snaps)
        {
            await snap.ApplyAsync();
        }
        var (active, failedTools) = await service.GetStateAsync();
        foreach (var tool in Tools)
        {
            tool.IsEnabled = !failedTools.Contains(tool.Tool);
        }
        UpdateActiveTool(Tools.FirstOrDefault(x => x.Tool == active));
    }

    private async Task ActivateAsync(EditorViewToolViewModel tool)
    {
        // The tool may have failed meanwhile: the active tool of the game shows again
        if (await service.ActivateAsync(tool.Tool))
            return;

        var (active, _) = await service.GetStateAsync();
        UpdateActiveTool(Tools.FirstOrDefault(x => x.Tool == active));
    }

    private void UpdateActiveTool(EditorViewToolViewModel? tool)
    {
        SetValue(ref activeTool, tool, nameof(ActiveTool), nameof(Space));
        // The scale gizmo works in the object space only, and the other tools have no gizmo
        var hasSpace = tool?.Tool is TransformationTool;
        foreach (var item in Spaces)
        {
            item.IsEnabled = hasSpace && (!IsScaleActive || item.Space == TransformationSpace.ObjectSpace);
        }
        foreach (var snap in Snaps)
        {
            snap.IsVisible = tool?.Tool is TransformationTool transformationTool && transformationTool.Transformation == snap.Transformation;
        }
    }

    private void ServiceActiveToolChanged(object? sender, IEditorViewTool tool)
    {
        Dispatcher.InvokeAsync(() =>
        {
            if (!IsDestroyed)
                UpdateActiveTool(Tools.FirstOrDefault(x => x.Tool == tool));
        });
    }

    private void ServiceToolFailed(object? sender, IEditorViewTool tool)
    {
        Dispatcher.InvokeAsync(() =>
        {
            if (Tools.FirstOrDefault(x => x.Tool == tool) is { } failedTool)
                failedTool.IsEnabled = false;
        });
    }
}

/// <summary>
/// A tool in the toolbar of the view of a scene or prefab editor.
/// </summary>
public sealed class EditorViewToolViewModel : DispatcherViewModel
{
    private bool isEnabled = true;

    internal EditorViewToolViewModel(IViewModelServiceProvider serviceProvider, IEditorViewTool tool)
        : base(serviceProvider)
    {
        Tool = tool;
        Name = tool.Name;
        IconPath = tool.IconPath;
        Initial = Name.Length > 0 ? Name[..1].ToUpperInvariant() : "?";
        ToolTip = tool.Shortcut != Keys.None ? $"{Name} ({tool.Shortcut})" : Name;
    }

    /// <summary>
    /// Gets the name of the tool.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the icon of the tool as path data, or <c>null</c>.
    /// </summary>
    public string? IconPath { get; }

    /// <summary>
    /// Gets the first letter of the name, shown when the tool has no icon.
    /// </summary>
    public string Initial { get; }

    /// <summary>
    /// Gets the tool tip of the tool: its name and its key.
    /// </summary>
    public string ToolTip { get; }

    /// <summary>
    /// Gets whether the tool can be used: <c>false</c> once it was turned off after an error.
    /// </summary>
    public bool IsEnabled { get => isEnabled; internal set => SetValue(ref isEnabled, value); }

    internal IEditorViewTool Tool { get; }
}

/// <summary>
/// A space of the transformation gizmos in the toolbar of the view of a scene or prefab editor.
/// </summary>
public sealed class EditorViewSpaceViewModel : DispatcherViewModel
{
    private bool isEnabled = true;

    internal EditorViewSpaceViewModel(IViewModelServiceProvider serviceProvider, TransformationSpace space, string toolTip, string iconPath)
        : base(serviceProvider)
    {
        Space = space;
        ToolTip = toolTip;
        IconPath = iconPath;
    }

    /// <summary>
    /// Gets the space.
    /// </summary>
    public TransformationSpace Space { get; }

    /// <summary>
    /// Gets the tool tip of the space.
    /// </summary>
    public string ToolTip { get; }

    /// <summary>
    /// Gets the icon of the space as path data.
    /// </summary>
    public string IconPath { get; }

    /// <summary>
    /// Gets whether the active tool works in this space.
    /// </summary>
    public bool IsEnabled { get => isEnabled; internal set => SetValue(ref isEnabled, value); }
}

public sealed class EditorViewSnapViewModel : DispatcherViewModel
{
    private readonly EditorGameToolService service;
    private bool isActive;
    private double value;
    private bool isVisible;

    internal EditorViewSnapViewModel(IViewModelServiceProvider serviceProvider, EditorGameToolService service, Transformation transformation, float value, string toolTip)
        : base(serviceProvider)
    {
        this.service = service;
        this.value = value;
        Transformation = transformation;
        ToolTip = toolTip;
    }

    /// <summary>
    /// Gets the transformation that snaps.
    /// </summary>
    public Transformation Transformation { get; }

    /// <summary>
    /// Gets the tool tip of the snap.
    /// </summary>
    public string ToolTip { get; }

    /// <summary>
    /// Gets or sets whether the gizmo of the transformation snaps; it also snaps while the snap key (Shift) is down.
    /// </summary>
    public bool IsActive { get => isActive; set => SetValue(ref isActive, value, () => ApplyAsync().Forget()); }

    /// <summary>
    /// Gets or sets the snap: a distance in scene units, an angle in degrees or a scale factor.
    /// </summary>
    public double Value { get => value; set => SetValue(ref this.value, Math.Max(0, value), () => ApplyAsync().Forget()); }

    /// <summary>
    /// Gets whether the tool of the transformation is active.
    /// </summary>
    public bool IsVisible { get => isVisible; internal set => SetValue(ref isVisible, value); }

    internal Task ApplyAsync() => service.SetSnapAsync(Transformation, (float)value, isActive);
}
