// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.EditorGame;
using Stride.Assets.Editor.EditorGame.Tools;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Mathematics;
using Stride.Engine;
using static Stride.Assets.Editor.Tests.EntityHierarchyEditorTests;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The rectangle selection of the scene view: which entities a rectangle holds, and how it changes the selection, as one
/// step of the selection history.
/// </summary>
public sealed class RectangleSelectionTests
{
    // A camera at (0, 0, 10) looking at the origin, with a 90° field of view: a point at the origin is at the center of the
    // view, and a point 5 units aside is halfway to the edge
    private static readonly Matrix ViewProjection = Matrix.LookAtRH(new Vector3(0, 0, 10), Vector3.Zero, Vector3.UnitY) * Matrix.PerspectiveFovRH(MathUtil.PiOverTwo, 1.0f, 0.1f, 100.0f);

    [Fact]
    public void APointIsInTheRectangleItProjectsInto()
    {
        Assert.True(RectangleSelection.Contains(new RectangleF(0.4f, 0.4f, 0.2f, 0.2f), Vector3.Zero, ViewProjection));
        // Right and up: the view's y goes down
        Assert.True(RectangleSelection.Contains(new RectangleF(0.7f, 0.2f, 0.1f, 0.1f), new Vector3(5, 5, 0), ViewProjection));
        Assert.False(RectangleSelection.Contains(new RectangleF(0.7f, 0.7f, 0.1f, 0.1f), new Vector3(5, 5, 0), ViewProjection));
        // Behind the camera, even where it would project
        Assert.False(RectangleSelection.Contains(new RectangleF(0, 0, 1, 1), new Vector3(0, 0, 20), ViewProjection));
    }

    [Fact]
    public void ABoxIsSelectedOnlyWhenTheRectangleHoldsAllOfIt()
    {
        var box = new BoundingBox(new Vector3(-1), new Vector3(1));

        Assert.True(RectangleSelection.Contains(new RectangleF(0.4f, 0.4f, 0.2f, 0.2f), box, ViewProjection));
        // The rectangle crosses it: a floor under the props isn't selected with them
        Assert.False(RectangleSelection.Contains(new RectangleF(0.5f, 0.4f, 0.2f, 0.2f), box, ViewProjection));
        // A box around the camera
        Assert.False(RectangleSelection.Contains(new RectangleF(0, 0, 1, 1), new BoundingBox(new Vector3(-20), new Vector3(20)), ViewProjection));
    }

    [Fact]
    public void AnEntityWithoutAModelIsInTheRectangleByItsPosition()
    {
        var light = new Entity("Light") { Transform = { Position = new Vector3(5, 5, 0) } };
        light.Transform.UpdateWorldMatrix();

        Assert.True(EditorGameEntitySelectionService.IsInRectangle(light, new RectangleF(0.7f, 0.2f, 0.1f, 0.1f), ViewProjection));
        Assert.False(EditorGameEntitySelectionService.IsInRectangle(light, new RectangleF(0.4f, 0.4f, 0.2f, 0.2f), ViewProjection));
    }

    [Fact]
    public void TheAnchorOfAChildSceneIsNotAnEntityOfTheContent()
    {
        // The game adds to a scene an entity with the identifier of each child scene, to place it
        var parent = new Scene();
        var child = new Scene();
        parent.Children.Add(child);
        var anchor = new Entity { Id = child.Id };
        var box = new Entity("Box");
        parent.Entities.Add(anchor);
        parent.Entities.Add(box);

        Assert.True(EditorGameEntitySelectionService.IsSceneAnchor(anchor));
        Assert.False(EditorGameEntitySelectionService.IsSceneAnchor(box));
    }

    [Fact]
    public void TheCornersGiveTheRectangleInAnyOrder()
    {
        Assert.Equal(new RectangleF(0.2f, 0.1f, 0.4f, 0.5f), RectangleSelection.FromCorners(new Vector2(0.6f, 0.1f), new Vector2(0.2f, 0.6f)));
    }

    [Theory]
    [InlineData(false, false, RectangleSelectionMode.Replace)]
    [InlineData(false, true, RectangleSelectionMode.Add)]
    [InlineData(true, false, RectangleSelectionMode.Toggle)]
    [InlineData(true, true, RectangleSelectionMode.Toggle)]
    public void CtrlTogglesAndShiftAdds(bool ctrl, bool shift, RectangleSelectionMode mode)
    {
        Assert.Equal(mode, RectangleSelection.ModeOf(ctrl, shift));
    }

    [Fact]
    public void TheModesReplaceAddToOrToggleTheSelection()
    {
        List<object> Selection() => ["A", "B"];

        var replaced = Selection();
        Assert.True(RectangleSelection.Apply(replaced, ["B", "C"], RectangleSelectionMode.Replace));
        Assert.Equal(["B", "C"], replaced);

        var added = Selection();
        Assert.True(RectangleSelection.Apply(added, ["B", "C"], RectangleSelectionMode.Add));
        Assert.Equal(["A", "B", "C"], added);

        // As Ctrl+click: what is selected leaves, the rest joins
        var toggled = Selection();
        Assert.True(RectangleSelection.Apply(toggled, ["B", "C"], RectangleSelectionMode.Toggle));
        Assert.Equal(["A", "C"], toggled);

        // An empty rectangle empties the selection, unless it adds
        var emptied = Selection();
        Assert.True(RectangleSelection.Apply(emptied, [], RectangleSelectionMode.Replace));
        Assert.Empty(emptied);
        var unchanged = Selection();
        Assert.False(RectangleSelection.Apply(unchanged, [], RectangleSelectionMode.Add));
        Assert.Equal(["A", "B"], unchanged);
    }

    [Fact]
    public async Task ARectangleIsOneStepOfTheSelectionHistory()
    {
        // On a dispatcher that queues the calls made from it, as the UI thread: a step of the history completes at the next
        // dispatcher frame
        using var ui = new ThreadDispatcherService("Fake UI thread");
        await ui.Run(async () =>
        {
            await using var fixture = await OpenSceneAsync(ui);
            var editor = await OpenEditorAsync(fixture);
            var history = fixture.Session.ServiceProvider.Get<SelectionService>();
            var box = FindEntity(editor, "Box");
            var parent = FindEntity(editor, "Parent");
            var child = FindEntity(editor, "Child");
            editor.SelectedContent.Add(box);
            await ui.InvokeAsync(() => { });

            // The game changes the selection of the hierarchy in one dispatcher call: the box leaves, two entities join
            RectangleSelection.Apply(editor.SelectedContent, [parent, child], RectangleSelectionMode.Replace);
            await ui.InvokeAsync(() => { });
            Assert.Equal([parent, child], editor.SelectedContent);

            history.NavigateBackward();
            Assert.Equal([box], editor.SelectedContent);
            history.NextSelection();
            Assert.Equal([parent, child], editor.SelectedContent.ToHashSet());
            editor.Destroy();
        });
    }
}
