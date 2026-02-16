using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Core.Buffers;

public sealed class SpirvBuffer() : IDisposable, IEnumerable<OpDataIndex>
{
    List<OpData> Instructions { get; set; } = [];
    public int Count => Instructions.Count;

    // internal ref OpData this[int index] => ref CollectionsMarshal.AsSpan(Instructions)[index];
    public OpDataIndex this[int index] => new(index, this);

    public List<OpData> Slice(int start, int length) => Instructions.Slice(start, length);


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

    public void InsertRange(int index, params ReadOnlySpan<OpData> source)
    {
        Instructions.InsertRange(index, source);
    }

    public OpData Replace(int index, OpData i, bool dispose = true)
    {
        if (index < 0 || index >= Instructions.Count)
            throw new InvalidOperationException();

        if (dispose)
            Instructions[index].Dispose();
        Instructions[index] = i;
        return Instructions[index];
    }

    public OpData Replace<T>(int index, in T instruction, bool dispose = true) where T : struct, IMemoryInstruction, allows ref struct
    {
        if (index < 0 || index >= Instructions.Count)
            throw new InvalidOperationException();

        if (dispose)
            Instructions[index].Dispose();
        Instructions[index] = new(instruction.InstructionMemory);
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

    public static SpirvBuffer Merge(SpirvBuffer buffer1, SpirvBuffer buffer2)
    {
        var result = new SpirvBuffer();
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