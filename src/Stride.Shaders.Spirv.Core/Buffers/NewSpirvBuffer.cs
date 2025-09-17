#pragma warning disable CS9264 // Non-nullable property must contain a non-null value when exiting constructor. Consider adding the 'required' modifier, or declaring the property as nullable, or safely handling the case where 'field' is null in the 'get' accessor.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using Stride.Shaders.Spirv.Core.Parsing;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Core.Buffers;


public interface IMemoryInstruction
{
    OpDataIndex? DataIndex { get; set; }
    MemoryOwner<int> InstructionMemory { get; }
    public void UpdateInstructionMemory();
}

public struct OpData : IDisposable, IComparable<OpData>
{
    public MemoryOwner<int> Memory { get; internal set { field?.Dispose(); field = value; } }
    public readonly Op Op => (Op)(Memory.Span[0] & 0xFFFF);

    public readonly int? IdResult => InstructionInfo.GetInfo(this).GetResultIndex(out var index) ? Memory.Span[index + 1] : null;
    public readonly int? IdResultType => InstructionInfo.GetInfo(this).GetResultTypeIndex(out var index) ? Memory.Span[index + 1] : null;

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


    public readonly bool TryGetOperand<T>(string name, out T operand)
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
}


public record struct OpDataIndex(int Index, NewSpirvBuffer Buffer)
{
    public readonly Op Op => Data.Op;
    public readonly ref OpData Data => ref Buffer.GetRef(Index);
}

public sealed class NewSpirvBuffer() : IDisposable
{
    public SpirvHeader Header { get; set; } = new("1.4", 0, 1);
    List<OpData> Instructions { get; set; } = [];
    public int Count => Instructions.Count;

    // internal ref OpData this[int index] => ref CollectionsMarshal.AsSpan(Instructions)[index];
    public OpDataIndex this[int index] => new(index, this);


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


    public void Add(OpData data)
    {
        if (InstructionInfo.GetInfo(data).GetResultIndex(out int index) && index >= Header.Bound)
            Header = Header with { Bound = data.Memory.Span[index] + 1 };
        Instructions.Add(data);
    }

    public OpData Add<T>(in T instruction) where T : struct, IMemoryInstruction
    {
        if (instruction.DataIndex is OpDataIndex odi)
        {
            if (odi.Buffer == this)
                return odi.Data;
            else
                Instructions.Add(new(instruction.InstructionMemory));
        }
        else Instructions.Add(new(instruction.InstructionMemory));
        return Instructions[^1];
    }

    public void AddRef<T>(ref T instruction) where T : struct, IMemoryInstruction
    {
        if (instruction.DataIndex is OpDataIndex odi)
        {
            if (odi.Buffer == this)
                return;
            else
                Instructions.Add(new(instruction.InstructionMemory));
        }
        else Instructions.Add(new(instruction.InstructionMemory));
        instruction.DataIndex = new(Instructions.Count - 1, this);

        if (instruction.GetInfo().GetResultIndex(out int index) && index >= Header.Bound)
            Header = Header with { Bound = instruction.InstructionMemory.Span[index] + 1 };
    }
    public NewSpirvBuffer FluentAdd<T>(in T instruction) where T : struct, IMemoryInstruction
    {
        if (instruction.DataIndex is OpDataIndex odi)
        {
            if (odi.Buffer == this)
                return this;
            else
                Instructions.Add(new(instruction.InstructionMemory));
        }
        else Instructions.Add(new(instruction.InstructionMemory));
        var tmp = instruction;
        if (tmp.GetInfo().GetResultIndex(out int index) && index >= Header.Bound)
            Header = Header with { Bound = tmp.InstructionMemory.Span[index] + 1 };
        return this;
    }
    public NewSpirvBuffer FluentAdd<T>(in T instruction, out T result) where T : struct, IMemoryInstruction
    {
        result = instruction;
        if (instruction.DataIndex is OpDataIndex odi)
        {
            if (odi.Buffer == this)
                return this;
            else
                Instructions.Add(new(instruction.InstructionMemory));
        }
        else Instructions.Add(new(instruction.InstructionMemory));
        var tmp = instruction;
        if (tmp.GetInfo().GetResultIndex(out int index) && index >= Header.Bound)
            Header = Header with { Bound = instruction.InstructionMemory.Span[index] + 1 };
        return this;
    }

    public void Insert(int index, OpData data)
        => Instructions.Insert(index, data);
    public T Insert<T>(int index, in T data)
        where T : struct, IMemoryInstruction
    {
        Instructions.Insert(index, new(data.InstructionMemory));
        var tmp = data;
        if (tmp.GetInfo().GetResultIndex(out int rid) && rid >= Header.Bound)
            Header = Header with { Bound = tmp.InstructionMemory.Span[rid] + 1 };
        return data;
    }
    public OpData InsertData<T>(int index, in T data)
        where T : struct, IMemoryInstruction
    {
        var result = new OpData(data.InstructionMemory);
        Instructions.Insert(index, result);
        return result;
    }

    /// <summary>
    /// Removes an instruction at a certain index. 
    /// <br/>Be careful when using this method, as it will invalidate any references to the removed instruction.
    /// </summary>
    /// <param name="index"></param>
    /// <returns>true if the instruction was successfully removed</returns>
    public bool RemoveAt(int index)
    {
        if (index < 0 || index >= Instructions.Count)
            return false;
        Instructions[index].Dispose();
        Instructions.RemoveAt(index);
        return true;
    }

    public Enumerator GetEnumerator() => new(this);

    public ref struct Enumerator(NewSpirvBuffer buffer)
    {
        readonly NewSpirvBuffer buffer = buffer;
        private readonly List<OpData> list = buffer.Instructions;
        private int index = -1;

        public readonly OpDataIndex Current => new(index, buffer);

        public bool MoveNext()
        {
            if (index < list.Count - 1)
            {
                index++;
                return true;
            }
            return false;
        }
    }

    public void Sort() => Instructions.Sort(static (a, b) => a.CompareTo(b));

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
}


public static class IMemoryInstructionExtensions
{
    /// <summary>
    /// Gets information for the instruction operation.
    /// </summary>
    /// <param name="op"></param>
    /// <returns></returns>
    public static LogicalOperandArray GetInfo<T>(this ref T op)
        where T : struct, IMemoryInstruction
    {
        Decoration? decoration = op switch
        {
            OpDecorate opd => opd.Decoration,
            OpMemberDecorate opd => opd.Decoration,
            _ => null
        };

        return InstructionInfo.GetInfo((Op)(op.InstructionMemory.Span[0] & 0xFFFF), decoration);
    }
}