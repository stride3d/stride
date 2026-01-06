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
    void Attach(OpDataIndex dataIndex);
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

public record SpirvBytecode(SpirvHeader Header, NewSpirvBuffer Buffer) : IDisposable
{
    public SpirvBytecode(NewSpirvBuffer buffer) : this(CreateHeader(buffer), buffer)
    {
    }

    public void Dispose() => Buffer.Dispose();

    public static SpirvHeader CreateHeader(NewSpirvBuffer buffer)
    {
        var header = new SpirvHeader("1.4", 0, 1);
        var bound = 1;
        foreach (var i in buffer)
        {
            ref var data = ref i.Data;
            if (data.IdResult is int index && index >= bound)
                bound = index + 1;
        }
        return new SpirvHeader("1.4", 0, bound);
    }

    public Span<byte> ToBytecode()
    {
        return CreateBytecodeFromBuffers(Header, false, Buffer);
    }

    public static SpirvBytecode CreateBufferFromBytecode(Span<byte> span)
    {
        return CreateBufferFromBytecode(MemoryMarshal.Cast<byte, int>(span));
    }

    public static SpirvBytecode CreateBufferFromBytecode(Span<int> span)
    {
        if (span[0] != MagicNumber)
            throw new InvalidOperationException("SPIRV Magic number not found");
        
        var header = SpirvHeader.Read(span);

        return new(header, new NewSpirvBuffer(span[SpirvHeader.IntSpanSize..]));
    }

    public static SpanOwner<int> CreateSpanFromBuffers(SpirvHeader header, bool computeBounds, params Span<NewSpirvBuffer> buffers)
    {
        int instructionsMemorySize = 0;
        var bound = header.Bound;
        foreach (var buffer in buffers)
        {
            foreach (var i in buffer)
            {
                ref var data = ref i.Data;
                if (data.IdResult is int index && index >= bound)
                    bound = index + 1;

                instructionsMemorySize += data.Memory.Length;
            }
        }

        header = header with { Bound = bound };

        var result = SpanOwner<int>.Allocate(5 + instructionsMemorySize);
        var span = result.Span;
        header.WriteTo(span);
        var offset = 5;
        foreach (var buffer in buffers)
        {
            foreach (var i in buffer)
            {
                i.Data.Memory.Span.CopyTo(span[offset..]);
                offset += i.Data.Memory.Length;
            }
        }
        return result;
    }

    public static Span<byte> CreateBytecodeFromBuffers(params Span<NewSpirvBuffer> buffers)
    {
        return CreateBytecodeFromBuffers(new("1.4", 0, 1), true, buffers);
    }

    public static Span<byte> CreateBytecodeFromBuffers(SpirvHeader header, bool computeBounds, params Span<NewSpirvBuffer> buffers)
    {
        return MemoryMarshal.AsBytes(CreateSpanFromBuffers(header, computeBounds, buffers).Span);
    }
}

public sealed class NewSpirvBuffer() : IDisposable, IEnumerable<OpDataIndex>
{
    List<OpData> Instructions { get; set; } = [];
    public int Count => Instructions.Count;

    // internal ref OpData this[int index] => ref CollectionsMarshal.AsSpan(Instructions)[index];
    public OpDataIndex this[int index] => new(index, this);

    public List<OpData> Slice(int start, int length) => Instructions.Slice(start, length);


    public ref OpData GetRef(int index) => ref CollectionsMarshal.AsSpan(Instructions)[index];

    public NewSpirvBuffer(Span<int> instructions) : this()
    {
        if (instructions.Length > 0 && instructions[0] == MagicNumber)
            throw new InvalidOperationException();

        int wid = 0;
        while (wid < instructions.Length)
        {
            Add(new(instructions.Slice(wid, instructions[wid] >> 16)));
            wid += instructions[wid] >> 16;
        }
    }

    public OpDataIndex Add(OpData data)
    {
        Instructions.Add(data);
        return new OpDataIndex(Instructions.Count - 1, this);
    }

    public OpDataIndex Insert(int index, OpData data)
    {
        Instructions.Insert(index, data);
        return new OpDataIndex(index, this);
    }

    public OpData Add<T>(in T instruction) where T : struct, IMemoryInstruction, allows ref struct
    {
        Instructions.Add(new(instruction.InstructionMemory));
        return Instructions[^1];
    }

    public NewSpirvBuffer FluentAdd<T>(in T instruction) where T : struct, IMemoryInstruction, allows ref struct
    {
        Instructions.Add(new(instruction.InstructionMemory));
        var tmp = instruction;
        return this;
    }
    public NewSpirvBuffer FluentAdd<T>(in T instruction, out T result) where T : struct, IMemoryInstruction, allows ref struct
    {
        result = instruction;
        Instructions.Add(new(instruction.InstructionMemory));
        instruction.Attach(new(Instructions.Count - 1, this));
        return this;
    }

    public T Insert<T>(int index, in T instruction)
        where T : struct, IMemoryInstruction, allows ref struct
    {
        Instructions.Insert(index, new(instruction.InstructionMemory));
        instruction.Attach(new(index, this));
        return instruction;
    }
    public OpData InsertData<T>(int index, in T instruction)
        where T : struct, IMemoryInstruction, allows ref struct
    {
        var result = new OpData(instruction.InstructionMemory);
        Instructions.Insert(index, result);
        instruction.Attach(new(index, this));
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
    }

    public OpData Replace(int index, OpData i)
    {
        if (index < 0 || index >= Instructions.Count)
            throw new InvalidOperationException();

        Instructions[index].Dispose();
        Instructions[index] = i;
        return Instructions[index];
    }

    public OpData Replace<T>(int index, in T instruction) where T : struct, IMemoryInstruction, allows ref struct
    {
        if (index < 0 || index >= Instructions.Count)
            throw new InvalidOperationException();

        Instructions[index].Dispose();
        Instructions[index] = new(instruction.InstructionMemory);
        return Instructions[index];
    }

    public NewSpirvBuffer FluentReplace<T>(int index, in T instruction, out T result) where T : struct, IMemoryInstruction, allows ref struct
    {
        Replace(index, instruction);
        instruction.Attach(new(index, this));
        result = instruction;
        return this;
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

    public Span<byte> ToBytecode()
    {
        return SpirvBytecode.CreateBytecodeFromBuffers(this);
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
        var result = new NewSpirvBuffer();
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