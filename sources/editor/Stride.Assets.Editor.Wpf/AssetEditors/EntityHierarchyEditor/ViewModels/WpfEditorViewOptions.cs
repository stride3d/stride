// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Stride.Assets.Editor.EditorGame;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.AssetEditors.GameEditor.ViewModels;
using Stride.Assets.Presentation.View;
using Stride.Assets.Presentation.ViewModel;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Presentation.Quantum.NodePresenters;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.ViewModels;
using Stride.Assets.Editor.AssetEditors.GameEditor.Services;
using Stride.Assets.Editor.AssetEditors.GameEditor.ViewModels;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.ViewModels;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;

/// <summary>
/// The view options of the WPF scene and prefab editors: the camera, transformation, grid, overlay, lighting, rendering
/// and gizmo options of the toolbar, the selection mask and material selection toggles, the camera preview of a scene,
/// and what the property grid of an entity shows for its camera components.
/// </summary>
public sealed class WpfEditorViewOptions : DispatcherViewModel, IEditorViewOptions
{
    private readonly EntityHierarchyEditorViewModel editor;
    private readonly EntityHierarchyEditorController controller;
    private readonly CameraComponentViewModel cameraComponent;
    private readonly DebugEntityHierarchyEditorUserControl debugPage;

    /// <summary>
    /// Initializes a new instance of the <see cref="WpfEditorViewOptions"/> class.
    /// </summary>
    /// <param name="editor">The scene or prefab editor.</param>
    /// <param name="controller">The controller of its game, whose scene is created.</param>
    public WpfEditorViewOptions(EntityHierarchyEditorViewModel editor, EntityHierarchyEditorController controller)
        : base(editor.ServiceProvider)
    {
        this.editor = editor;
        this.controller = controller;
        Camera = new EditorCameraViewModel(ServiceProvider, controller);
        Transform = new EntityTransformationViewModel(ServiceProvider, controller);
        Grid = new EditorGridViewModel(ServiceProvider, controller);
        Overlays = new EditorOverlaysViewModel(ServiceProvider, controller);
        Lighting = new EditorLightingViewModel(ServiceProvider, controller, editor);
        Rendering = new EditorRenderingViewModel(ServiceProvider, controller);
        EntityGizmos = new EntityGizmosViewModel(ServiceProvider, controller);
        cameraComponent = new CameraComponentViewModel(ServiceProvider);

        // The speed and projection keys of the game go through the camera options, which show and save them
        if (controller.GetService<EditorGameEntityCameraService>() is { } camera)
            camera.Options = Camera;

        debugPage = new DebugEntityHierarchyEditorUserControl(editor);
        EditorDebugTools.RegisterDebugPage(debugPage);
        editor.Session.AssetCollection.SelectedAssets.CollectionChanged += SelectedAssetsChanged;
    }

    /// <summary>
    /// Gets the camera options.
    /// </summary>
    public EditorCameraViewModel Camera { get; }

    /// <summary>
    /// Gets the transformation options: the active gizmo, its space and snapping.
    /// </summary>
    public EntityTransformationViewModel Transform { get; }

    /// <summary>
    /// Gets the grid options.
    /// </summary>
    public EditorGridViewModel Grid { get; }

    /// <summary>
    /// Gets the overlay options.
    /// </summary>
    public EditorOverlaysViewModel Overlays { get; }

    /// <summary>
    /// Gets the lighting options: light probes and cubemaps.
    /// </summary>
    public EditorLightingViewModel Lighting { get; }

    /// <summary>
    /// Gets the rendering options.
    /// </summary>
    public EditorRenderingViewModel Rendering { get; }

    /// <summary>
    /// Gets the gizmo options of the components.
    /// </summary>
    public EntityGizmosViewModel EntityGizmos { get; }

    /// <summary>
    /// Gets or sets whether the selected entities show their selection mask.
    /// </summary>
    public bool DisplaySelectionMask
    {
        get => Selection?.DisplaySelectionMask ?? false;
        set
        {
            if (Selection is { } selection)
                SetValue(selection.DisplaySelectionMask != value, () => selection.DisplaySelectionMask = value);
        }
    }

    /// <summary>
    /// Gets or sets whether a click in the game selects a material instead of an entity.
    /// </summary>
    public bool MaterialSelectionMode
    {
        get => MaterialHighlight?.IsActive ?? false;
        set
        {
            if (MaterialHighlight is { } highlight)
                SetValue(highlight.IsActive != value, () => highlight.IsActive = value);
        }
    }

    /// <summary>
    /// Gets or sets whether the game shows the preview of the selected camera (scene editor only).
    /// </summary>
    public bool DisplayCameraPreview
    {
        get => Preview?.IsActive ?? false;
        set
        {
            if (Preview is { } preview)
                SetValue(preview.IsActive != value, () => preview.IsActive = value);
        }
    }

    private IEditorGameSelectionViewModelService? Selection => controller.GetService<IEditorGameSelectionViewModelService>();

    private EditorGameMaterialHighlightService? MaterialHighlight => controller.GetService<EditorGameMaterialHighlightService>();

    private IEditorGameCameraPreviewViewModelService? Preview => controller.GetService<IEditorGameCameraPreviewViewModelService>();

    /// <inheritdoc/>
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void LoadSettings(SceneSettingsData settings)
    {
        Camera.LoadSettings(settings);
        Transform.LoadSettings(settings);
        EntityGizmos.LoadSettings(settings);
        DisplaySelectionMask = settings.SelectionMaskVisible;
        Grid.LoadSettings(settings);
        Lighting.LoadSettings(settings);
        MaterialSelectionMode = false;
        Overlays.LoadSettings(settings);
        Rendering.LoadSettings(settings);
        // The editor always opens in the editor render mode
        Rendering.RenderMode = EditorRenderMode.DefaultEditor;
        if (Preview is not null)
            DisplayCameraPreview = settings.CameraPreviewVisible;
    }

    /// <inheritdoc/>
    public void SaveSettings(SceneSettingsData settings)
    {
        Camera.SaveSettings(settings);
        Transform.SaveSettings(settings);
        settings.SelectionMaskVisible = DisplaySelectionMask;
        Grid.SaveSettings(settings);
        Lighting.SaveSettings(settings);
        Overlays.SaveSettings(settings);
        Rendering.SaveSettings(settings);
        EntityGizmos.SaveSettings(settings);
        if (Preview is not null)
            settings.CameraPreviewVisible = DisplayCameraPreview;
    }

    /// <inheritdoc/>
    public Vector3 SnapPosition(Vector3 position)
    {
        return Transform.TranslationSnap.IsActive ? MathUtil.Snap(position, Transform.TranslationSnap.Value) : position;
    }

    /// <inheritdoc/>
    /// <remarks>The model components follow their model with the shared <c>ModelComponentChangeHandler</c> of the editor.</remarks>
    public void OnEntityLinked(EntityViewModel entity)
    {
    }

    /// <inheritdoc/>
    public void OnEntityUnlinked(EntityViewModel entity)
    {
    }

    /// <summary>
    /// Finalizes the property grid of the given entity, for its camera components.
    /// </summary>
    public void FinalizeNodePresenterTree(EntityViewModel entity, IAssetNodePresenter root)
    {
        cameraComponent.FinalizeNodePresenterTree(root);
    }

    /// <inheritdoc/>
    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(WpfEditorViewOptions));

        editor.Session.AssetCollection.SelectedAssets.CollectionChanged -= SelectedAssetsChanged;
        EditorDebugTools.UnregisterDebugPage(debugPage);
        if (controller.GetService<EditorGameEntityCameraService>() is { } camera && camera.Options == Camera)
            camera.Options = null;
        Overlays.Destroy();
        Transform.Destroy();
        cameraComponent.Destroy();
        base.Destroy();
    }

    private void SelectedAssetsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        controller.GetService<IEditorGameAssetHighlighterViewModelService>()?.HighlightAssets(editor.Session.AssetCollection.SelectedAssets);
    }
}

/// <summary>
/// Creates the view options of the WPF scene and prefab editors.
/// </summary>
public sealed class WpfEditorViewOptionsFactory : IEditorViewOptionsFactory
{
    /// <inheritdoc/>
    public IEditorViewOptions? CreateViewOptions(EntityHierarchyEditorViewModel editor, EntityHierarchyEditorController controller)
    {
        return new WpfEditorViewOptions(editor, controller);
    }
}
