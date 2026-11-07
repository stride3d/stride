// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.EditorGame;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.GameEditor;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Mathematics;
using Stride.Core.Quantum;
using Stride.Engine;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The edits of an entity of the asset (property grid, undo and redo, gizmo moves) reach its clone in the game. The game
/// is a fake one with a thread of its own, and the test runs on a UI thread of its own, as the propagators lock both.
/// </summary>
public sealed class GameEditorChangePropagatorTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(20);

    [Fact]
    public Task RenamingAnEntityRenamesItsGameSideClone() => RunAsync(async context =>
    {
        context.Box.Name = "Renamed";
        await context.WaitForGameSide(x => x.Name == "Renamed");

        context.Fixture.Session.UndoRedoService.Undo();
        await context.WaitForGameSide(x => x.Name == "Box");
    });

    [Fact]
    public Task AnAddedComponentIsClonedToTheGameSideAndUndoRemovesIt() => RunAsync(async context =>
    {
        var light = new LightComponent { Intensity = 3 };
        var undoRedo = context.Fixture.Session.UndoRedoService;
        using (var transaction = undoRedo.CreateTransaction())
        {
            var components = context.Fixture.Session.AssetNodeContainer.GetNode(context.Box.AssetSideEntity)[nameof(Entity.Components)].Target!;
            components.Add(light, new NodeIndex(1));
            undoRedo.SetName(transaction, "Add component");
        }

        await context.WaitForGameSide(x => x.Components.Count == 2);
        var gameSideLight = await context.Controller.InvokeAsync(() => context.GameSideBox.Components[1]);
        var clone = Assert.IsType<LightComponent>(gameSideLight);
        Assert.NotSame(light, clone);
        Assert.Equal(3, clone.Intensity);
        Assert.Same(context.GameSideBox, clone.Entity);

        undoRedo.Undo();
        await context.WaitForGameSide(x => x.Components.Count == 1);
    });

    [Fact]
    public Task AGizmoMoveReachesTheGameSideThroughTheAsset() => RunAsync(async context =>
    {
        var position = new Vector3(1, 2, 3);
        var scale = new Vector3(2);

        context.Editor.UpdateTransformations(new Dictionary<AbsoluteId, TransformationTRS>
        {
            [context.Box.Id] = new() { Position = position, Rotation = Quaternion.Identity, Scale = scale },
        });

        await context.WaitForGameSide(x => x.Transform.Position == position && x.Transform.Scale == scale);

        context.Fixture.Session.UndoRedoService.Undo();
        await context.WaitForGameSide(x => x.Transform.Position == Vector3.Zero && x.Transform.Scale == Vector3.One);
    });

    [Fact]
    public Task ADestroyedPropagatorLeavesTheGameSideAlone() => RunAsync(async context =>
    {
        context.Propagator.Destroy();

        var count = await context.Controller.InvokeAsync(() => context.Controller.RenderStageReevaluationCount);
        context.Box.Name = "Renamed";
        // A live propagator replays a change within a few milliseconds
        await Task.Delay(200);

        Assert.Equal("Box", await context.Controller.InvokeAsync(() => context.GameSideBox.Name));
        Assert.Equal(count, await context.Controller.InvokeAsync(() => context.Controller.RenderStageReevaluationCount));
    });

    private static async Task RunAsync(Func<PropagatorContext, Task> test)
    {
        using var ui = new ThreadDispatcherService("Fake UI thread");
        await ui.Run(async () =>
        {
            using var fixture = await EntityHierarchyEditorTests.OpenSceneAsync(ui);
            var editor = await EntityHierarchyEditorTests.OpenEditorAsync(fixture);
            using var controller = new FakeEditorGameController(fixture.Session);
            var box = EntityHierarchyEditorTests.FindEntity(editor, "Box");

            // The game-side clone, as the controller of a real game makes it
            var gameSideBox = AssetCloner.Clone(box.AssetSideEntity, AssetClonerFlags.ReferenceAsNull);
            await controller.InvokeAsync(() => controller.AddGameSidePart(box.Id, gameSideBox));
            var propagator = new EntityHierarchyElementChangePropagator(editor, controller, box.Asset, box.Id, box.AssetSideEntity);
            propagator.NotifyGameSidePartAdded();
            await propagator.Initialized.WaitAsync(Timeout);
            try
            {
                await test(new PropagatorContext(fixture, editor, controller, box, gameSideBox, propagator));
            }
            finally
            {
                propagator.Destroy();
                editor.Destroy();
            }
        });
    }

    private sealed record PropagatorContext(AssetsSessionFixture Fixture, SceneEditorViewModel Editor, FakeEditorGameController Controller, EntityViewModel Box, Entity GameSideBox, GameEditorChangePropagator Propagator)
    {
        /// <summary>
        /// Waits until the game-side clone of the box meets the condition (UI thread).
        /// </summary>
        public async Task WaitForGameSide(Func<Entity, bool> condition)
        {
            var deadline = DateTime.UtcNow + Timeout;
            while (!await Controller.InvokeAsync(() => condition(GameSideBox)))
            {
                if (DateTime.UtcNow > deadline)
                    Assert.Fail("The change did not reach the game side.");
                await Task.Delay(10);
            }
        }
    }
}
