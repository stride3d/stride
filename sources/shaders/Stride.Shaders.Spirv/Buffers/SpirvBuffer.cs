using System.Collections;
using System.Runtime.InteropServices;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Core.Buffers;

/// <summary>
/// A list of SPIR-V instructions, whose words are stored in one array.
/// </summary>
/// <remarks>
/// The words of an instruction stay where they are while it keeps its size. An instruction that is inserted or grows gets its words
/// at the end of the array; the old words are garbage until the array is compacted.
/// </remarks>
public sealed class SpirvBuffer() : IDisposable, IEnumerable<OpDataIndex>
{
    private int[] words = [];
    private int wordCount;
    // Where the words of each instruction start, in the order of the instructions; the size is in the first word
    private readonly List<int> offsets = [];
    private int garbage;

    public int Count => offsets.Count;

    /// <summary>
    /// Increases when an instruction is inserted before the end, removed or replaced, or when the buffer is sorted: the positions of the instructions may have changed.
    /// </summary>
    public int StructureVersion { get; private set; }

    // Increases each time words are added, as that may move all of them: it ends the views that OpData took before
    internal int WordsVersion { get; private set; }

    public OpDataIndex this[int index] => new(index, this);

    public SpirvBuffer(ReadOnlySpan<int> instructions) : this()
    {
        if (instructions.Length > 0 && instructions[0] == MagicNumber)
            throw new InvalidOperationException();

        words = instructions.ToArray();
        wordCount = words.Length;
        for (var wid = 0; wid < wordCount; wid += words[wid] >> 16)
        {
            // A word count of 0 would never move on (a damaged file read from a cache)
            if (words[wid] >> 16 <= 0)
                throw new InvalidOperationException($"Invalid word count at word {wid}");
            offsets.Add(wid);
        }
    }

    /// <summary>
    /// The words of the instruction at <paramref name="index"/>. Take them again after the buffer changes.
    /// </summary>
    public Span<int> GetWords(int index)
    {
        var offset = offsets[index];
        return words.AsSpan(offset, words[offset] >> 16);
    }

    internal OpData GetData(int index)
    {
        var offset = offsets[index];
        return new(this, words.AsMemory(offset, words[offset] >> 16));
    }

    public OpDataIndex Add(OpData data) => Insert(Count, data.Memory.Span);

    public OpDataIndex Insert(int index, OpData data) => Insert(index, data.Memory.Span);

    public T Add<T>(in T instruction) where T : struct, IMemoryInstruction, allows ref struct
        => Insert(Count, instruction);

    public OpData AddData<T>(in T instruction) where T : struct, IMemoryInstruction, allows ref struct
        => Insert(Count, instruction.InstructionMemory.Span).Data;

    public SpirvBuffer FluentAdd<T>(in T instruction) where T : struct, IMemoryInstruction, allows ref struct
    {
        Insert(Count, instruction.InstructionMemory.Span);
        return this;
    }

    public SpirvBuffer FluentAdd<T>(in T instruction, out T result) where T : struct, IMemoryInstruction, allows ref struct
    {
        result = Insert(Count, instruction);
        return this;
    }

    /// <summary>
    /// Copies the words of <paramref name="instruction"/> to <paramref name="index"/>.
    /// </summary>
    /// <returns>The instruction, attached to the copy in this buffer.</returns>
    public T Insert<T>(int index, in T instruction)
        where T : struct, IMemoryInstruction, allows ref struct
    {
        var inserted = Insert(index, instruction.InstructionMemory.Span);
        var result = instruction;
        result.Attach(inserted);
        return result;
    }

    public OpData InsertData<T>(int index, in T instruction)
        where T : struct, IMemoryInstruction, allows ref struct
        => Insert(index, instruction.InstructionMemory.Span).Data;

    private OpDataIndex Insert(int index, ReadOnlySpan<int> instruction)
    {
        if (index < offsets.Count)
            StructureVersion++;
        offsets.Insert(index, Append(instruction));
        return new(index, this);
    }

    /// <summary>
    /// Removes an instruction at a certain index.
    /// <br/>Be careful when using this method, as it will invalidate any references to the removed instruction.
    /// </summary>
    public void RemoveAt(int index)
    {
        if (index < 0 || index >= offsets.Count)
            throw new ArgumentOutOfRangeException();
        RemoveRange(index, 1);
    }

    public void RemoveRange(int index, int count)
    {
        for (var i = index; i < index + count; i++)
            garbage += words[offsets[i]] >> 16;
        offsets.RemoveRange(index, count);
        StructureVersion++;
    }

    /// <summary>
    /// Removes the instructions that match, keeping the order of the others.
    /// </summary>
    /// <param name="match">Called once per instruction, in order, before any instruction after it moves.</param>
    public void RemoveAll(Func<OpDataIndex, bool> match)
    {
        var kept = 0;
        for (var index = 0; index < offsets.Count; index++)
        {
            if (match(new(index, this)))
                garbage += words[offsets[index]] >> 16;
            else
                offsets[kept++] = offsets[index];
        }
        offsets.RemoveRange(kept, offsets.Count - kept);
        StructureVersion++;
    }

    /// <summary>
    /// Copies the instructions of <paramref name="source"/> to <paramref name="index"/>, and leaves <paramref name="source"/> empty.
    /// </summary>
    public void InsertRange(int index, SpirvBuffer source)
    {
        if (index < offsets.Count)
            StructureVersion++;
        var inserted = new int[source.Count];
        for (var i = 0; i < inserted.Length; i++)
            inserted[i] = Append(source.GetWords(i));
        offsets.InsertRange(index, inserted);
        source.Clear();
    }

    /// <summary>
    /// Moves a range of instructions out of this buffer, into a new buffer.
    /// </summary>
    public SpirvBuffer ExtractRange(int index, int count)
    {
        var result = new SpirvBuffer();
        for (var i = index; i < index + count; i++)
            result.Insert(result.Count, GetWords(i));
        RemoveRange(index, count);
        return result;
    }

    public OpData Replace(int index, OpData i)
    {
        if (index < 0 || index >= offsets.Count)
            throw new InvalidOperationException();

        SetWords(index, i.Memory.Span);
        StructureVersion++;
        return GetData(index);
    }

    public OpData Replace<T>(int index, in T instruction) where T : struct, IMemoryInstruction, allows ref struct
    {
        if (index < 0 || index >= offsets.Count)
            throw new InvalidOperationException();

        SetWords(index, instruction.InstructionMemory.Span);
        StructureVersion++;
        return GetData(index);
    }

    public SpirvBuffer FluentReplace<T>(int index, in T instruction, out T result) where T : struct, IMemoryInstruction, allows ref struct
    {
        Replace(index, instruction);
        result = instruction;
        result.Attach(new(index, this));
        return this;
    }

    public Enumerator GetEnumerator() => new(this);

    public struct Enumerator(SpirvBuffer buffer) : IEnumerator<OpDataIndex>
    {
        readonly SpirvBuffer buffer = buffer;
        private int index = -1;

        public readonly OpDataIndex Current => new(index, buffer);

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            if (index < buffer.Count - 1)
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
        var sorted = offsets.Select((offset, index) => (offset, order: InstructionInfo.GetGroupOrder(GetData(index))))
            .OrderBy(x => x.order).Select(x => x.offset).ToList();
        offsets.Clear();
        offsets.AddRange(sorted);
        StructureVersion++;
    }

    public Span<byte> ToBytecode()
    {
        return SpirvBytecode.CreateBytecodeFromBuffers(this);
    }

    /// <summary>
    /// Copies the words of all instructions, in order, to <paramref name="destination"/>.
    /// </summary>
    /// <returns>The number of words written.</returns>
    public int CopyTo(Span<int> destination)
    {
        // One copy when the words are already in order, as after a load or appends only
        if (garbage == 0 && IsInOrder())
        {
            words.AsSpan(0, wordCount).CopyTo(destination);
            return wordCount;
        }
        var written = 0;
        for (var i = 0; i < offsets.Count; i++)
        {
            var instruction = GetWords(i);
            instruction.CopyTo(destination[written..]);
            written += instruction.Length;
        }
        return written;
    }

    /// <summary>
    /// The number of words of all instructions.
    /// </summary>
    public int WordCount => wordCount - garbage;

    private bool IsInOrder()
    {
        var expected = 0;
        foreach (var offset in offsets)
        {
            if (offset != expected)
                return false;
            expected += words[offset] >> 16;
        }
        return expected == wordCount;
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
    /// Writes the words of the instruction at <paramref name="index"/>, which may change its size.
    /// </summary>
    public void SetWords(int index, ReadOnlySpan<int> instruction)
    {
        var offset = offsets[index];
        var size = words[offset] >> 16;
        if (instruction.Length <= size)
        {
            instruction.CopyTo(words.AsSpan(offset));
            garbage += size - instruction.Length;
            return;
        }
        // Counted after Append: a compaction there still copies the old words, as offsets point to them until the new ones are in
        var newOffset = Append(instruction);
        garbage += size;
        offsets[index] = newOffset;
    }

    /// <summary>
    /// Turns the words of an instruction into <see cref="Specification.Op.OpNop"/> words, keeping its size.
    /// </summary>
    public static void SetOpNop(Span<int> words)
    {
        words[0] = words.Length << 16;
        words[1..].Clear();
    }

    public void Dispose() => Clear();

    private void Clear()
    {
        words = [];
        wordCount = 0;
        garbage = 0;
        offsets.Clear();
        StructureVersion++;
        Changed();
    }

    // Copies an instruction after the last word, and returns where it starts
    private int Append(ReadOnlySpan<int> instruction)
    {
        if (garbage > 0 && garbage * 2 > wordCount)
            Compact();
        if (wordCount + instruction.Length > words.Length)
            Array.Resize(ref words, Math.Max(wordCount + instruction.Length, Math.Max(256, words.Length * 2)));
        var offset = wordCount;
        instruction.CopyTo(words.AsSpan(offset));
        wordCount += instruction.Length;
        Changed();
        return offset;
    }

    // Writes the instructions again in order, without the garbage
    private void Compact()
    {
        var compacted = new int[Math.Max(256, (wordCount - garbage) * 2)];
        var written = 0;
        var span = CollectionsMarshal.AsSpan(offsets);
        for (var i = 0; i < span.Length; i++)
        {
            var instruction = words.AsSpan(span[i], words[span[i]] >> 16);
            instruction.CopyTo(compacted.AsSpan(written));
            span[i] = written;
            written += instruction.Length;
        }
        words = compacted;
        wordCount = written;
        garbage = 0;
    }

    private void Changed() => WordsVersion++;

    /// <summary>
    /// Copies the instructions of two buffers into a new buffer.
    /// </summary>
    public static SpirvBuffer Merge(SpirvBuffer buffer1, SpirvBuffer buffer2)
    {
        var result = new SpirvBuffer();
        for (var i = 0; i < buffer1.Count; i++)
            result.Insert(result.Count, buffer1.GetWords(i));
        for (var i = 0; i < buffer2.Count; i++)
            result.Insert(result.Count, buffer2.GetWords(i));
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
        return InstructionInfo.GetInfo(op.InstructionMemory.Span);
    }
}
