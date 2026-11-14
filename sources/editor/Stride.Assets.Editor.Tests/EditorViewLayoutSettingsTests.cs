// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.AssetEditors;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Tools;
using Stride.Assets.Editor.AssetEditors.GameEditor;
using Stride.Assets.Editor.AssetEditors.GameEditor.Services;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Assets;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Settings;
using Stride.Editor.Engine;
using Stride.Engine.Processors;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The views of a scene editor go with the user settings of the package: the layout, the camera of each view and the
/// maximized view are saved when the editor closes and restored when the scene opens again, and the camera of the scene
/// settings stays the primary view's.
/// </summary>
public sealed class EditorViewLayoutSettingsTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "Stride.Assets.Editor.Tests", Guid.NewGuid().ToString("N"));

    public EditorViewLayoutSettingsTests()
    {
        Directory.CreateDirectory(directory);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(directory, true);
        }
        catch (IOException)
        {
            // Best effort
        }
    }

    [Fact]
    public void TheLayoutAndTheCamerasOfTheViewsRoundTripThroughTheSettingsOfTheScene()
    {
        var service = new FakeMultiViewService();
        var layout = CreateLayout(service);
        layout.Layout = EditorViewLayout.Quad;
        var (top, front, left) = (layout.Panes[0], layout.Panes[1], layout.Panes[2]);
        front.Camera = EditorViewCamera.Right;
        // The user moved the cameras of the views; the left view was never drawn, so the game has no camera for it yet
        var topCamera = new EditorGameViewCameraState(new Vector3(1, 50, 2), new Vector2(-MathUtil.PiOverTwo, 0), true, 25) { Target = new Vector3(1, 0, 2) };
        var rightCamera = new EditorGameViewCameraState(new Vector3(-50, 3, 0), new Vector2(0, -MathUtil.PiOverTwo), true, 12) { Target = new Vector3(0, 3, 0) };
        service.Cameras[top.Id] = topCamera;
        service.Cameras[front.Id] = rightCamera;
        service.Cameras.Remove(left.Id);
        layout.MaximizedPane = front;

        var settings = new SceneViewsSettingsData();
        layout.SaveSettings(settings);
        var reloaded = SaveAndLoad(settings);

        Assert.Equal(EditorViewLayout.Quad, reloaded.Layout);
        Assert.Equal(1, reloaded.MaximizedView);
        Assert.Equal([EditorViewCamera.Top, EditorViewCamera.Right, EditorViewCamera.Left], reloaded.Views.Select(x => x.Camera));
        Assert.Equal([true, true, false], reloaded.Views.Select(x => x.HasCameraState));

        // The scene opens again: the views come back with their cameras where they were
        var reopenedService = new FakeMultiViewService();
        var reopened = CreateLayout(reopenedService);
        reopened.LoadSettings(reloaded);

        Assert.Equal(EditorViewLayout.Quad, reopened.Layout);
        Assert.Equal([EditorViewCamera.Top, EditorViewCamera.Right, EditorViewCamera.Left, EditorViewCamera.Perspective], reopened.Panes.Select(x => x.Camera));
        Assert.Equal(IEditorGameMultiViewport.PrimaryView, reopened.Panes[3].Id);
        Assert.Same(reopened.Panes[1], reopened.MaximizedPane);
        Assert.Equal(topCamera, reopenedService.Cameras[reopened.Panes[0].Id]);
        Assert.Equal(rightCamera, reopenedService.Cameras[reopened.Panes[1].Id]);
        // A view saved without a camera starts from the camera of its title
        Assert.Equal((reopened.Panes[2].Id, EditorViewCamera.Left), reopenedService.Presets.Single());
    }

    [Fact]
    public void ASceneSavedWithOneViewOpensWithOneView()
    {
        var service = new FakeMultiViewService();
        var layout = CreateLayout(service);

        var settings = new SceneViewsSettingsData();
        layout.SaveSettings(settings);
        Assert.Equal(EditorViewLayout.Single, settings.Layout);
        Assert.Empty(settings.Views);
        Assert.Equal(-1, settings.MaximizedView);

        layout.LoadSettings(new SceneViewsSettingsData());
        Assert.Equal(EditorViewLayout.Single, layout.Layout);
        Assert.Equal([IEditorGameMultiViewport.PrimaryView], layout.Panes.Select(x => x.Id));
        Assert.Empty(service.Presets);
    }

    [Fact]
    public void TheSceneKeepsThePrimaryViewsCameraWhileAnotherViewIsActive()
    {
        var service = new FakeMultiViewService();
        var camera = new FakeCameraService();
        var serviceProvider = new ViewModelServiceProvider([new NullDispatcherService()]);
        var layout = new EditorViewLayoutViewModel(serviceProvider, service, null);
        var toolbar = new EditorViewToolbarViewModel(serviceProvider, new FakeToolService(), null, null, camera: camera, views: layout);
        layout.Layout = EditorViewLayout.Quad;
        var top = layout.Panes[0];

        // The pointer is over the top view: the editor camera is its camera, and the game keeps the primary view's
        var primary = new EditorGameViewCameraState(new Vector3(4, 2, 4), new Vector2(-0.25f, 0.75f), false, 10);
        var topCamera = new EditorGameViewCameraState(new Vector3(0, 50, 0), new Vector2(-MathUtil.PiOverTwo, 0), true, 30);
        camera.LoadSettings(topCamera.Position, topCamera.PitchYaw);
        service.PrimaryViewCamera = primary;
        service.Cameras[IEditorGameMultiViewport.PrimaryView] = primary;
        service.Cameras[top.Id] = topCamera;

        var settings = SceneSettingsData.CreateDefault();
        toolbar.SaveSettings(settings);

        // The scene's camera is the primary view's, the top view's camera goes with its view
        Assert.Equal(primary.Position, settings.CamPosition);
        Assert.Equal(primary.PitchYaw, settings.CamPitchYaw);
        Assert.Equal(CameraProjectionMode.Perspective, settings.CamProjection);
        var views = new SceneViewsSettingsData();
        layout.SaveSettings(views);
        var savedTop = views.Views[0];
        Assert.Equal((EditorViewCamera.Top, true, topCamera.Position, topCamera.PitchYaw, CameraProjectionMode.Orthographic, 30f),
            (savedTop.Camera, savedTop.HasCameraState, savedTop.CamPosition, savedTop.CamPitchYaw, savedTop.CamProjection, savedTop.CamOrthographicSize));
        toolbar.Destroy();
    }

    private static EditorViewLayoutViewModel CreateLayout(FakeMultiViewService service)
        => new(new ViewModelServiceProvider([new NullDispatcherService()]), service, null);

    /// <summary>
    /// Writes the views of a scene in a package user settings file, and reads them back.
    /// </summary>
    private SceneViewsSettingsData SaveAndLoad(SceneViewsSettingsData settings)
    {
        var path = Path.Combine(directory, "MyGame.sdpkg.user");
        var sceneId = AssetId.New();
        var container = new SettingsContainer();
        var key = new SettingsKey<Dictionary<AssetId, SceneViewsSettingsData>>(PackageSceneViews.Views.Name, container, () => new Dictionary<AssetId, SceneViewsSettingsData>());
        var profile = container.CreateSettingsProfile(true);
        key.SetValue(new Dictionary<AssetId, SceneViewsSettingsData> { [sceneId] = settings }, profile);
        Assert.True(container.SaveSettingsProfile(profile, path));

        var reloadedContainer = new SettingsContainer();
        var reloadedKey = new SettingsKey<Dictionary<AssetId, SceneViewsSettingsData>>(PackageSceneViews.Views.Name, reloadedContainer, () => new Dictionary<AssetId, SceneViewsSettingsData>());
        var reloaded = reloadedContainer.LoadSettingsProfile(path, false, null, false);
        Assert.NotNull(reloaded);
        return reloadedKey.GetValue(reloaded, true)[sceneId];
    }

    private sealed class FakeMultiViewService : IEditorGameMultiViewViewModelService
    {
        public Dictionary<int, EditorGameViewCameraState> Cameras { get; } = [];

        public List<(int Id, EditorViewCamera Camera)> Presets { get; } = [];

        public bool IsAvailable => true;

        public EditorGameViewCameraState? PrimaryViewCamera { get; set; }

        public event EventHandler<EditorGameViewCameraInfo>? ActiveViewChanged { add { } remove { } }

        public void SetViewCamera(int viewId, EditorViewCamera camera)
        {
            Presets.Add((viewId, camera));
            // The game places a new view's camera from the primary view's
            Cameras[viewId] = new EditorGameViewCameraState(Vector3.Zero, Vector2.Zero, camera != EditorViewCamera.Perspective, 10);
        }

        public IReadOnlyDictionary<int, EditorGameViewCameraState> GetViewCameras() => new Dictionary<int, EditorGameViewCameraState>(Cameras);

        public void SetViewCamera(int viewId, EditorGameViewCameraState state) => Cameras[viewId] = state;

        public void RemoveView(int viewId) => Cameras.Remove(viewId);
    }

    private sealed class FakeCameraService : IEditorGameCameraViewModelService
    {
        private (Vector3 Position, Vector2 PitchYaw) camera;

        public float SceneUnit { get; set; } = 1;

        public float MoveSpeed { get; set; } = 1;

        public void SetOrthographicProjection(bool value) { }

        public void SetOrthographicSize(float value) { }

        public void SetNearPlane(float value) { }

        public void SetFarPlane(float value) { }

        public void SetFieldOfView(float value) { }

        public void ResetCamera() { }

        public void ResetCameraOrientation(CameraOrientation orientation) { }

        public void LoadSettings(Vector3 position, Vector2 pitchYaw) => camera = (position, pitchYaw);

        public (Vector3 Position, Vector2 PitchYaw) SaveSettings() => camera;
    }

    private sealed class FakeToolService : IEditorGameToolViewModelService
    {
        public IReadOnlyList<IEditorViewTool> Tools => [];

        public event EventHandler? ToolsChanged { add { } remove { } }

        public event EventHandler<EditorViewContextMenuRequest>? ContextMenuRequested { add { } remove { } }

        public event EventHandler<IEditorViewTool>? ActiveToolChanged { add { } remove { } }

        public event EventHandler<IEditorViewTool>? ToolFailed { add { } remove { } }

        public Task<(IEditorViewTool ActiveTool, IReadOnlyList<IEditorViewTool> FailedTools)> GetStateAsync() => throw new NotSupportedException();

        public Task<bool> RunMenuEntryAsync(IEditorViewTool tool, EditorViewMenuEntry entry) => Task.FromResult(false);

        public Task<bool> ActivateAsync(IEditorViewTool tool) => Task.FromResult(false);

        public Task SetSpaceAsync(TransformationSpace space) => Task.CompletedTask;

        public Task SetGizmoSizeAsync(float size) => Task.CompletedTask;

        public Task SetSnapAsync(Transformation transformation, float value, bool isActive) => Task.CompletedTask;
    }
}
