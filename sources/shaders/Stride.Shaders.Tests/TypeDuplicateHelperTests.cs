// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using CommunityToolkit.HighPerformance.Buffers;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Processing;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Parsers.Tests;

public class TypeDuplicateHelperTests
{
    // The mixer's sequence: a class adds names, PopulateShaderInfo turns some of them into OpNop (target 0),
    // then the next class merges a duplicate type into a type of the first class.
    [Fact]
    public void DecorationsOfAKeptTypeAreFoundAfterNamesBecomeNop()
    {
        var context = new SpirvContext();
        var helper = new TypeDuplicateHelper(context);

        Insert(helper, context, new OpName(1, "kept").InstructionMemory);
        Insert(helper, context, new OpDecorate(1, Decoration.ArrayStride, [16]).InstructionMemory);
        for (var id = 2; id <= 6; id++)
            Insert(helper, context, new OpName(id, $"removed{id}").InstructionMemory);

        for (var index = 2; index < context.Count; index++)
            SpirvBuilder.SetOpNop(context[index].Data.Memory.Span);
        helper.DropNopDecorations();

        Insert(helper, context, new OpName(8, "duplicate").InstructionMemory);
        Insert(helper, context, new OpDecorate(8, Decoration.ArrayStride, [16]).InstructionMemory);

        var mismatches = helper.MergeTypeDecorations(1, 8);

        Assert.Null(mismatches);
        // The name of the duplicate is adopted by the kept type, beside its own name and decoration
        var (start, end) = helper.FindDecorationRange(1);
        Assert.Equal(3, end - start);
        Assert.Equal(Op.OpNop, context[context.Count - 1].Data.Op);
    }

    private static void Insert(TypeDuplicateHelper helper, SpirvContext context, MemoryOwner<int> instruction)
        => helper.InsertInstruction(context.Count, new OpData(instruction));
}
