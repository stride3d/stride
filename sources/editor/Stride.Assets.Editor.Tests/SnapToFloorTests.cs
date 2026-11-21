// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.EditorGame;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;
using static Stride.Assets.Editor.Tests.EntityHierarchyEditorTests;
using static Stride.Assets.Editor.Tests.SceneEditingCommandsTests;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The snap to the floor: the floor under an entity, from the bounding boxes of the models of the game or the ground plane,
/// and the single entry of the undo history of the move.
/// </summary>
public sealed class SnapToFloorTests
{
    // A crate of 1 unit, floating between 2 and 3
    private static readonly BoundingBox Crate = new(new Vector3(-0.5f, 2, -0.5f), new Vector3(0.5f, 3, 0.5f));

    [Fact]
    public void TheCrateLandsOnTheHighestSurfaceUnderIt()
    {
        var table = new BoundingBox(new Vector3(-1, 0, -1), new Vector3(1, 1, 1));
        var stool = new BoundingBox(new Vector3(0.4f, 0, 0.4f), new Vector3(2, 0.5f, 2));

        Assert.Equal(-1, SceneEditingGeometry.GetFloorOffset(Crate, [stool, table]));
    }

    [Fact]
    public void WithNothingUnderItTheCrateLandsOnTheGround()
    {
        // Beside it, or above its middle (a shelf it hangs under): not a floor
        var beside = new BoundingBox(new Vector3(2, 0, 0), new Vector3(3, 1, 1));
        var shelf = new BoundingBox(new Vector3(-1, 2.6f, -1), new Vector3(1, 2.8f, 1));

        Assert.Equal(-2, SceneEditingGeometry.GetFloorOffset(Crate, [beside, shelf]));
    }

    [Fact]
    public void ACrateThatSinksIntoTheFloorGoesUp()
    {
        var sunk = new BoundingBox(new Vector3(-0.5f, 0.8f, -0.5f), new Vector3(0.5f, 1.8f, 0.5f));
        var table = new BoundingBox(new Vector3(-1, 0, -1), new Vector3(1, 1, 1));

        Assert.Equal(0.2f, SceneEditingGeometry.GetFloorOffset(sunk, [table])!.Value, 4);
    }

    [Fact]
    public void UnderTheGroundWithNothingBelowThereIsNoFloor()
    {
        var buried = new BoundingBox(new Vector3(-0.5f, -3, -0.5f), new Vector3(0.5f, -2, 0.5f));

        Assert.Null(SceneEditingGeometry.GetFloorOffset(buried, []));
    }

    [Fact]
    public void AnEntityWithoutAModelLandsByItsPosition()
    {
        var light = new Entity("Light") { Transform = { Position = new Vector3(1, 4, 2) } };
        light.Transform.UpdateWorldMatrix();

        var bounds = SceneEditingGeometry.GetSnapBounds(light);

        Assert.Equal(new BoundingBox(new Vector3(1, 4, 2), new Vector3(1, 4, 2)), bounds);
        Assert.Equal(-4, SceneEditingGeometry.GetFloorOffset(bounds, []));
    }

    [Fact]
    public void TheBoundsOfAnEntityHoldTheModelsOfItsChildrenThatShow()
    {
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        var hidden = new Entity("Hidden");
        parent.AddChild(child);
        parent.AddChild(hidden);
        child.Add(new ModelComponent(new Model()) { BoundingBox = new BoundingBox(new Vector3(0, 1, 0), new Vector3(1, 2, 1)) });
        hidden.Add(new ModelComponent(new Model()) { Enabled = false, BoundingBox = new BoundingBox(new Vector3(0, -5, 0), new Vector3(1, -4, 1)) });
        // A model without a mesh yet: the game didn't compute its box
        parent.Add(new ModelComponent(new Model()) { BoundingBox = new BoundingBox(new Vector3(float.MaxValue), new Vector3(float.MinValue)) });

        Assert.Equal(new BoundingBox(new Vector3(0, 1, 0), new Vector3(1, 2, 1)), SceneEditingGeometry.GetModelBounds(parent));
        Assert.Null(SceneEditingGeometry.GetModelBounds(hidden));
    }

    [Fact]
    public async Task TheSnapMovesTheEntitiesInOneUndoableOperation()
    {
        await using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var (box, parent, child) = Place(fixture, editor);
        // The parent is scaled: the child moves by the world offset all the same
        Update(fixture, parent, nameof(TransformComponent.Scale), new Vector3(2));
        var names = RecordUndoNames(fixture);

        editor.SnapSelectionToFloor(new Dictionary<AbsoluteId, float> { [box.Id] = -1, [child.Id] = 3, [parent.Id] = 0 });

        Assert.Equal(["Snap to floor"], names);
        AssertNear(new Vector3(1, -1, 0), WorldPosition(box));
        AssertNear(new Vector3(3, 3, 4), WorldPosition(child));
        Assert.Equal(new Vector3(3, 0, 0), parent.AssetSideEntity.Transform.Position);

        fixture.Session.UndoRedoService.Undo();
        Assert.Equal(new Vector3(1, 0, 0), box.AssetSideEntity.Transform.Position);
        Assert.Equal(new Vector3(0, 0, 2), child.AssetSideEntity.Transform.Position);

        // Already on the floor: nothing to undo
        editor.SnapSelectionToFloor(new Dictionary<AbsoluteId, float> { [box.Id] = 0 });
        Assert.Single(names);
        editor.Destroy();
    }
}
