// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.AssetEditors;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Editor.AssetEditors.GameEditor.Services;
using Stride.Assets.Editor.AssetEditors.GameEditor.ViewModels;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Entities;
using Stride.Core.Assets;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Processors;
using Stride.Rendering.LightProbes;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The view options that both editors share: the grid, the render mode, the lighting (light probes) and the camera, their
/// values reaching the services of the game and their round trip through the settings of the scene.
/// </summary>
public sealed class SceneViewOptionsTests
{
    [Fact]
    public async Task TheGridOptionsReachTheGridServiceAndRoundTripThroughTheSettings()
    {
        await using var fixture = await EntityHierarchyEditorTests.OpenSceneAsync();
        using var controller = new FakeEditorGameController(fixture.Session);
        var service = new FakeGridService();
        controller.Services.Add(service);
        var grid = new EditorGridViewModel(fixture.ServiceProvider, controller);

        grid.LoadSettings(new SceneSettingsData { GridVisible = false, GridOpacity = 0.5f, GridAxisIndex = (int)(ViewportGridAxis.X | ViewportGridAxis.Z) });
        Assert.False(service.IsActive);
        Assert.Equal(0.5f, service.Alpha);
        Assert.True(grid.IsXAxisVisible);
        Assert.False(grid.IsYAxisVisible);
        Assert.True(grid.IsZAxisVisible);

        // An axis checked in the toolbar adds its flag, and the toggle shows the grid
        grid.IsYAxisVisible = true;
        grid.IsXAxisVisible = false;
        Assert.Equal((int)(ViewportGridAxis.Y | ViewportGridAxis.Z), service.AxisIndex);
        grid.ToggleCommand.Execute();
        Assert.True(service.IsActive);

        var saved = new SceneSettingsData();
        grid.SaveSettings(saved);
        Assert.Equal((true, 0.5f, 6), (saved.GridVisible, saved.GridOpacity, saved.GridAxisIndex));
    }

    [Fact]
    public async Task TheRenderModesListTheMaterialStreamsAndSaveTheActiveOne()
    {
        await using var fixture = await EntityHierarchyEditorTests.OpenSceneAsync();
        using var controller = new FakeEditorGameController(fixture.Session);
        var service = new FakeRenderModeService();
        controller.Services.Add(service);
        var rendering = new EditorRenderingViewModel(fixture.ServiceProvider, controller);

        // The editor, the game preview, a separator, then the streams of the materials
        Assert.Same(EditorRenderMode.DefaultEditor, rendering.AvailableRenderModes[0]);
        Assert.Same(EditorRenderMode.DefaultGamePreview, rendering.AvailableRenderModes[1]);
        Assert.True(rendering.AvailableRenderModes[2].IsSeparator);
        var streams = rendering.AvailableRenderModes.Skip(3).ToList();
        Assert.NotEmpty(streams);
        Assert.All(streams, x => Assert.Equal(SceneRenderMode.SingleStream, x.Mode));

        // A stream picked in the toolbar reaches the game and is saved by name
        rendering.RenderMode = streams[0];
        Assert.Same(streams[0], service.RenderMode);
        var saved = new SceneSettingsData();
        rendering.SaveSettings(saved);
        Assert.Equal((SceneRenderMode.SingleStream, streams[0].Name), (saved.RenderMode, saved.ActiveStream));

        // The saved stream comes back; an unknown one falls back to the editor
        rendering.RenderMode = EditorRenderMode.DefaultEditor;
        rendering.LoadSettings(saved);
        Assert.Same(streams[0], rendering.RenderMode);
        rendering.LoadSettings(new SceneSettingsData { ActiveStream = "No such stream" });
        Assert.Same(EditorRenderMode.DefaultEditor, rendering.RenderMode);
    }

    [Fact]
    public async Task TheCameraOptionsApplyEverySavedCameraSetting()
    {
        await using var fixture = await EntityHierarchyEditorTests.OpenSceneAsync();
        using var controller = new FakeEditorGameController(fixture.Session);
        var service = new FakeCameraService();
        controller.Services.Add(service);
        var camera = new EditorCameraViewModel(fixture.ServiceProvider, controller);

        var settings = new SceneSettingsData
        {
            CamPosition = new Vector3(1, 2, 3), CamPitchYaw = new Vector2(0.5f, 1.5f), CamProjection = CameraProjectionMode.Orthographic,
            CamOrthographicSize = 20, CamNearClipPlane = 0.5f, CamFarClipPlane = 500, CamVerticalFieldOfView = 60, SceneUnit = 2, CamMoveSpeed = 10,
        };
        camera.LoadSettings(settings);
        Assert.Equal((new Vector3(1, 2, 3), new Vector2(0.5f, 1.5f)), (service.Position, service.PitchYaw));
        Assert.Equal((true, 20f, 0.5f, 500f, 60f, 2f, 10f), (service.Orthographic, service.OrthographicSize, service.NearPlane, service.FarPlane, service.FieldOfView, service.SceneUnit, service.MoveSpeed));

        // The speed slider steps through the speeds of the WPF editor
        camera.MoveSpeedIndex++;
        Assert.Equal(15f, service.MoveSpeed);
        var saved = new SceneSettingsData();
        camera.SaveSettings(saved);
        Assert.Equal((CameraProjectionMode.Orthographic, 20f, 0.5f, 500f, 60f, 15f), (saved.CamProjection, saved.CamOrthographicSize, saved.CamNearClipPlane, saved.CamFarClipPlane, saved.CamVerticalFieldOfView, saved.CamMoveSpeed));
        Assert.Equal(new Vector3(1, 2, 3), saved.CamPosition);
    }

    [Fact]
    public async Task ComputingTheLightProbesCopiesEachBounceToTheAssetInOneUndoableOperation()
    {
        await using var fixture = await EntityHierarchyEditorTests.OpenSceneAsync();
        var editor = await EntityHierarchyEditorTests.OpenEditorAsync(fixture);
        using var controller = new FakeEditorGameController(fixture.Session);
        var lightProbes = new FakeLightProbeService();
        controller.Services.Add(lightProbes);
        var lighting = new EditorLightingViewModel(fixture.ServiceProvider, controller, editor);

        // A light probe in the scene, added as the editor adds entities
        var probe = new LightProbeComponent();
        var entity = new Entity("Probe") { probe };
        var design = new EntityDesign(entity);
        using (var transaction = fixture.Session.UndoRedoService.CreateTransaction())
        {
            EntityHierarchyEditorTests.PropertyGraph(editor).AddPartToAsset(new AssetPartCollection<EntityDesign, Entity> { design }, design, null, 0);
            fixture.Session.UndoRedoService.SetName(transaction, "Add entity");
        }
        var bounce = Enumerable.Repeat(new Color3(1, 0.5f, 0.25f), LightProbeGenerator.LambertHamonicOrder * LightProbeGenerator.LambertHamonicOrder).ToList();
        lightProbes.Result = new() { [entity.Id] = bounce };

        // Two bounces: the coefficients are reset, then each bounce is computed and copied back
        lighting.LightProbeBounces = 2;
        await lighting.RebuildLightProbesAsync(lighting.LightProbeBounces);
        Assert.Equal(2, lightProbes.Steps);
        Assert.Equal(3, lightProbes.Updates);
        Assert.Equal(bounce, probe.Coefficients);

        // The reset sets them to black; one undo brings the computed ones back
        await lighting.RebuildLightProbesAsync(0);
        Assert.All(probe.Coefficients, x => Assert.Equal(default, x));
        fixture.Session.UndoRedoService.Undo();
        Assert.Equal(bounce, probe.Coefficients);

        // The bounces and the volumes are saved with the scene
        lighting.LightProbeWireframeVisible = true;
        var saved = new SceneSettingsData();
        lighting.SaveSettings(saved);
        Assert.Equal((2, true), (saved.LightProbeBounces, saved.LightProbeWireframe));
        lighting.LoadSettings(new SceneSettingsData { LightProbeBounces = 4, LightProbeWireframe = false });
        Assert.Equal((4, false), (lighting.LightProbeBounces, lightProbes.IsLightProbeVolumesVisible));
        editor.Destroy();
    }

    private sealed class FakeGridService : IEditorGameGridViewModelService
    {
        public bool IsActive { get; set; } = true;

        public Color3 Color { get; set; }

        public float Alpha { get; set; }

        public int AxisIndex { get; set; } = 2;
    }

    private sealed class FakeRenderModeService : IEditorGameRenderModeViewModelService
    {
        public EditorRenderMode RenderMode { get; set; } = EditorRenderMode.DefaultEditor;
    }

    private sealed class FakeCameraService : IEditorGameCameraViewModelService
    {
        public Vector3 Position { get; private set; }

        public Vector2 PitchYaw { get; private set; }

        public bool Orthographic { get; private set; }

        public float OrthographicSize { get; private set; }

        public float NearPlane { get; private set; }

        public float FarPlane { get; private set; }

        public float FieldOfView { get; private set; }

        public float SceneUnit { get; set; } = 1;

        public float MoveSpeed { get; set; } = 1;

        public void SetOrthographicProjection(bool value) => Orthographic = value;

        public void SetOrthographicSize(float value) => OrthographicSize = value;

        public void SetNearPlane(float value) => NearPlane = value;

        public void SetFarPlane(float value) => FarPlane = value;

        public void SetFieldOfView(float value) => FieldOfView = value;

        public void ResetCamera() { }

        public void ResetCameraOrientation(CameraOrientation orientation) { }

        public void LoadSettings(Vector3 position, Vector2 pitchYaw) => (Position, PitchYaw) = (position, pitchYaw);

        public (Vector3 Position, Vector2 PitchYaw) SaveSettings() => (Position, PitchYaw);
    }

    private sealed class FakeLightProbeService : IEditorGameLightProbeService
    {
        public Dictionary<Guid, List<Color3>> Result { get; set; } = [];

        public int Steps { get; private set; }

        public int Updates { get; private set; }

        public bool IsLightProbeVolumesVisible { get; set; }

        public Task<Dictionary<Guid, List<Color3>>> RequestLightProbesStep()
        {
            Steps++;
            return Task.FromResult(Result.ToDictionary(x => x.Key, x => new List<Color3>(x.Value)));
        }

        public Task UpdateLightProbeCoefficients()
        {
            Updates++;
            return Task.CompletedTask;
        }
    }
}
