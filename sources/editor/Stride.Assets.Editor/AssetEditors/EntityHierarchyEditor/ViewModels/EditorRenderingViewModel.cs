// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Editor.AssetEditors.GameEditor.Services;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Materials;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.ViewModels;

/// <summary>
/// The render mode of the view of a scene or prefab editor: the editor, the game preview, or one stream of the materials.
/// </summary>
public sealed class EditorRenderingViewModel : DispatcherViewModel
{
    private readonly IEditorGameController controller;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditorRenderingViewModel"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for this view model.</param>
    /// <param name="controller">The controller of the editor game, which has the render mode service.</param>
    public EditorRenderingViewModel(IViewModelServiceProvider serviceProvider, IEditorGameController controller)
        : base(serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(controller);
        this.controller = controller;
        AvailableRenderModes = GetAvailableRenderModes();
    }

    /// <summary>
    /// Gets or sets the render mode of the view.
    /// </summary>
    public EditorRenderMode RenderMode { get => Service.RenderMode; set => SetValue(Service.RenderMode != value, () => Service.RenderMode = value); }

    /// <summary>
    /// Gets the render modes: the editor, the game preview, a separator (named "-"), then the streams of the materials.
    /// </summary>
    public IReadOnlyList<EditorRenderMode> AvailableRenderModes { get; }

    private IEditorGameRenderModeViewModelService Service => controller.GetService<IEditorGameRenderModeViewModelService>();

    /// <summary>
    /// Shows the render mode saved with the scene: the material stream it shows, or the editor.
    /// </summary>
    public void LoadSettings(SceneSettingsData sceneSettings)
    {
        // Default to the first render mode (Editor), or the stream the scene showed
        var activeRenderMode = AvailableRenderModes[0];
        if (!string.IsNullOrEmpty(sceneSettings.ActiveStream))
        {
            var streamRenderMode = AvailableRenderModes.FirstOrDefault(x => x.Mode == SceneRenderMode.SingleStream && x.StreamDescriptor.Name == sceneSettings.ActiveStream);
            if (streamRenderMode is not null)
                activeRenderMode = streamRenderMode;
        }

        RenderMode = activeRenderMode;
    }

    /// <summary>
    /// Writes the render mode in the settings of the scene.
    /// </summary>
    public void SaveSettings(SceneSettingsData sceneSettings)
    {
        // The game preview is not saved on purpose: the editor opens in the editor mode (better for the user, and prevents crashes)
        sceneSettings.RenderMode = RenderMode.Mode;
        sceneSettings.ActiveStream = RenderMode.StreamDescriptor?.Name ?? "";
    }

    private static List<EditorRenderMode> GetAvailableRenderModes()
    {
        var renderModes = new List<EditorRenderMode>
        {
            EditorRenderMode.DefaultEditor,
            EditorRenderMode.DefaultGamePreview,
            // Separator
            new("-"),
        };
        foreach (var materialMode in MaterialStreamFactory.GetAvailableStreams())
        {
            renderModes.Add(new EditorRenderMode(materialMode));
        }
        return renderModes;
    }
}
