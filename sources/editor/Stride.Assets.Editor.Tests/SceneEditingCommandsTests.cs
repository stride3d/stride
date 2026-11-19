// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using static Stride.Assets.Editor.Tests.EntityHierarchyEditorTests;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The scene editing commands that place the selection from the camera of the view: where the entities go, and the single
/// entry of the undo history of each one. The camera is given, as the game would give it.
/// </summary>
public sealed class SceneEditingCommandsTests
{
    private const float Tolerance = 1e-4f;

    // A camera at (0, 2, 10) turned a quarter to the left: it looks along -X
    private static readonly Vector3 CameraPosition = new(0, 2, 10);
    private static readonly Quaternion CameraRotation = Quaternion.RotationY(MathUtil.PiOverTwo);

    [Fact]
    public async Task MoveToViewPutsTheCenterOfTheSelectionInFrontOfTheCamera()
    {
        await using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var (box, parent, child) = Place(fixture, editor);
        editor.SelectedContent.Add(box);
        editor.SelectedContent.Add(parent);
        editor.SelectedContent.Add(child);
        await editor.PropertiesRefreshCompletion;
        var names = RecordUndoNames(fixture);

        editor.MoveSelectionToView(CameraPosition, CameraRotation, 5);

        // The center of Box (1, 0, 0) and Parent (3, 0, 0) goes 5 units along -X from the camera; Child moves with its parent
        AssertNear(new Vector3(-6, 2, 10), WorldPosition(box));
        AssertNear(new Vector3(-4, 2, 10), WorldPosition(parent));
        AssertNear(new Vector3(0, 0, 2), child.AssetSideEntity.Transform.Position);
        Assert.Equal(Quaternion.Identity, box.AssetSideEntity.Transform.Rotation);
        Assert.Equal(["Move to view"], names);

        fixture.Session.UndoRedoService.Undo();
        AssertNear(new Vector3(1, 0, 0), WorldPosition(box));
        AssertNear(new Vector3(3, 0, 0), WorldPosition(parent));
        editor.Destroy();
    }

    [Fact]
    public async Task AlignWithViewGivesTheCameraPlaceAndRotationToAChildToo()
    {
        await using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var (_, parent, child) = Place(fixture, editor);
        // The parent turns, and the child is scaled: the child still ends where the camera is, looking where it looks
        Update(fixture, parent, nameof(TransformComponent.Rotation), Quaternion.RotationY(-MathUtil.PiOverFour));
        Update(fixture, child, nameof(TransformComponent.Scale), new Vector3(2));
        editor.SelectedContent.Add(child);
        await editor.PropertiesRefreshCompletion;
        var names = RecordUndoNames(fixture);

        editor.AlignSelectionWith(CameraPosition, CameraRotation);

        var transform = child.AssetSideEntity.Transform;
        transform.UpdateWorldMatrix();
        transform.GetWorldTransformation(out var position, out var rotation, out var scale);
        AssertNear(CameraPosition, position);
        AssertNear(Vector3.Transform(-Vector3.UnitZ, CameraRotation), Vector3.Transform(-Vector3.UnitZ, rotation));
        Assert.Equal(new Vector3(2), transform.Scale);
        AssertNear(new Vector3(2), scale);
        Assert.Equal(["Align with view"], names);

        fixture.Session.UndoRedoService.Undo();
        Assert.Equal(new Vector3(0, 0, 2), transform.Position);
        Assert.Equal(Quaternion.Identity, transform.Rotation);
        editor.Destroy();
    }

    [Fact]
    public async Task TheViewCommandsNeedTheGameAndASelection()
    {
        await using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        editor.SelectedContent.Add(FindEntity(editor, "Box"));

        // The session of the tests has no game viewport: no camera to place the selection from
        Assert.False(editor.MoveToViewCommand.CanExecute(null));
        Assert.False(editor.AlignWithViewCommand.CanExecute(null));
        Assert.False(editor.AlignViewToSelectionCommand.CanExecute(null));
        editor.Destroy();
    }

    [Fact]
    public async Task OnlyTheTopmostSelectedEntitiesMove()
    {
        await using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var (box, parent, child) = Place(fixture, editor);
        editor.SelectedContent.Add(child);
        editor.SelectedContent.Add(parent);
        editor.SelectedContent.Add(box);

        Assert.Equal([parent, box], editor.GetSelectedRoots());
        editor.Destroy();
    }

    [Fact]
    public void TheViewPointIsAlongTheDirectionTheCameraLooks()
    {
        AssertNear(new Vector3(0, 0, -5), EntityHierarchyEditorViewModel.InFrontOf(Vector3.Zero, Quaternion.Identity, 5));
        AssertNear(new Vector3(-3, 2, 10), EntityHierarchyEditorViewModel.InFrontOf(CameraPosition, CameraRotation, 3));
    }

    /// <summary>
    /// Box at (1, 0, 0), Parent at (3, 0, 0), Child at (0, 0, 2) under Parent.
    /// </summary>
    internal static (EntityViewModel Box, EntityViewModel Parent, EntityViewModel Child) Place(AssetsSessionFixture fixture, EntityHierarchyEditorViewModel editor)
    {
        var box = FindEntity(editor, "Box");
        var parent = FindEntity(editor, "Parent");
        var child = FindEntity(editor, "Child");
        Update(fixture, box, nameof(TransformComponent.Position), new Vector3(1, 0, 0));
        Update(fixture, parent, nameof(TransformComponent.Position), new Vector3(3, 0, 0));
        Update(fixture, child, nameof(TransformComponent.Position), new Vector3(0, 0, 2));
        return (box, parent, child);
    }

    internal static void Update(AssetsSessionFixture fixture, EntityViewModel entity, string member, object value)
    {
        using var transaction = fixture.Session.UndoRedoService.CreateTransaction();
        fixture.Session.AssetNodeContainer.GetNode(entity.AssetSideEntity.Transform)[member].Update(value);
    }

    internal static Vector3 WorldPosition(EntityViewModel entity)
    {
        entity.AssetSideEntity.Transform.UpdateWorldMatrix();
        return entity.AssetSideEntity.Transform.WorldMatrix.TranslationVector;
    }

    internal static void AssertNear(Vector3 expected, Vector3 actual)
    {
        Assert.True(Vector3.Distance(expected, actual) < Tolerance, $"Expected {expected}, got {actual}.");
    }

    internal static List<string?> RecordUndoNames(AssetsSessionFixture fixture)
    {
        var undoRedo = fixture.Session.UndoRedoService;
        var names = new List<string?>();
        undoRedo.Done += (_, e) => names.Add(undoRedo.GetName(e.Transaction));
        return names;
    }
}
