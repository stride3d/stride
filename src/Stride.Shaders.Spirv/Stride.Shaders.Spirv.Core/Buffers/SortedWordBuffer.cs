using CommunityToolkit.HighPerformance.Buffers;
using Stride.Shaders.Spirv.Core.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders.Spirv.Core.Buffers;

/// <summary>
/// A buffer where all instructions have been sorted, no instructions can be added to it
/// </summary>
public sealed class SortedWordBuffer : BufferBase<int>, ISpirvBuffer
{
    public static SortedWordBuffer Empty { get; } = new();
    public int InstructionCount => new SpirvReader(Memory).Count;
    public bool IsEmpty => Span.IsEmpty;

    public Span<int> InstructionSpan => InstructionMemory.Span;

    public Memory<int> InstructionMemory => HasHeader ? Memory[5..] : Memory;

    public bool HasHeader => Span[0] == Spv.Specification.MagicNumber;

    public Instruction this[int index]
    {
        get
        {
            var enumerator = GetEnumerator();
            int tmp = 0;
            while (enumerator.MoveNext() && tmp < index)
                tmp += 1;
            return enumerator.Current;
        }
    }

    public InstructionEnumerator GetEnumerator() => new(this);

    public SortedWordBuffer()
    {
        _owner = MemoryOwner<int>.Empty;
    }

    public SortedWordBuffer(WordBuffer buffer)
    {
        _owner = MemoryOwner<int>.Allocate(buffer.Length, AllocationMode.Clear);
        Length = 0;
        foreach (var item in buffer)
        {
            item.Words.Span.CopyTo(_owner.Span[Length..(Length + item.WordCount)]);
            Length += item.WordCount;
        }
    }
    public SortedWordBuffer(MultiBuffer buffer)
    {
        _owner = MemoryOwner<int>.Allocate(buffer.Length, AllocationMode.Clear);
        Length = 0;
        foreach (var item in buffer.Instructions)
        {
            item.Words.Span.CopyTo(_owner.Span[Length..(Length + item.WordCount)]);
            Length += item.WordCount;
        }
    }
    public SortedWordBuffer(SpirvBuffer buffer)
    {
        _owner = buffer._owner;
        Length = buffer.Length;
        buffer._owner = MemoryOwner<int>.Empty;
    }
    public SpirvSpan AsSpan() => new(Span);
    public SpirvMemory AsMemory() => new(this);

}
