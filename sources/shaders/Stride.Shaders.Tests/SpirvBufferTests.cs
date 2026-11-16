// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;

namespace Stride.Shaders.Parsers.Tests;

public class SpirvBufferTests
{
    [Fact]
    public void ATypedInstructionKeepsALongerValueAfterItsBufferGrew()
    {
        using var buffer = new SpirvBuffer();
        buffer.Add(new OpName(1, "a"));
        OpName name = buffer[0];
        for (var i = 0; i < 100; i++)
            buffer.Add(new OpName(2, "b"));

        name.Name = "a longer name";

        Assert.Equal("a longer name", ((OpName)buffer[0]).Name);
    }

    [Fact]
    public void InsertRangeMovesTheInstructionsOfAScratchBuffer()
    {
        using var buffer = Names(1, 4);
        using var scratch = Names(2, 3);

        buffer.InsertRange(1, scratch);

        Assert.Equal([1, 2, 3, 4], Targets(buffer));
        Assert.Equal(0, scratch.Count);
    }

    [Fact]
    public void ExtractRangeMovesInstructionsOutInOrder()
    {
        using var buffer = Names(1, 2, 3, 4);

        using var extracted = buffer.ExtractRange(1, 2);

        Assert.Equal([1, 4], Targets(buffer));
        Assert.Equal([2, 3], Targets(extracted));
    }

    [Fact]
    public void RemoveAllKeepsTheOrderOfTheOthers()
    {
        using var buffer = Names(1, 2, 3, 4, 5);

        buffer.RemoveAll(i => i.Words[1] % 2 == 0);

        Assert.Equal([1, 3, 5], Targets(buffer));
    }

    [Fact]
    public void WordCountMatchesTheWordsCopiedAfterAnInstructionGrowsDuringACompaction()
    {
        using var buffer = Names(1, 2, 3, 4);
        // Enough garbage that the next append compacts the storage
        buffer.RemoveRange(0, 3);

        OpName name = buffer[0];
        name.Name = "a longer name";

        var copied = new int[buffer.WordCount + 8];
        Assert.Equal(buffer.WordCount, buffer.CopyTo(copied));
        Assert.Equal("a longer name", ((OpName)buffer[0]).Name);
    }

    [Fact]
    public void AWordCountOfZeroIsRejected()
    {
        Assert.Throws<InvalidOperationException>(() => new SpirvBuffer([(int)Stride.Shaders.Spirv.Specification.Op.OpNop | (1 << 16), 0, 0]));
    }

    private static SpirvBuffer Names(params int[] targets)
    {
        var buffer = new SpirvBuffer();
        foreach (var target in targets)
            buffer.Add(new OpName(target, $"n{target}"));
        return buffer;
    }

    private static int[] Targets(SpirvBuffer buffer) => buffer.Select(i => i.Words[1]).ToArray();
}
