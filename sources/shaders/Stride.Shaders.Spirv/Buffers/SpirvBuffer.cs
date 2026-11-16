using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Core.Buffers;

public sealed class SpirvBuffer() : IDisposable, IEnumerable<OpDataIndex>
{
    List<OpData> Instructions { get; set; } = [];
    public int Count => Instructions.Count;

    /// <summary>
    /// Increases when an instruction is inserted before the end, removed or replaced, or when the buffer is sorted: the positions of the instructions may have changed.
    /// </summary>
    public int StructureVersion { get; private set; }

    // internal ref OpData this[int index] => ref CollectionsMarshal.AsSpan(Instructions)[index];
    public OpDataIndex this[int index] => new(index, this);

    public ref OpData GetRef(int index) => ref CollectionsMarshal.AsSpan(Instructions)[index];

    public SpirvBuffer(Span<int> instructions) : this()
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
        InsertingAt(index);
        Instructions.Insert(index, data);
        return new OpDataIndex(index, this);
    }

    public T Add<T>(in T instruction) where T : struct, IMemoryInstruction, allows ref struct
    {
        Instructions.Add(new(instruction.InstructionMemory));
        instruction.Attach(new(Instructions.Count - 1, this));
        return instruction;
    }

    public OpData AddData<T>(in T instruction) where T : struct, IMemoryInstruction, allows ref struct
    {
        Instructions.Add(new(instruction.InstructionMemory));
        return Instructions[^1];
    }

    public SpirvBuffer FluentAdd<T>(in T instruction) where T : struct, IMemoryInstruction, allows ref struct
    {
        Instructions.Add(new(instruction.InstructionMemory));
        var tmp = instruction;
        return this;
    }
    public SpirvBuffer FluentAdd<T>(in T instruction, out T result) where T : struct, IMemoryInstruction, allows ref struct
    {
        result = instruction;
        Instructions.Add(new(instruction.InstructionMemory));
        instruction.Attach(new(Instructions.Count - 1, this));
        return this;
    }

    public T Insert<T>(int index, in T instruction)
        where T : struct, IMemoryInstruction, allows ref struct
    {
        InsertingAt(index);
        Instructions.Insert(index, new(instruction.InstructionMemory));
        instruction.Attach(new(index, this));
        return instruction;
    }
    public OpData InsertData<T>(int index, in T instruction)
        where T : struct, IMemoryInstruction, allows ref struct
    {
        var result = new OpData(instruction.InstructionMemory);
        InsertingAt(index);
        Instructions.Insert(index, result);
        instruction.Attach(new(index, this));
        return result;
    }

    /// <summary>
    /// Removes an instruction at a certain index. 
    /// <br/>Be careful when using this method, as it will invalidate any references to the removed instruction.
    /// </summary>
    public void RemoveAt(int index)
    {
        if (index < 0 || index >= Instructions.Count)
            throw new ArgumentOutOfRangeException();
        Instructions[index].Dispose();
        Instructions.RemoveAt(index);
        StructureVersion++;
    }

    public void RemoveRange(int index, int count)
    {
        for (int i = index; i < index + count; ++i)
            Instructions[i].Dispose();
        Instructions.RemoveRange(index, count);
        StructureVersion++;
    }

    /// <summary>
    /// Removes the instructions that match, keeping the order of the others.
    /// </summary>
    /// <param name="match">Called once per instruction, in order, before any instruction after it moves.</param>
    public void RemoveAll(Func<OpDataIndex, bool> match)
    {
        var kept = 0;
        for (var index = 0; index < Instructions.Count; index++)
        {
            if (match(new(index, this)))
                Instructions[index].Dispose();
            else
                Instructions[kept++] = Instructions[index];
        }
        Instructions.RemoveRange(kept, Instructions.Count - kept);
        StructureVersion++;
    }

    /// <summary>
    /// Moves the instructions of <paramref name="source"/> to <paramref name="index"/>, and leaves <paramref name="source"/> empty.
    /// </summary>
    public void InsertRange(int index, SpirvBuffer source)
    {
        InsertingAt(index);
        Instructions.InsertRange(index, source.Instructions);
        source.Instructions.Clear();
    }

    /// <summary>
    /// Moves a range of instructions out of this buffer, into a new buffer.
    /// </summary>
    public SpirvBuffer ExtractRange(int index, int count)
    {
        var result = new SpirvBuffer();
        result.Instructions.AddRange(CollectionsMarshal.AsSpan(Instructions).Slice(index, count));
        Instructions.RemoveRange(index, count);
        StructureVersion++;
        return result;
    }

    private void InsertingAt(int index)
    {
        if (index < Instructions.Count)
            StructureVersion++;
    }

    public OpData Replace(int index, OpData i)
    {
        if (index < 0 || index >= Instructions.Count)
            throw new InvalidOperationException();

        Instructions[index].Dispose();
        Instructions[index] = i;
        StructureVersion++;
        return Instructions[index];
    }

    public OpData Replace<T>(int index, in T instruction) where T : struct, IMemoryInstruction, allows ref struct
    {
        if (index < 0 || index >= Instructions.Count)
            throw new InvalidOperationException();

        Instructions[index].Dispose();
        Instructions[index] = new(instruction.InstructionMemory);
        StructureVersion++;
        return Instructions[index];
    }

    public SpirvBuffer FluentReplace<T>(int index, in T instruction, out T result) where T : struct, IMemoryInstruction, allows ref struct
    {
        Replace(index, instruction);
        instruction.Attach(new(index, this));
        result = instruction;
        return this;
    }

    public Enumerator GetEnumerator() => new(this);

    public struct Enumerator(SpirvBuffer buffer) : IEnumerator<OpDataIndex>
    {
        readonly SpirvBuffer buffer = buffer;
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
        StructureVersion++;
    }

    public Span<byte> ToBytecode()
    {
        return SpirvBytecode.CreateBytecodeFromBuffers(this);
    }

    /// <summary>
    /// Gets type ID from value ID. Only work if the defining type instruction ID has a ResultType operand.
    /// </summary>
    public bool TryGetTypeId(int id, out int typeId)
    {
        typeId = default;
        if (TryGetInstructionById(id, out var instruction))
        {
            var info = InstructionInfo.GetInfo(instruction.Op);
            if (info.GetResultTypeIndex(out int typeIndex) && typeIndex < instruction.Words.Length)
            {
                typeId = instruction.Words[typeIndex + 1];
                return true;
            }
        }

        return false;
    }

    public bool TryGetInstructionById(int id, out OpDataIndex instruction)
    {
        foreach (var op in this)
        {
            var info = InstructionInfo.GetInfo(op.Op);
            if (info.GetResultIndex(out int index) && index < op.Words.Length && op.Words[index + 1] == id)
            {
                instruction = op;
                return true;
            }
        }
        instruction = default;
        return false;
    }

    /// <summary>
    /// Turns the words of an instruction into <see cref="Specification.Op.OpNop"/> words, keeping its size.
    /// </summary>
    public static void SetOpNop(Span<int> words)
    {
        words[0] = words.Length << 16;
        words[1..].Clear();
    }

    public void Dispose()
    {
        foreach (var instruction in Instructions)
            instruction.Dispose();
        Instructions.Clear();
    }

    /// <summary>
    /// Copies the instructions of two buffers into a new buffer.
    /// </summary>
    public static SpirvBuffer Merge(SpirvBuffer buffer1, SpirvBuffer buffer2)
    {
        var result = new SpirvBuffer();
        foreach (var i in buffer1)
            result.Add(new OpData(i.Words));
        foreach (var i in buffer2)
            result.Add(new OpData(i.Words));
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
