// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Stride.Assets.Editor.AssetEditors;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Editor.AssetEditors.GameEditor.Services;
using Stride.Assets.Editor.AssetEditors.GameEditor.ViewModels;
using Stride.Assets.Editor.ViewModels;
using Stride.Core;
using Stride.Core.Assets.Quantum;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Engine.Processors;
using Stride.Editor.EditorGame.ContentLoader;
using Stride.Editor.EditorGame.ViewModels;
using Stride.Editor.Engine;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using static Stride.GameStudio.Avalonia.Tests.SceneViewContextMenuTests;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The view toolbar of the scene editor has the camera, grid, lighting and render mode menus of the WPF scene editor, on the
/// shared view models.
/// </summary>
public sealed class SceneViewOptionsMenuTests
{
    [AvaloniaFact]
    public async Task TheToolbarShowsTheViewOptionsMenusAndTheirValuesReachTheGame()
    {
        await using var scene = await SceneTreeDropTests.OpenSceneAsync();
        var editor = scene.Editor;
        var controller = new FakeController();
        var grid = new FakeGridService();
        var camera = new FakeCameraService();
        var renderMode = new FakeRenderModeService();
        var preview = new FakeCameraPreviewService();
        controller.Services.AddRange([grid, camera, renderMode, preview, new FakeLightProbeService()]);
        var serviceProvider = editor.ServiceProvider;
        var toolbar = new EditorViewToolbarViewModel(serviceProvider, new FakeToolService(), null, null, camera: camera,
            cameraOptions: new EditorCameraViewModel(serviceProvider, controller), grid: new EditorGridViewModel(serviceProvider, controller),
            rendering: new EditorRenderingViewModel(serviceProvider, controller), lighting: new EditorLightingViewModel(serviceProvider, controller, editor),
            cameraPreview: preview);
        editor.ViewOptions = toolbar;
        try
        {
            await WindowTestHelper.PumpAsync();
            var view = scene.View;
            var gridButton = view.FindControl<Button>("GridButton")!;
            var lightingButton = view.FindControl<Button>("LightingButton")!;
            var cameraButton = view.FindControl<Button>("CameraButton")!;
            var renderModes = view.FindControl<ComboBox>("RenderModeBox")!;
            Assert.True(gridButton.IsEffectivelyVisible);
            Assert.True(lightingButton.IsEffectivelyVisible);
            Assert.True(cameraButton.IsEffectivelyVisible);
            Assert.True(renderModes.IsEffectivelyVisible);

            // The settings of the scene show in the menus
            toolbar.LoadSettings(new SceneSettingsData { CamVerticalFieldOfView = 60, GridVisible = true, CameraPreviewVisible = false });
            Assert.Equal(60f, camera.FieldOfView);

            // The camera menu: the orthographic projection reaches the camera of the game
            var (cameraFlyout, cameraMenu) = OpenFlyout(cameraButton);
            cameraMenu.GetLogicalDescendants().OfType<RadioButton>().Single(x => Equals(x.Content, "Orthographic")).IsChecked = true;
            Assert.True(camera.Orthographic);
            Assert.True(toolbar.Camera!.OrthographicProjection);
            cameraFlyout.Hide();

            // The grid menu hides the grid
            var (gridFlyout, gridMenu) = OpenFlyout(gridButton);
            gridMenu.GetLogicalDescendants().OfType<CheckBox>().Single(x => Equals(x.Content, "Show grid")).IsChecked = false;
            Assert.False(grid.IsActive);
            gridFlyout.Hide();

            // The lighting menu has the light probe and cubemap commands
            var (lightingFlyout, lightingMenu) = OpenFlyout(lightingButton);
            Assert.Equal(["Compute", "Reset", "Capture"], lightingMenu.GetLogicalDescendants().OfType<Button>()
                .Select(x => x.GetLogicalDescendants().OfType<TextBlock>().Single().Text));
            Assert.All(lightingMenu.GetLogicalDescendants().OfType<Button>(), x => Assert.NotNull(x.Command));
            lightingFlyout.Hide();

            // A material stream picked in the render mode list reaches the game
            Assert.Equal(EditorRenderMode.DefaultEditor, renderModes.SelectedItem);
            renderModes.SelectedIndex = 3;
            Assert.Same(toolbar.Rendering!.AvailableRenderModes[3], renderMode.RenderMode);

            // The camera preview option goes with the gizmo options
            toolbar.DisplayCameraPreview = true;
            Assert.True(preview.IsActive);
            var saved = new SceneSettingsData();
            toolbar.SaveSettings(saved);
            Assert.Equal((CameraProjectionMode.Orthographic, false, true), (saved.CamProjection, saved.GridVisible, saved.CameraPreviewVisible));
        }
        finally
        {
            DetachToolbar(scene, toolbar);
        }
    }

    private static (Flyout Flyout, Control Content) OpenFlyout(Button button)
    {
        // The content binds to the toolbar while the flyout is open
        var flyout = Assert.IsType<Flyout>(button.Flyout);
        flyout.ShowAt(button);
        return (flyout, Assert.IsAssignableFrom<Control>(flyout.Content));
    }

    private sealed class FakeGridService : IEditorGameGridViewModelService
    {
        public bool IsActive { get; set; } = true;

        public Color3 Color { get; set; }

        public float Alpha { get; set; } = 0.35f;

        public int AxisIndex { get; set; } = 2;
    }

    private sealed class FakeRenderModeService : IEditorGameRenderModeViewModelService
    {
        public EditorRenderMode RenderMode { get; set; } = EditorRenderMode.DefaultEditor;
    }

    private sealed class FakeCameraPreviewService : IEditorGameCameraPreviewViewModelService
    {
        public bool IsActive { get; set; }
    }

    private sealed class FakeLightProbeService : IEditorGameLightProbeService
    {
        public bool IsLightProbeVolumesVisible { get; set; }

        public Task<Dictionary<Guid, List<Color3>>> RequestLightProbesStep() => Task.FromResult(new Dictionary<Guid, List<Color3>>());

        public Task UpdateLightProbeCoefficients() => Task.CompletedTask;
    }

    private sealed class FakeCameraService : IEditorGameCameraViewModelService
    {
        public bool Orthographic { get; private set; }

        public float FieldOfView { get; private set; }

        public float SceneUnit { get; set; } = 1;

        public float MoveSpeed { get; set; } = 1;

        public void SetOrthographicProjection(bool value) => Orthographic = value;

        public void SetOrthographicSize(float value) { }

        public void SetNearPlane(float value) { }

        public void SetFarPlane(float value) { }

        public void SetFieldOfView(float value) => FieldOfView = value;

        public void ResetCamera() { }

        public void ResetCameraOrientation(CameraOrientation orientation) { }

        public void LoadSettings(Vector3 position, Vector2 pitchYaw) { }

        public (Vector3 Position, Vector2 PitchYaw) SaveSettings() => default;
    }

    /// <summary>
    /// A game controller without a game, with the services the view models reach.
    /// </summary>
    private sealed class FakeController : IEditorGameController
    {
        public List<IEditorGameViewModelService> Services { get; } = [];

        public IEditorContentLoader Loader => throw new NotSupportedException();

        public Logger Logger { get; } = GlobalLogger.GetLogger(nameof(SceneViewOptionsMenuTests));

        public Task GameContentLoaded => Task.CompletedTask;

        public Task GameEnded => Task.CompletedTask;

        public SessionNodeContainer GameSideNodeContainer => throw new NotSupportedException();

        public object View => throw new NotSupportedException();

        public IEditorGameViewport Viewport => throw new NotSupportedException();

        public Task<bool> StartGame() => Task.FromResult(true);

        public Task<bool> CreateScene() => Task.FromResult(true);

        public void OnHideGame() { }

        public void OnShowGame() { }

        public object? FindGameSidePart(AbsoluteId partId) => null;

        public T GetService<T>() where T : IEditorGameViewModelService => Services.OfType<T>().FirstOrDefault()!;

        public IReadOnlyList<T> GetServices<T>() where T : IEditorGameViewModelService => Services.OfType<T>().ToList();

        public void TriggerActiveRenderStageReevaluation() { }

        public void Destroy() { }

        public void Invoke(Action callback) => callback();

        public TResult Invoke<TResult>(Func<TResult> callback) => callback();

        public Task InvokeAsync(Action callback, CancellationToken token = default) { callback(); return Task.CompletedTask; }

        public Task LowPriorityInvokeAsync(Action callback, CancellationToken token = default) => InvokeAsync(callback, token);

        public Task<TResult> InvokeAsync<TResult>(Func<TResult> callback, CancellationToken token = default) => Task.FromResult(callback());

        public Task InvokeTask(Func<Task> task, CancellationToken token = default) => task();

        public Task<TResult> InvokeTask<TResult>(Func<Task<TResult>> task, CancellationToken token = default) => task();

        public bool CheckAccess() => true;

        public void EnsureAccess(bool inDispatcherThread = true) { }
    }
}
