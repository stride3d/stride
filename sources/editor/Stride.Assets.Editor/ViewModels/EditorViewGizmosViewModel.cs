// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.EditorGame;
using Stride.Core;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Core;
using Stride.Core.Presentation.ViewModels;
using Stride.Engine;
using Stride.Engine.Gizmos;

namespace Stride.Assets.Editor.ViewModels;

/// <summary>
/// The gizmo options popup of the view of a scene or prefab editor: the size of the transformation and component gizmos,
/// and one toggle per type of component with a gizmo, plugins included (the collider shapes of the Physics package are
/// hidden until their toggle is checked), as the gizmo options popup of the WPF scene editor.
/// </summary>
/// <remarks>
/// The agnostic copy of the WPF <c>EntityGizmosViewModel</c> and of the size sliders of its popup. The options are saved
/// per scene in <see cref="SceneSettingsData"/>, as WPF does; the toggles of a scene without settings start with what the
/// gizmo classes declare (<see cref="GizmoComponentAttribute.HiddenByDefault"/>).
/// </remarks>
public sealed class EditorViewGizmosViewModel : DispatcherViewModel
{
    private readonly EditorGameComponentGizmoService service;
    private readonly Func<float, Task>? setTransformationGizmoSize;
    private readonly EditorGameLightProbeGizmoService? lightProbes;
    private int transformationGizmoSizeIndex = Utils.ZoomFactorIdentityIndex;
    private int componentGizmoSizeIndex = Utils.ZoomFactorIdentityIndex;

    internal EditorViewGizmosViewModel(IViewModelServiceProvider serviceProvider, EditorGameComponentGizmoService service, Func<float, Task>? setTransformationGizmoSize,
        EditorGameLightProbeGizmoService? lightProbes = null)
        : base(serviceProvider)
    {
        this.service = service;
        this.setTransformationGizmoSize = setTransformationGizmoSize;
        this.lightProbes = lightProbes;

        var gizmoTypes = StrideEditorPlugin.GizmoTypeDictionary;
        Gizmos = typeof(EntityComponent).GetInheritedTypes()
            .Where(gizmoTypes.ContainsKey)
            .Select(x => new EditorViewGizmoViewModel(ServiceProvider, service, x, !IsHiddenByDefault(gizmoTypes[x])))
            .OrderBy(x => x.ComponentName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        // The entities without any other gizmo: hidden by default, as in the WPF scene editor
        FallbackGizmo = new EditorViewGizmoViewModel(ServiceProvider, service, typeof(TransformComponent), false);
        ToggleAllCommand = new AnonymousCommand<bool>(ServiceProvider, value => Gizmos.ForEach(x => x.IsActive = value));
    }

    /// <summary>
    /// Gets the toggles of the gizmos of the types of component, sorted by name.
    /// </summary>
    public IReadOnlyList<EditorViewGizmoViewModel> Gizmos { get; }

    /// <summary>
    /// Gets the toggle of the gizmo of the entities without any other gizmo ("Other entities").
    /// </summary>
    public EditorViewGizmoViewModel FallbackGizmo { get; }

    /// <summary>
    /// Gets the command that shows (<c>true</c>) or hides (<c>false</c>) the gizmos of every type of component.
    /// </summary>
    public ICommandBase ToggleAllCommand { get; }

    /// <summary>
    /// Gets the highest index of the size sliders (in <see cref="Utils.ZoomFactors"/>).
    /// </summary>
    public int MaximumSizeIndex => Utils.ZoomFactors.Length - 1;

    /// <summary>
    /// Gets or sets the size of the transformation gizmos, as an index in <see cref="Utils.ZoomFactors"/>.
    /// </summary>
    public int TransformationGizmoSizeIndex
    {
        get => transformationGizmoSizeIndex;
        set => SetValue(ref transformationGizmoSizeIndex, Math.Clamp(value, 0, MaximumSizeIndex), () => setTransformationGizmoSize?.Invoke((float)Utils.ZoomFactors[transformationGizmoSizeIndex]));
    }

    /// <summary>
    /// Gets or sets the size of the component gizmos, as an index in <see cref="Utils.ZoomFactors"/>.
    /// </summary>
    public int ComponentGizmoSizeIndex
    {
        get => componentGizmoSizeIndex;
        set => SetValue(ref componentGizmoSizeIndex, Math.Clamp(value, 0, MaximumSizeIndex), () => service.GizmoSize = (float)Utils.ZoomFactors[componentGizmoSizeIndex]);
    }

    /// <summary>
    /// Gets or sets whether the component gizmos keep their size on screen whatever their distance to the camera.
    /// </summary>
    public bool FixedSize
    {
        get => service.FixedSize;
        set => SetValue(service.FixedSize != value, () => service.FixedSize = value);
    }

    /// <summary>
    /// Shows the options saved with the scene, as the WPF <c>EntityGizmosViewModel</c> and <c>EntityTransformationViewModel</c>.
    /// </summary>
    internal void LoadSettings(SceneSettingsData settings)
    {
        TransformationGizmoSizeIndex = ToSizeIndex(settings.TransformationGizmoSize);
        ComponentGizmoSizeIndex = ToSizeIndex(settings.ComponentGizmoSize);
        FixedSize = settings.FixedSizeGizmos;
        // As in WPF, the settings name the hidden gizmos (CreateDefault names the ones hidden by default): the others show
        var hiddenGizmos = settings.HiddenGizmos;
        Gizmos.ForEach(x => x.IsActive = !hiddenGizmos.Contains(x.ComponentName));
        FallbackGizmo.IsActive = !hiddenGizmos.Contains(FallbackGizmo.ComponentName);
        // The WPF scene editor keeps this option with its lighting options
        LightProbeVolumesVisible = settings.LightProbeWireframe;
    }

    /// <summary>
    /// Writes the options in the settings of the scene, in the format of the WPF scene editor.
    /// </summary>
    internal void SaveSettings(SceneSettingsData settings)
    {
        settings.TransformationGizmoSize = Utils.ZoomFactors[TransformationGizmoSizeIndex];
        settings.ComponentGizmoSize = Utils.ZoomFactors[ComponentGizmoSizeIndex];
        settings.FixedSizeGizmos = FixedSize;
        var hiddenGizmos = settings.HiddenGizmos;
        hiddenGizmos.Clear();
        hiddenGizmos.AddRange(Gizmos.Where(x => !x.IsActive).Select(x => x.ComponentName));
        if (!FallbackGizmo.IsActive)
            hiddenGizmos.Add(FallbackGizmo.ComponentName);
        if (lightProbes is not null)
            settings.LightProbeWireframe = LightProbeVolumesVisible;
    }

    /// <summary>
    /// Finds the index of the size in <see cref="Utils.ZoomFactors"/>: WPF saves the factor of its slider.
    /// </summary>
    private static int ToSizeIndex(double size)
    {
        var index = 0;
        for (var i = 1; i < Utils.ZoomFactors.Length; ++i)
        {
            if (Math.Abs(Utils.ZoomFactors[i] - size) < Math.Abs(Utils.ZoomFactors[index] - size))
                index = i;
        }
        return index;
    }

    /// <summary>
    /// Gets whether the game draws the light probes: the "Light probe volumes" option shows only then.
    /// </summary>
    public bool HasLightProbes => lightProbes is not null;

    /// <summary>
    /// Gets or sets whether the volumes between the light probes of the scene show, as a yellow wireframe.
    /// </summary>
    public bool LightProbeVolumesVisible
    {
        get => lightProbes?.IsLightProbeVolumesVisible ?? false;
        set => SetValue(LightProbeVolumesVisible != value && lightProbes is not null, () => lightProbes!.IsLightProbeVolumesVisible = value);
    }

    private static bool IsHiddenByDefault(Type gizmoType)
    {
        return gizmoType.GetCustomAttributes(typeof(GizmoComponentAttribute), true).OfType<GizmoComponentAttribute>().Any(x => x.HiddenByDefault);
    }
}

/// <summary>
/// The toggle of the gizmos of one type of component.
/// </summary>
public sealed class EditorViewGizmoViewModel : DispatcherViewModel
{
    private readonly EditorGameComponentGizmoService service;
    private bool isActive;

    internal EditorViewGizmoViewModel(IViewModelServiceProvider serviceProvider, EditorGameComponentGizmoService service, Type componentType, bool isActive)
        : base(serviceProvider)
    {
        this.service = service;
        this.isActive = isActive;
        ComponentType = componentType;
        ComponentName = DisplayAttribute.GetDisplayName(componentType) ?? componentType.Name;
    }

    /// <summary>
    /// Gets the type of component whose gizmos this toggle shows or hides.
    /// </summary>
    public Type ComponentType { get; }

    /// <summary>
    /// Gets the display name of the type of component.
    /// </summary>
    public string ComponentName { get; }

    /// <summary>
    /// Gets or sets whether the gizmos of this type of component show.
    /// </summary>
    public bool IsActive
    {
        get => isActive;
        set => SetValue(ref isActive, value, () => service.ToggleGizmoVisibility(ComponentType, value));
    }
}
