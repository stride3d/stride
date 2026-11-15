// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.AssetEditors.GameEditor.Services;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Assets.Editor.AssetEditors.GameEditor.ViewModels;

/// <summary>
/// The axes of the grid of an editor game, as flags of <see cref="EditorGridViewModel.AxisIndex"/>.
/// </summary>
[Flags]
public enum ViewportGridAxis
{
    X = 1,
    Y = 2,
    Z = 4,
}

/// <summary>
/// The grid options of the view of a scene or prefab editor: whether the grid shows, on which planes, and its opacity.
/// </summary>
public sealed class EditorGridViewModel : DispatcherViewModel
{
    private readonly IEditorGameController controller;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditorGridViewModel"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for this view model.</param>
    /// <param name="controller">The controller of the editor game, which has the grid service.</param>
    public EditorGridViewModel(IViewModelServiceProvider serviceProvider, IEditorGameController controller)
        : base(serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(controller);
        this.controller = controller;
        ToggleCommand = new AnonymousCommand(ServiceProvider, () => IsVisible = !IsVisible);
    }

    /// <summary>
    /// Gets or sets whether the grid is currently visible.
    /// </summary>
    public bool IsVisible { get => Service.IsActive; set => SetValue(IsVisible != value, () => Service.IsActive = value); }

    /// <summary>
    /// Gets or sets the color to apply to the grid.
    /// </summary>
    public Color3 Color { get => Service.Color; set => SetValue(Color != value, () => Service.Color = value); }

    /// <summary>
    /// Gets or sets the alpha level of the grid.
    /// </summary>
    public float Alpha { get => Service.Alpha; set => SetValue(Math.Abs(Alpha - value) > MathUtil.ZeroTolerance, () => Service.Alpha = value); }

    /// <summary>
    /// Gets or sets the axes of the grid, as <see cref="ViewportGridAxis"/> flags.
    /// </summary>
    public int AxisIndex
    {
        get => Service.AxisIndex;
        set => SetValue(AxisIndex != value, () => Service.AxisIndex = value, nameof(AxisIndex), nameof(IsXAxisVisible), nameof(IsYAxisVisible), nameof(IsZAxisVisible));
    }

    /// <summary>
    /// Gets or sets whether the grid shows on the plane perpendicular to the X axis.
    /// </summary>
    public bool IsXAxisVisible { get => HasAxis(ViewportGridAxis.X); set => SetAxis(ViewportGridAxis.X, value); }

    /// <summary>
    /// Gets or sets whether the grid shows on the plane perpendicular to the Y axis (the ground).
    /// </summary>
    public bool IsYAxisVisible { get => HasAxis(ViewportGridAxis.Y); set => SetAxis(ViewportGridAxis.Y, value); }

    /// <summary>
    /// Gets or sets whether the grid shows on the plane perpendicular to the Z axis.
    /// </summary>
    public bool IsZAxisVisible { get => HasAxis(ViewportGridAxis.Z); set => SetAxis(ViewportGridAxis.Z, value); }

    /// <summary>
    /// Gets a command that shows or hides the grid.
    /// </summary>
    public ICommandBase ToggleCommand { get; }

    private IEditorGameGridViewModelService Service => controller.GetService<IEditorGameGridViewModelService>();

    /// <summary>
    /// Shows the grid options saved with the scene.
    /// </summary>
    public void LoadSettings(SceneSettingsData sceneSettings)
    {
        IsVisible = sceneSettings.GridVisible;
        Color = sceneSettings.GridColor;
        Alpha = sceneSettings.GridOpacity;
        AxisIndex = sceneSettings.GridAxisIndex;
    }

    /// <summary>
    /// Writes the grid options in the settings of the scene.
    /// </summary>
    public void SaveSettings(SceneSettingsData sceneSettings)
    {
        sceneSettings.GridVisible = IsVisible;
        sceneSettings.GridColor = Color;
        sceneSettings.GridOpacity = Alpha;
        sceneSettings.GridAxisIndex = AxisIndex;
    }

    private bool HasAxis(ViewportGridAxis axis) => (AxisIndex & (int)axis) != 0;

    private void SetAxis(ViewportGridAxis axis, bool value)
    {
        AxisIndex = value ? AxisIndex | (int)axis : AxisIndex & ~(int)axis;
    }
}
