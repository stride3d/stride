// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using CommunityToolkit.HighPerformance.Buffers;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Processing;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Parsers.Tests;

public class TypeMergeIndexTests
{
    // The mixer's sequence: a class adds names, PopulateShaderInfo turns some of them into OpNop (target 0),
    // then the next class merges a duplicate type into a type of the first class.
    [Fact]
    public void DecorationsOfAKeptTypeAreFoundAfterNamesBecomeNop()
    {
        var context = new SpirvContext();
        var index = new TypeMergeIndex(context);

        index.Add(Data(new OpName(1, "kept").InstructionMemory));
        index.Add(Data(new OpDecorate(1, Decoration.ArrayStride, [16]).InstructionMemory));
        for (var id = 2; id <= 6; id++)
            index.Add(Data(new OpName(id, $"removed{id}").InstructionMemory));
        for (var position = 2; position < context.Count; position++)
            context[position].SetOpNop();

        index.Add(Data(new OpName(8, "duplicate").InstructionMemory));
        index.Add(Data(new OpDecorate(8, Decoration.ArrayStride, [16]).InstructionMemory));

        Assert.Null(index.MergeTypeDecorations(1, 8));
        Assert.Equal(Op.OpNop, context[context.Count - 1].Data.Op);
        Assert.Equal(Op.OpNop, context[context.Count - 2].Data.Op);

        // The kept type's decoration is still found: a duplicate with another stride gives a mismatch on each side
        index.Add(Data(new OpDecorate(9, Decoration.ArrayStride, [32]).InstructionMemory));
        var mismatches = index.MergeTypeDecorations(1, 9);
        Assert.NotNull(mismatches);
        Assert.Collection(mismatches, m => Assert.False(m.OnKeepOnly), m => Assert.True(m.OnKeepOnly));
    }

    // Types that other code appends become visible at the next node, as when the mixer built an index per node
    [Fact]
    public void ATypeAppendedByOtherCodeIsFoundAfterSync()
    {
        var context = new SpirvContext();
        var index = new TypeMergeIndex(context);
        context.Add(new OpTypeFloat(1, 32, null));
        var duplicate = Data(new OpTypeFloat(2, 32, null).InstructionMemory);

        Assert.False(index.CheckForDuplicates(duplicate, out _));
        index.Sync();
        Assert.True(index.CheckForDuplicates(duplicate, out var found));
        Assert.Equal(0, found.Index);
    }

    [Fact]
    public void PositionsAreRebuiltAfterAnInsertBeforeThem()
    {
        var context = new SpirvContext();
        context.Add(new OpTypeFloat(1, 32, null));
        var index = new TypeMergeIndex(context);
        context.Insert(0, new OpTypeInt(2, 32, 1));

        Assert.True(index.CheckForDuplicates(Data(new OpTypeFloat(3, 32, null).InstructionMemory), out var found));
        Assert.Equal(1, found.Index);
    }

    [Fact]
    public void TheDecorationsOfTheDuplicateBecomeNopAfterAnInsertBeforeThem()
    {
        var context = new SpirvContext();
        var index = new TypeMergeIndex(context);
        index.Add(Data(new OpName(1, "kept").InstructionMemory));
        index.Add(Data(new OpName(2, "duplicate").InstructionMemory));
        context.Insert(0, new OpName(3, "inserted"));

        Assert.Null(index.MergeTypeDecorations(1, 2));
        Assert.Equal(Op.OpName, context[0].Data.Op);
        Assert.Equal(Op.OpName, context[1].Data.Op);
        Assert.Equal(Op.OpNop, context[2].Data.Op);
    }

    private static OpData Data(MemoryOwner<int> instruction) => new(instruction);
}
