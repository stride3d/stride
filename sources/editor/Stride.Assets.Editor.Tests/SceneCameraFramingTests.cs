// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.EditorGame;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The camera of the scene view frames an entity (F, the focus button of the hierarchy) as in the WPF scene editor: it
/// keeps its direction and backs away to three times the radius of the entity.
/// </summary>
public sealed class SceneCameraFramingTests
{
    [Fact]
    public void TheCameraFramesAnEntityAndItsChildrenFromItsCurrentDirection()
    {
        var camera = CreateCamera();
        var parent = new Entity("Parent") { Transform = { Position = new Vector3(2, 1, -3) } };
        parent.AddChild(new Entity("Left") { Transform = { Position = new Vector3(-1, 0, 0) } });
        parent.AddChild(new Entity("Right") { Transform = { Position = new Vector3(1, 0, 0) } });
        // The camera looks along -X
        camera.ResetCamera(-Vector3.UnitX);

        camera.SetTarget(parent, keepActualTargetDistance: false);

        // The children make a sphere of radius 1 around the parent: the camera stands 3 units away, on +X
        AssertNearEqual(new Vector3(5, 1, -3), camera.Position);
        Assert.Equal(MathUtil.PiOverTwo, camera.Yaw, 5);
        Assert.Equal(0, camera.Pitch, 5);
    }

    [Fact]
    public void AnEntityWithoutVolumeIsFramedFromHalfASceneUnit()
    {
        var camera = CreateCamera();
        camera.SceneUnit = 2;
        var entity = new Entity("Empty") { Transform = { Position = new Vector3(0, 4, 0) } };

        // The camera looks along -Z
        camera.SetTarget(entity, keepActualTargetDistance: false);

        AssertNearEqual(new Vector3(0, 4, 3), camera.Position);
    }

    [Fact]
    public void TheCameraCanFrameOneMeshOfAModel()
    {
        var camera = CreateCamera();
        var model = new Model();
        model.Meshes.Add(new Mesh { BoundingBox = new BoundingBox(new Vector3(-1), new Vector3(1)) });
        model.Meshes.Add(new Mesh { BoundingBox = new BoundingBox(new Vector3(4, 0, 0), new Vector3(6, 2, 2)) });
        var entity = new Entity("Model") { new ModelComponent { Model = model } };

        // The highlighted material of the material selection mode
        camera.SetTarget(entity, keepActualTargetDistance: false, meshIndex: 1);

        // The mesh is a box of half size 1 centered on (5, 1, 1); the camera looks along -Z
        AssertNearEqual(new Vector3(5, 1, 4), camera.Position);
    }

    private static EditorGameEntityCameraService CreateCamera()
    {
        // Framing doesn't use the game
        return new EditorGameEntityCameraService(null!);
    }

    private static void AssertNearEqual(Vector3 expected, Vector3 actual)
    {
        var epsilon = new Vector3(1e-4f);
        Assert.True(Vector3.NearEqual(in expected, in actual, in epsilon), $"Expected {expected}, got {actual}.");
    }
}
