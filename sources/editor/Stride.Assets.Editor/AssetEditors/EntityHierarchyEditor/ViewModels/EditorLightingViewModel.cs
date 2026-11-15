// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Editor.AssetEditors.GameEditor.Services;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.LightProbes;

namespace Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.ViewModels;

/// <summary>
/// The lighting options of the view of a scene or prefab editor: computing and resetting the light probes, and capturing a
/// cubemap from the camera.
/// </summary>
public sealed class EditorLightingViewModel : DispatcherViewModel
{
    private readonly IEditorGameController controller;
    private readonly EntityHierarchyEditorViewModel editor;
    private int lightProbeBounces = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditorLightingViewModel"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for this view model.</param>
    /// <param name="controller">The controller of the editor game, which has the light probe and cubemap services.</param>
    /// <param name="editor">The scene or prefab editor, whose light probes are computed.</param>
    public EditorLightingViewModel(IViewModelServiceProvider serviceProvider, IEditorGameController controller, EntityHierarchyEditorViewModel editor)
        : base(serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(editor);
        this.controller = controller;
        this.editor = editor;

        RequestLightProbesComputeCommand = new AnonymousTaskCommand(ServiceProvider, () => RebuildLightProbesAsync(LightProbeBounces));
        RequestLightProbesResetCommand = new AnonymousTaskCommand(ServiceProvider, () => RebuildLightProbesAsync(0));
        CaptureCubemapCommand = new AnonymousTaskCommand(ServiceProvider, CaptureCubemapAsync);
    }

    /// <summary>
    /// Gets or sets how many bounces of light the computation of the light probes takes.
    /// </summary>
    public int LightProbeBounces { get => lightProbeBounces; set => SetValue(ref lightProbeBounces, Math.Max(0, value)); }

    /// <summary>
    /// Gets or sets whether the volumes between the light probes show.
    /// </summary>
    public bool LightProbeWireframeVisible
    {
        get => LightProbeService?.IsLightProbeVolumesVisible ?? false;
        set => SetValue(LightProbeWireframeVisible != value && LightProbeService is not null, () => LightProbeService!.IsLightProbeVolumesVisible = value);
    }

    /// <summary>
    /// Gets a command that computes the light probes of the scene, with <see cref="LightProbeBounces"/> bounces (one undo entry).
    /// </summary>
    public ICommandBase RequestLightProbesComputeCommand { get; }

    /// <summary>
    /// Gets a command that resets the light probes of the scene to black (one undo entry).
    /// </summary>
    public ICommandBase RequestLightProbesResetCommand { get; }

    /// <summary>
    /// Gets a command that captures a cubemap from the camera of the view, and saves it as a DDS texture.
    /// </summary>
    public ICommandBase CaptureCubemapCommand { get; }

    private IEditorGameLightProbeService? LightProbeService => controller.GetService<IEditorGameLightProbeService>();

    private IEditorGameCubemapService? CubemapService => controller.GetService<IEditorGameCubemapService>();

    /// <summary>
    /// Shows the lighting options saved with the scene.
    /// </summary>
    public void LoadSettings(SceneSettingsData settings)
    {
        LightProbeWireframeVisible = settings.LightProbeWireframe;
        LightProbeBounces = settings.LightProbeBounces;
    }

    /// <summary>
    /// Writes the lighting options in the settings of the scene.
    /// </summary>
    public void SaveSettings(SceneSettingsData settings)
    {
        settings.LightProbeWireframe = LightProbeWireframeVisible;
        settings.LightProbeBounces = LightProbeBounces;
    }

    /// <summary>
    /// Resets the coefficients of the light probes of the scene, then computes the given number of bounces, in one transaction.
    /// </summary>
    internal async Task RebuildLightProbesAsync(int bounces)
    {
        if (LightProbeService is not { } lightProbeService)
            return;

        using var transaction = editor.UndoRedoService.CreateTransaction();
        editor.UndoRedoService.SetName(transaction, bounces > 0 ? "Capture LightProbes" : "Reset LightProbes");

        // Reset the coefficients
        var zeroCoefficients = Enumerable.Repeat(default(Color3), LightProbeGenerator.LambertHamonicOrder * LightProbeGenerator.LambertHamonicOrder).ToList();
        foreach (var entity in Entities())
        {
            foreach (var lightProbe in entity.Components.OfType<LightProbeComponent>())
            {
                var assetNode = editor.Session.AssetNodeContainer.GetOrCreateNode(lightProbe);
                assetNode[nameof(LightProbeComponent.Coefficients)].Update(new List<Color3>(zeroCoefficients));
            }
        }
        await lightProbeService.UpdateLightProbeCoefficients();

        for (var bounce = 0; bounce < bounces; ++bounce)
        {
            // Compute the coefficients, then copy them to the asset side
            var result = await lightProbeService.RequestLightProbesStep();
            foreach (var entity in Entities().Where(x => result.ContainsKey(x.AssetSideEntity.Id)))
            {
                // TODO: Use LightProbe Id instead of entity id once copy/paste and duplicate properly remap them
                if (entity.Components.OfType<LightProbeComponent>().FirstOrDefault() is { } lightProbe)
                {
                    var assetNode = editor.Session.AssetNodeContainer.GetOrCreateNode(lightProbe);
                    assetNode[nameof(LightProbeComponent.Coefficients)].Update(result[lightProbe.Entity.Id]);
                }
            }
            await lightProbeService.UpdateLightProbeCoefficients();
        }
    }

    private IEnumerable<EntityViewModel> Entities() => editor.HierarchyRoot.Children.SelectDeep(x => x.Children).OfType<EntityViewModel>();

    private async Task CaptureCubemapAsync()
    {
        if (CubemapService is not { } cubemapService)
            return;

        var filepath = await ServiceProvider.Get<IDialogService>().SaveFilePickerAsync(
            editor.Session.SolutionPath?.GetFullDirectory(),
            [new FilePickerFilter("DDS texture") { Patterns = ["*.dds"] }],
            "dds");
        if (filepath is null)
            return;

        using var image = await cubemapService.CaptureCubemap();
        using var file = File.Create(filepath.ToOSPath());
        image.Save(file, ImageFileType.Dds);
    }
}
