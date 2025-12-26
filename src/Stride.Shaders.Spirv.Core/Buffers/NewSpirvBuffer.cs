#pragma warning disable CS9264 // Non-nullable property must contain a non-null value when exiting constructor. Consider adding the 'required' modifier, or declaring the property as nullable, or safely handling the case where 'field' is null in the 'get' accessor.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using Stride.Shaders.Spirv.Core.Parsing;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Core.Buffers;


public interface IMemoryInstruction
{
    ref OpData OpData { get; }
    MemoryOwner<int> InstructionMemory { get; }
    public void UpdateInstructionMemory();
}

public struct OpData : IDisposable, IComparable<OpData>
{
    public MemoryOwner<int> Memory { get; internal set { field?.Dispose(); field = value; } }
    public readonly Op Op => (Op)(Memory.Span[0] & 0xFFFF);

    public readonly int? IdResult
    {
        get => InstructionInfo.GetInfo(this).GetResultIndex(out var index) ? Memory.Span[index + 1] : null;
        set
        {
            if (InstructionInfo.GetInfo(this).GetResultIndex(out var index) && value is not null)
                Memory.Span[index + 1] = value ?? 0;
        }
    }
    public readonly int? IdResultType
    {
        get  => InstructionInfo.GetInfo(this).GetResultTypeIndex(out var index) ? Memory.Span[index + 1] : null;
        set
        {
            if (InstructionInfo.GetInfo(this).GetResultTypeIndex(out var index) && value is not null)
                Memory.Span[index + 1] = value ?? 0;
        }
    }

    public OpData()
    {
        Memory = MemoryOwner<int>.Empty;
    }

    public OpData(MemoryOwner<int> memory)
    {
        Memory = memory;
    }
    public OpData(Span<int> memory)
    {
        Memory = MemoryOwner<int>.Allocate(memory.Length);
        memory.CopyTo(Memory.Span);
    }

    public readonly void Dispose() => Memory.Dispose();

    public readonly SpvOperand Get(string name)
    {
        foreach (var o in this)
        {
            if (name == o.Name && (o.Kind.ToString().Contains("Literal") || o.Kind.ToString().Contains("Id")))
                return o;
        }
        throw new Exception($"No operand '{name}' in op {Op}");
    }

    public readonly bool TryGet<T>(string name, out T operand)
    {
        foreach (var o in this)
        {
            if (name == o.Name)
            {
                operand = o.To<T>();
                return true;
            }
        }
        operand = default!;
        return false;
    }

    public readonly T Get<T>(string name)
    {
        foreach (var o in this)
        {
            if (name == o.Name && (o.Kind.ToString().Contains("Literal") || o.Kind.ToString().Contains("Id")))
                return o.To<T>();
        }
        throw new Exception($"No operand '{name}' in op {Op}");
    }
    public readonly T GetEnum<T>(string name)
        where T : Enum
    {
        foreach (var o in this)
        {
            if (name == o.Name && !o.Kind.ToString().Contains("Literal") && !o.Kind.ToString().Contains("Id"))
                return o.ToEnum<T>();
        }
        throw new Exception($"No enum operand '{name}' in op {Op}");
    }

    public readonly OpDataEnumerator GetEnumerator() => new(Memory.Span);

    public readonly int CompareTo(OpData other)
    {
        var group = InstructionInfo.GetGroupOrder(this);
        var otherGroup = InstructionInfo.GetGroupOrder(other);
        return group.CompareTo(otherGroup);
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        // Check for IdResult first
        foreach (var op in this)
        {
            switch (op.Kind)
            {
                case OperandKind.IdResult:
                    sb.Append("%");
                    sb.Append(op.Words[0]);
                    sb.Append(" = ");
                    break;
            }
        }

        sb.Append(Op);
        foreach (var op in this)
        {
            if (op.Kind == OperandKind.IdResult)
                continue;
            sb.Append(" ");
            switch (op.Kind)
            {
                case OperandKind.IdResultType:
                case OperandKind.IdRef:
                    for (var index = 0; index < op.Words.Length; index++)
                    {
                        if (index > 0)
                            sb.Append(" ");
                        sb.Append("%");
                        sb.Append(op.Words[index]);
                    }
                    break;
                case OperandKind.LiteralInteger when op.Words.Length == 1:
                    foreach (var e in op.Words)
                        sb.Append(e);
                    break;
                case OperandKind.LiteralString:
                    sb.Append('"');
                    sb.Append(op.ToLiteral<string>());
                    sb.Append('"');
                    break;
                case OperandKind k when k.IsEnum():
                    for (var index = 0; index < op.Words.Length; index++)
                    {
                        if (index > 0)
                            sb.Append(" ");
                        sb.Append(k.ConvertEnumValueToString(op.Words[index]));
                    }
                    break;
                default:
                    sb.Append($"unknown_{op.Kind}");
                    if (op.Words.Length != 1)
                        sb.Append($"_{op.Words.Length}");
                    break;
            }
        }
        return sb.ToString();
    }
}


public record struct OpDataIndex(int Index, NewSpirvBuffer Buffer)
{
    public readonly Op Op => Data.Op;
    public readonly ref OpData Data => ref Buffer.GetRef(Index);
}

public sealed class NewSpirvBuffer() : IDisposable, IEnumerable<OpDataIndex>
{
    public SpirvHeader Header { get; set; } = new("1.4", 0, 1);
    List<OpData> Instructions { get; set; } = [];
    public int Count => Instructions.Count;

    // internal ref OpData this[int index] => ref CollectionsMarshal.AsSpan(Instructions)[index];
    public OpDataIndex this[int index] => new(index, this);

    public List<OpData> Slice(int start, int length) => Instructions.Slice(start, length);


    public ref OpData GetRef(int index) => ref CollectionsMarshal.AsSpan(Instructions)[index];

    public NewSpirvBuffer(Span<int> span) : this()
    {
        if (span[0] == MagicNumber)
            Header = SpirvHeader.Read(span);
        var instructions = span[5..];

        int wid = 0;
        while (wid < instructions.Length)
        {
            Add(new(instructions.Slice(wid, instructions[wid] >> 16)));
            wid += instructions[wid] >> 16;
        }
    }


    void UpdateBound(OpData data)
    {
        if (data.IdResult is int index && index >= Header.Bound)
            Header = Header with { Bound = index + 1 };
    }

    public OpDataIndex Add(OpData data)
    {
        Instructions.Add(data);
        UpdateBound(data);
        return new OpDataIndex(Instructions.Count - 1, this);
    }

    public OpDataIndex Insert(int index, OpData data)
    {
        Instructions.Insert(index, data);
        UpdateBound(data);
        return new OpDataIndex(index, this);
    }

    public OpData Add<T>(in T instruction) where T : struct, IMemoryInstruction, allows ref struct
    {
        Instructions.Add(new(instruction.InstructionMemory));
        UpdateBound(Instructions[^1]);
        return Instructions[^1];
    }

    public NewSpirvBuffer FluentAdd<T>(in T instruction) where T : struct, IMemoryInstruction, allows ref struct
    {
        Instructions.Add(new(instruction.InstructionMemory));
        var tmp = instruction;
        UpdateBound(Instructions[^1]);
        return this;
    }
    public NewSpirvBuffer FluentAdd<T>(in T instruction, out T result) where T : struct, IMemoryInstruction, allows ref struct
    {
        result = instruction;
        Instructions.Add(new(instruction.InstructionMemory));
        UpdateBound(Instructions[^1]);
        return this;
    }

    public T Insert<T>(int index, in T data)
        where T : struct, IMemoryInstruction, allows ref struct
    {
        Instructions.Insert(index, new(data.InstructionMemory));
        UpdateBound(Instructions[^1]);
        return data;
    }
    public OpData InsertData<T>(int index, in T data)
        where T : struct, IMemoryInstruction, allows ref struct
    {
        var result = new OpData(data.InstructionMemory);
        Instructions.Insert(index, result);
        UpdateBound(result);
        return result;
    }

    /// <summary>
    /// Removes an instruction at a certain index. 
    /// <br/>Be careful when using this method, as it will invalidate any references to the removed instruction.
    /// </summary>
    /// <param name="index"></param>
    /// <returns>true if the instruction was successfully removed</returns>
    public void RemoveAt(int index, bool dispose = true)
    {
        if (index < 0 || index >= Instructions.Count)
            throw new ArgumentOutOfRangeException();
        if (dispose)
            Instructions[index].Dispose();
        Instructions.RemoveAt(index);
    }

    public void RemoveRange(int index, int count, bool dispose = true)
    {
        if (dispose)
        {
            for (int i = index; i < index + count; ++i)
                Instructions[i].Dispose();
        }
        Instructions.RemoveRange(index, count);
    }

    public void InsertRange(int index, ReadOnlySpan<OpData> source)
    {
        Instructions.InsertRange(index, source);
        for (int i = index; i < index + source.Length; ++i)
            UpdateBound(Instructions[i]);
    }

    public OpData Replace(int index, OpData i)
    {
        if (index < 0 || index >= Instructions.Count)
            throw new InvalidOperationException();

        Instructions[index].Dispose();
        Instructions[index] = i;
        UpdateBound(Instructions[index]);
        return Instructions[index];
    }

    public OpData Replace<T>(int index, in T instruction) where T : struct, IMemoryInstruction, allows ref struct
    {
        if (index < 0 || index >= Instructions.Count)
            throw new InvalidOperationException();

        Instructions[index].Dispose();
        Instructions[index] = new(instruction.InstructionMemory);
        UpdateBound(Instructions[index]);
        return Instructions[index];
    }

    public Enumerator GetEnumerator() => new(this);

    public struct Enumerator(NewSpirvBuffer buffer) : IEnumerator<OpDataIndex>
    {
        readonly NewSpirvBuffer buffer = buffer;
        private readonly List<OpData> list = buffer.Instructions;
        private int index = -1;

        public readonly OpDataIndex Current => new(index, buffer);

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            if (index < list.Count - 1)
            {
                index++;
                return true;
            }
            return false;
        }

        public void Reset()
        {
            index = -1;
        }
    }

    public void Sort()
    {
        // Note: We don't use List.Sort because it's not stable.
        //       This is especially important for type depending on another type.
        var sortedInstructions = Instructions.OrderBy(InstructionInfo.GetGroupOrder).ToList();
        Instructions.Clear();
        Instructions.AddRange(sortedInstructions);
    }

    public byte[] ToBytecode()
    {
        return MemoryMarshal.AsBytes(ToBuffer().Span).ToArray();
    }

    public SpanOwner<int> ToBuffer()
    {
        var result = SpanOwner<int>.Allocate(5 + Instructions.Sum(i => i.Memory.Length));
        var span = result.Span;
        Header.WriteTo(span);
        var offset = 5;
        foreach (var instruction in Instructions)
        {
            instruction.Memory.Span.CopyTo(span[offset..]);
            offset += instruction.Memory.Length;
        }
        return result;
    }

    public bool TryGetInstructionById(int typeId, out OpDataIndex instruction)
    {
        foreach (var op in this)
        {
            var info = InstructionInfo.GetInfo(op.Op);
            if (info.GetResultIndex(out int index) && index < op.Data.Memory.Length && op.Data.Memory.Span[index + 1] == typeId)
            {
                instruction = op;
                return true;
            }
        }
        instruction = default;
        return false;
    }

    public void Dispose()
    {
        foreach (var instruction in Instructions)
            instruction.Dispose();
        Instructions.Clear();
    }

    public static NewSpirvBuffer Merge(NewSpirvBuffer buffer1, NewSpirvBuffer buffer2)
    {
        var result = new NewSpirvBuffer
        {
            Header = new SpirvHeader("1.4", Math.Max(buffer1.Header.Generator, buffer2.Header.Generator), Math.Max(buffer1.Header.Bound, buffer2.Header.Bound))
        };
        result.Instructions.AddRange(buffer1.Instructions);
        result.Instructions.AddRange(buffer2.Instructions);
        return result;
    }

    IEnumerator<OpDataIndex> IEnumerable<OpDataIndex>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public static class IMemoryInstructionExtensions
{
    /// <summary>
    /// Gets information for the instruction operation.
    /// </summary>
    /// <param name="op"></param>
    /// <returns></returns>
    public static LogicalOperandArray GetInfo<T>(this T op)
        where T : struct, IMemoryInstruction, allows ref struct
    {
        if (!Unsafe.IsNullRef(ref op.OpData))
            return InstructionInfo.GetInfo(op.OpData);
        return InstructionInfo.GetInfo(op.InstructionMemory.Span);
    }
}