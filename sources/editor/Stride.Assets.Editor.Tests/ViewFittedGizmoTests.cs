// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.AssetEditors.Gizmos;
using Stride.Assets.Editor.EditorGame;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Processors;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The transformation gizmos are sized for the camera of each view the game draws: they keep the same size on the screen
/// of every view, whatever its projection, its distance to the selection and its height.
/// </summary>
public sealed class ViewFittedGizmoTests
{
    private static readonly Vector3 Selection = new(1, 0, -2);

    [Fact]
    public void AGizmoHasTheSameSizeOnScreenInAPerspectiveAndAnOrthographicViewOfAnotherHeight()
    {
        var perspective = CreateView(new Vector3(4, 2, 4), CameraProjectionMode.Perspective, 10, 720);
        var top = CreateView(Selection + new Vector3(0, 50, 0), CameraProjectionMode.Orthographic, 10, 360, pitch: -MathUtil.PiOverTwo);
        var zoomedOutTop = CreateView(Selection + new Vector3(0, 50, 0), CameraProjectionMode.Orthographic, 40, 360, pitch: -MathUtil.PiOverTwo);

        var onScreen = OnScreenSize(1.0f, perspective);
        Assert.Equal(onScreen, OnScreenSize(1.0f, top), 3);
        Assert.Equal(onScreen, OnScreenSize(1.0f, zoomedOutTop), 3);
        // ... while their size in the scene differs: the gizmo of the zoomed-out orthographic view is four times larger
        Assert.Equal(4 * TransformationGizmo.GetScreenFittedScale(1.0f, Selection, top), TransformationGizmo.GetScreenFittedScale(1.0f, Selection, zoomedOutTop), 3);
    }

    [Fact]
    public void AGizmoFartherFromTheCameraOfAViewIsLargerInTheSceneButNotOnScreen()
    {
        var near = CreateView(Selection + new Vector3(0, 0, 3), CameraProjectionMode.Perspective, 10, 400);
        var far = CreateView(Selection + new Vector3(0, 0, 30), CameraProjectionMode.Perspective, 10, 400);

        Assert.Equal(10 * TransformationGizmo.GetScreenFittedScale(1.0f, Selection, near), TransformationGizmo.GetScreenFittedScale(1.0f, Selection, far), 3);
        Assert.Equal(OnScreenSize(1.0f, near), OnScreenSize(1.0f, far), 3);
        // The size factor of the toolbar scales it in every view
        Assert.Equal(2 * OnScreenSize(1.0f, far), OnScreenSize(2.0f, far), 3);
    }

    [Fact]
    public void TheTranslationAndScaleGizmosShowTheSideOfTheirAxesThatFacesTheCameraOfEachView()
    {
        var front = CreateView(Selection + new Vector3(1, 1, 50), CameraProjectionMode.Orthographic, 10, 360);
        var back = CreateView(Selection + new Vector3(1, 1, -50), CameraProjectionMode.Orthographic, 10, 360, yaw: MathUtil.Pi);
        var perspective = CreateView(Selection + new Vector3(-4, 2, -4), CameraProjectionMode.Perspective, 10, 720, yaw: -3 * MathUtil.PiOverFour, pitch: -0.3f);

        // A gizmo in world space: the back view sees its Z axis from behind, while the front view sees none from behind
        var worldSpace = Matrix.Translation(Selection);
        Assert.Equal(GizmoTransformationAxes.None, AxisTransformationGizmo.GetBackfacingAxes(worldSpace, front.ViewMatrix));
        Assert.Equal(GizmoTransformationAxes.Z, AxisTransformationGizmo.GetBackfacingAxes(worldSpace, back.ViewMatrix));
        Assert.Equal(GizmoTransformationAxes.X | GizmoTransformationAxes.Z, AxisTransformationGizmo.GetBackfacingAxes(worldSpace, perspective.ViewMatrix));

        // A gizmo in the space of an entity turned around the Y axis: its X and Z axes point the other way
        var objectSpace = Matrix.RotationY(MathUtil.Pi) * Matrix.Translation(Selection);
        Assert.Equal(GizmoTransformationAxes.X | GizmoTransformationAxes.Z, AxisTransformationGizmo.GetBackfacingAxes(objectSpace, front.ViewMatrix));
        Assert.Equal(GizmoTransformationAxes.X, AxisTransformationGizmo.GetBackfacingAxes(objectSpace, back.ViewMatrix));
    }

    [Fact]
    public void TheEditorCameraOfAViewHasTheProjectionOfItsComponent()
    {
        var view = CreateView(Vector3.Zero, CameraProjectionMode.Orthographic, 25, 0);

        Assert.True(view.IsOrthographic);
        Assert.Equal(25, view.OrthographicSize);
        // A view that is not laid out yet counts as one pixel high, rather than dividing by zero
        Assert.Equal(1, view.Height);
        Assert.False(view.IsActive);
    }

    /// <summary>
    /// The size in pixels of a gizmo of scale <see cref="TransformationGizmo.GetScreenFittedScale"/> on the screen of the view.
    /// </summary>
    private static float OnScreenSize(float sizeFactor, EditorGameViewCamera view)
    {
        var scale = TransformationGizmo.GetScreenFittedScale(sizeFactor, Selection, view);
        // The height of the view in the scene, at the depth of the selection
        var depth = MathF.Abs(Vector3.TransformCoordinate(Selection, view.ViewMatrix).Z);
        var viewHeightInScene = view.IsOrthographic ? view.OrthographicSize : 2 * depth * MathF.Tan(MathUtil.DegreesToRadians(view.VerticalFieldOfView / 2));
        return scale / viewHeightInScene * view.Height;
    }

    private static EditorGameViewCamera CreateView(Vector3 position, CameraProjectionMode projection, float orthographicSize, int height, float yaw = 0, float pitch = 0)
    {
        var component = new CameraComponent
        {
            UseCustomViewMatrix = true,
            Projection = projection,
            OrthographicSize = orthographicSize,
            ViewMatrix = EditorGameCameraService.ComputeViewMatrix(position, yaw, pitch),
        };
        return new EditorGameViewCamera(component, height, false);
    }
}
