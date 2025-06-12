using CommunityToolkit.HighPerformance.Buffers;
using Stride.Shaders.Spirv.Core.Parsing;
using System.Numerics;

namespace Stride.Shaders.Spirv.Core.Buffers;

/// <summary>
/// A common SPIR-V buffer containing a header.
/// </summary>
public class SpirvBuffer : IMutSpirvBuffer, IDisposable
{
    /// <summary>
    /// Reusable buffer containing the SPIR-V code
    /// </summary>
    MemoryOwner<int> _owner;
    public int Length { get; protected set; }
    public Span<int> Span => _owner.Span[..Length];
    public Memory<int> Memory => _owner.Memory[..Length];
    public bool HasHeader => true;
    public RefHeader Header
    {
        get => new(_owner.Span[..5]);
        set
        {
            value.Words.CopyTo(Header.Words);
        }
    }

    public Span<int> InstructionSpan => Span[5..];
    public Memory<int> InstructionMemory => Memory[5..];

    public int InstructionCount => new SpirvReader(Memory).Count;

    public Instruction FindInstructionByResultId(int resultId)
    {
        foreach (var instruction in this)
        {
            if (instruction.ResultId == resultId)
                return instruction;
        }

        throw new InvalidOperationException();
    }

    public Instruction this[int index]
    {
        get
        {
            int id = 0;
            int wid = 5;
            while (id < index)
            {
                wid += Span[wid] >> 16;
                id++;
            }
            return new Instruction(Memory.Slice(wid, Span[wid] >> 16));
        }
    }

    public SpirvBuffer(int initialSize = 32)
    {
        _owner = MemoryOwner<int>.Allocate(initialSize, AllocationMode.Clear);
        Header = Header with
        {
            MagicNumber = Spv.Specification.MagicNumber,
            VersionNumber = new(1, 3)
        };
        Length = 5;
    }
    public SpirvBuffer(Memory<int> memory)
    {
        _owner = MemoryOwner<int>.Allocate(memory.Length, AllocationMode.Clear);
        memory.CopyTo(_owner.Memory);
        Header = Header with
        {
            MagicNumber = Spv.Specification.MagicNumber,
            VersionNumber = new(1, 3)
        };
        Length = _owner.Length;
    }
    public SpirvBuffer(Span<int> span)
    {
        _owner = MemoryOwner<int>.Allocate(span.Length, AllocationMode.Clear);
        span.CopyTo(_owner.Span);
        Header = Header with
        {
            MagicNumber = Spv.Specification.MagicNumber,
            VersionNumber = new(1, 3)
        };
        Length = _owner.Length;
    }


    public InstructionEnumerator GetEnumerator() => new(InstructionMemory, HasHeader);

    public void Sort()
    {
        var sorted = new OrderedEnumerator(this);
        var other = MemoryOwner<int>.Allocate(Length, AllocationMode.Clear);
        var pos = 5;
        while (sorted.MoveNext())
        {
            sorted.Current.Memory.CopyTo(other.Memory[pos..]);
            pos += sorted.Current.WordCount;
        }
        _owner.Span[0..5].CopyTo(other.Span[0..5]);
        _owner.Dispose();
        _owner = other;
    }
    public SpirvSpan AsSpan() => new(Span);
    public SpirvMemory AsMemory() => new(Memory);

    public Instruction Add(Span<int> instruction)
    {
        var result = Insert(Length, instruction);
        if (result.ResultId is int resultId && resultId >= Header.Bound)
            Header = Header with { Bound = resultId + 1 };
        return result;
    }

    public void Remove(int position)
    {
        if(position < 5 && position > Length)
            throw new ArgumentOutOfRangeException($"Can't remove at position {position}");
        var size = Span[position] >> 16;
        Span[(position + size)..].CopyTo(Span[position..]);
        Length -= size;
    }
    public Instruction Insert(int start, ReadOnlySpan<int> words)
    {
        Expand(words.Length);
        if (start == Length)
            words.CopyTo(_owner.Span[start..]);
        else
        {
            var slice = _owner.Span[start..Length];
            slice.CopyTo(_owner.Span[(start + words.Length)..]);
            words.CopyTo(_owner.Span.Slice(start, words.Length));
        }
        Length += words.Length;
        return new(Memory[start..(start + words.Length)]);
    }

    void Expand(int size)
    {
        if (Length + size > _owner.Length)
        {
            var n = MemoryOwner<int>.Allocate((int)BitOperations.RoundUpToPowerOf2((uint)(Length + size)), AllocationMode.Clear);
            _owner.Span.CopyTo(n.Span);
            var toDispose = _owner;
            _owner = n;
            toDispose.Dispose();
        }
    }

    internal void Add<TBuff>(TBuff buffer)
        where TBuff : ISpirvBuffer
    {
        Expand(buffer.InstructionSpan.Length);
        buffer.InstructionSpan.CopyTo(_owner.Span[Length..]);
        Length += buffer.InstructionSpan.Length;
    }


    public static SpirvBuffer Merge<T1, T2>(T1 left, T2 right)
        where T1 : ISpirvBuffer
        where T2 : ISpirvBuffer
    {
        var buff = new SpirvBuffer(left.Length + right.Length + 5);
        buff.Add(left);
        buff.Add(right);
        foreach (var e in buff)
            if (e.ResultId is int r && buff.Header.Bound < r + 1)
                buff.Header = buff.Header with { Bound = r + 1 };
        return buff;
    }

    public void Dispose() => _owner.Dispose();

}
