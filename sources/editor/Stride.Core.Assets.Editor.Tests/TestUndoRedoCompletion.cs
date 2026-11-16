// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.Services;
using Stride.Core.Transactions;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The completions of the undo service, which the property grid waits on before it shows a selection: they complete when the
/// outermost transaction, undo or redo ends, whatever is nested in it.
/// </summary>
public sealed class TestUndoRedoCompletion
{
    [Fact]
    public void TheCompletionTakenBeforeANestedTransactionCompletesWhenTheOuterTransactionEnds()
    {
        var service = new UndoRedoService(10);

        var outer = service.CreateTransaction();
        var beforeNested = service.TransactionCompletion;
        var inner = service.CreateTransaction();
        var insideNested = service.TransactionCompletion;
        service.PushOperation(new CountingOperation());
        inner.Dispose();

        Assert.False(beforeNested.IsCompleted);
        Assert.False(insideNested.IsCompleted);
        Assert.False(service.TransactionCompletion.IsCompleted);

        outer.Dispose();

        Assert.True(beforeNested.IsCompleted);
        Assert.True(insideNested.IsCompleted);
        Assert.True(service.TransactionCompletion.IsCompleted);
    }

    [Fact]
    public void EachOuterTransactionHasACompletionOfItsOwn()
    {
        var service = new UndoRedoService(10);

        using (service.CreateTransaction())
        {
            service.PushOperation(new CountingOperation());
        }
        Assert.True(service.TransactionCompletion.IsCompleted);

        var transaction = service.CreateTransaction();
        var second = service.TransactionCompletion;
        Assert.False(second.IsCompleted);
        transaction.Dispose();
        Assert.True(second.IsCompleted);
    }

    [Fact]
    public void AnUndoStartedByAnUndoHandlerCompletesWithTheOuterUndo()
    {
        var service = new UndoRedoService(10);
        var first = new CountingOperation();
        var second = new CountingOperation();
        Push(service, first);
        Push(service, second);

        Task? outerCompletion = null;
        var nestedRan = false;
        service.Undone += (_, _) =>
        {
            if (nestedRan)
                return;
            nestedRan = true;
            outerCompletion = service.UndoRedoCompletion;
            service.Undo();
            Assert.True(service.UndoRedoInProgress);
            Assert.False(outerCompletion.IsCompleted);
        };

        service.Undo();

        Assert.True(nestedRan);
        Assert.Equal(1, first.Rollbacks);
        Assert.Equal(1, second.Rollbacks);
        Assert.True(outerCompletion!.IsCompleted);
        Assert.False(service.UndoRedoInProgress);
        Assert.True(service.UndoRedoCompletion.IsCompleted);
    }

    [Fact]
    public void ARedoStartedByARedoHandlerCompletesWithTheOuterRedo()
    {
        var service = new UndoRedoService(10);
        var first = new CountingOperation();
        var second = new CountingOperation();
        Push(service, first);
        Push(service, second);
        service.Undo();
        service.Undo();

        Task? outerCompletion = null;
        var nestedRan = false;
        service.Redone += (_, _) =>
        {
            if (nestedRan)
                return;
            nestedRan = true;
            outerCompletion = service.UndoRedoCompletion;
            service.Redo();
            Assert.True(service.UndoRedoInProgress);
            Assert.False(outerCompletion.IsCompleted);
        };

        service.Redo();

        Assert.True(nestedRan);
        Assert.Equal(1, first.Rollforwards);
        Assert.Equal(1, second.Rollforwards);
        Assert.True(outerCompletion!.IsCompleted);
        Assert.False(service.UndoRedoInProgress);
    }

    [Fact]
    public async Task APropertyGridRequestMadeInANestedTransactionCompletesWhenTheOuterTransactionEnds()
    {
        await using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var wood = fixture.Session.AllAssets.Single();
        var undoRedo = fixture.Session.UndoRedoService!;

        var request = fixture.OnUiThread(() =>
        {
            var outer = undoRedo.CreateTransaction();
            Task<GraphViewModel?> pending;
            using (undoRedo.CreateTransaction())
            {
                wood.PropertyGraph.RootNode[nameof(TestEditorAsset.Value)].Update("oak");
                pending = fixture.Session.AssetViewProperties.GenerateSelectionPropertiesAsync([wood]);
            }
            using (undoRedo.CreateTransaction())
            {
                wood.PropertyGraph.RootNode[nameof(TestEditorAsset.Value)].Update("pine");
            }
            Assert.False(pending.IsCompleted);
            undoRedo.SetName(outer, "Rename");
            outer.Dispose();
            return pending;
        });

        var graph = await request.WaitAsync(TimeSpan.FromSeconds(30));
        Assert.NotNull(graph);
        Assert.Equal("pine", graph.RootNode.GetChild(nameof(TestEditorAsset.Value))!.NodeValue);
    }

    private static void Push(UndoRedoService service, CountingOperation operation)
    {
        using (service.CreateTransaction())
        {
            service.PushOperation(operation);
        }
    }

    private sealed class CountingOperation : Operation
    {
        public int Rollbacks { get; private set; }

        public int Rollforwards { get; private set; }

        protected override void Rollback() => ++Rollbacks;

        protected override void Rollforward() => ++Rollforwards;
    }
}
