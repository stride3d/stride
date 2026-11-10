// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.CodeAnalysis;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Presentation.Services;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.Scripts;
using Stride.Core.Quantum;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The slots of the blocks of a visual script method, regenerated from the latest compilation of the scripts, as the WPF view
/// model did: the slots that stay keep their id, their value and their links, and each regeneration that changes a block is one
/// undo entry.
/// </summary>
public sealed class VisualScriptMethodViewModelTests
{
    [Fact]
    public async Task WithoutACompilationProviderTheBlocksKeepTheirSlots()
    {
        using var test = await VisualScriptTest.OpenAsync(withCompilation: false);
        var names = test.RecordUndoNames();

        await test.Method.RegenerateSlots();

        Assert.All(test.Method.Method.Blocks.Values, x => Assert.Empty(x.Slots));
        Assert.Empty(names);
    }

    [Fact]
    public async Task RegeneratingTheSlotsOfABlockIsOneUndoEntry()
    {
        using var test = await VisualScriptTest.OpenAsync(withCompilation: true);
        var names = test.RecordUndoNames();

        await test.Method.RegenerateSlots();

        foreach (var block in test.Method.Method.Blocks.Values)
        {
            Assert.Equal([null, "Condition", "True", "False"], block.Slots.Select(x => x.Name));
            Assert.All(block.Slots, x => Assert.Same(block, x.Owner));
        }
        Assert.Equal(["Updated slots", "Updated slots"], names);

        test.Undo();
        Assert.Single(test.Method.Method.Blocks.Values, x => x.Slots.Count == 0);
    }

    [Fact]
    public async Task RegeneratingKeepsTheIdsValuesAndLinksOfTheSlotsThatStay()
    {
        using var test = await VisualScriptTest.OpenAsync(withCompilation: true);
        await test.Method.RegenerateSlots();
        var (first, second) = test.Blocks;
        var trueSlot = first.TrueSlot;
        var input = second.Slots.Single(x => x.Kind == SlotKind.Execution && x.Direction == SlotDirection.Input);
        var link = new Link(trueSlot, input);
        using (test.UndoRedo.CreateTransaction())
        {
            test.Method.AddLink(link);
            test.NodeOf(first.ConditionSlot)[nameof(Slot.Value)].Update("false");
            // The first block lost a slot, as when the code it calls changed
            var slots = test.NodeOf(first)[nameof(Block.Slots)].Target!;
            slots.Remove(first.FalseSlot, new NodeIndex(first.Slots.IndexOf(first.FalseSlot)));
        }
        var ids = first.Slots.Select(x => x.Id).ToList();
        var names = test.RecordUndoNames();

        await test.Method.RegenerateSlots(first);

        Assert.Equal([null, "Condition", "True", "False"], first.Slots.Select(x => x.Name));
        Assert.Equal(ids, first.Slots.Take(3).Select(x => x.Id));
        Assert.Equal("false", first.ConditionSlot.Value);
        var kept = Assert.Single(test.Method.Method.Links.Values);
        Assert.Same(first.TrueSlot, kept.Source);
        Assert.Same(input, kept.Target);
        Assert.Equal(["Updated slots"], names);

        // A block whose slots are the same is left as it is
        await test.Method.RegenerateSlots(second);
        Assert.Equal(["Updated slots"], names);
    }

    [Fact]
    public async Task BlocksAndLinksAreAddedAndRemovedByTheirId()
    {
        using var test = await VisualScriptTest.OpenAsync(withCompilation: true);
        await test.Method.RegenerateSlots();
        var (first, second) = test.Blocks;
        var names = test.RecordUndoNames();

        var third = new ConditionalBranchBlock { Position = new(400, 0) };
        using (var transaction = test.UndoRedo.CreateTransaction())
        {
            test.Method.AddBlock(third);
            test.UndoRedo.SetName(transaction, "Add block");
        }
        Assert.Same(third, test.Method.Method.Blocks[third.Id]);

        var input = second.Slots.Single(x => x.Kind == SlotKind.Execution && x.Direction == SlotDirection.Input);
        var toSecond = new Link(first.TrueSlot, input);
        var back = new Link(second.TrueSlot, first.Slots.Single(x => x.Kind == SlotKind.Execution && x.Direction == SlotDirection.Input));
        using (var transaction = test.UndoRedo.CreateTransaction())
        {
            test.Method.AddLink(toSecond);
            test.Method.AddLink(back);
            test.UndoRedo.SetName(transaction, "Add links");
        }
        Assert.Equal(2, test.Method.Method.Links.Count);

        // Removing a block removes the links to and from it
        using (var transaction = test.UndoRedo.CreateTransaction())
        {
            test.Method.RemoveBlock(second);
            test.UndoRedo.SetName(transaction, "Remove block");
        }
        Assert.False(test.Method.Method.Blocks.ContainsKey(second.Id));
        Assert.Empty(test.Method.Method.Links);
        Assert.Equal(["Add block", "Add links", "Remove block"], names);

        test.Undo();
        Assert.Same(second, test.Method.Method.Blocks[second.Id]);
        Assert.Equal(2, test.Method.Method.Links.Count);
    }

    private sealed class FakeCompilationProvider : IScriptCompilationProvider
    {
        public Compilation? LatestCompilation => null;

        public event EventHandler? LatestCompilationChanged { add { } remove { } }
    }

    private sealed class VisualScriptTest : IDisposable
    {
        private readonly AssetsSessionFixture fixture;

        private VisualScriptTest(AssetsSessionFixture fixture, VisualScriptMethodViewModel method)
        {
            this.fixture = fixture;
            Method = method;
        }

        public VisualScriptMethodViewModel Method { get; }

        public (ConditionalBranchBlock First, ConditionalBranchBlock Second) Blocks
        {
            get
            {
                var blocks = Method.Method.Blocks.Values.Cast<ConditionalBranchBlock>().OrderBy(x => x.Position.X).ToList();
                return (blocks[0], blocks[1]);
            }
        }

        public Stride.Core.Presentation.Services.IUndoRedoService UndoRedo => fixture.Session.UndoRedoService;

        public static async Task<VisualScriptTest> OpenAsync(bool withCompilation)
        {
            // Two blocks saved without their slots
            var method = new Method("Update");
            method.Blocks.Add(new ConditionalBranchBlock { Position = new(0, 0) });
            method.Blocks.Add(new ConditionalBranchBlock { Position = new(200, 0) });
            var asset = new VisualScriptAsset { BaseType = "Stride.Engine.SyncScript" };
            asset.Methods.Add(method);

            var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: true, ("Script", asset));
            Assert.False(fixture.Result.HasErrors, fixture.Log);
            // A visual script is a code asset of the project, named after its namespace
            var visualScript = fixture.Session.AllAssets.OfType<VisualScriptViewModel>().Single();
            if (withCompilation)
                visualScript.ServiceProvider.RegisterService(new FakeCompilationProvider());
            return new VisualScriptTest(fixture, Assert.Single(visualScript.Methods));
        }

        public IObjectNode NodeOf(object value) => fixture.Session.AssetNodeContainer.GetOrCreateNode(value);

        public List<string?> RecordUndoNames()
        {
            var names = new List<string?>();
            UndoRedo.Done += (_, e) =>
            {
                if (e.Transaction.Operations.Any(x => x.HasEffect))
                    names.Add(UndoRedo.GetName(e.Transaction));
            };
            return names;
        }

        public void Undo() => UndoRedo.Undo();

        public void Dispose() => fixture.Dispose();
    }
}
