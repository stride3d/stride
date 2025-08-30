using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using Stride.Shaders.Spirv.Core.Parsing;
using System;
using System.Buffers;
using System.Numerics;

namespace Stride.Shaders.Spirv.Core.Buffers;

/// <summary>
/// A common SPIR-V buffer containing a header.
/// </summary>
public class SpirvBuffer : IMutSpirvBuffer
{
    private SpirvHeader header = new()
    {
        VersionNumber = new(1, 3),
        MagicNumber = Specification.MagicNumber,
    };
    private ArrayPool<int> pool = ArrayPool<int>.Shared;

    public List<Instruction> Instructions { get; } = [];

    public Span<Instruction> InstructionsSpan => Instructions.AsSpan();

    public bool HasHeader => true;
    public ref SpirvHeader Header => ref header;

    public Instruction FindInstructionByResultId(int resultId)
    {
        foreach (var instruction in Instructions)
        {
            if (instruction.ResultId == resultId)
                return instruction;
        }

        throw new InvalidOperationException();
    }

    public Instruction this[int index] => Instructions[index];

    public SpirvBuffer(int initialSize = 32)
    {
    }
    public SpirvBuffer(Memory<int> memory)
    {
        Header = SpirvHeader.Read(memory.Span);
        var instructions = memory[5..];

        int wid = 0;
        while (wid < instructions.Length)
        {
            Instructions.Add(new Instruction(instructions.Slice(wid, instructions.Span[wid] >> 16)));
            wid += instructions.Span[wid] >> 16;
        }
    }

    public SpirvBuffer(Span<int> span)
    {
        Header = SpirvHeader.Read(span);
        var instructions = span[5..];

        int wid = 0;
        while (wid < instructions.Length)
        {
            Add(instructions.Slice(wid, instructions[wid] >> 16));
            wid += instructions[wid] >> 16;
        }
    }

    public int[] ToBuffer()
    {
        var offset = 5;
        foreach (var instruction in Instructions)
            offset += instruction.WordCount;
        var buffer = new int[offset];

        Header.WriteTo(buffer);
        offset = 5;
        foreach (var instruction in Instructions)
        {
            instruction.Words.CopyTo(buffer.AsSpan()[offset..]);
            offset += instruction.WordCount;
        }

        return buffer;
    }


    public void Sort()
    {
        var sorted = new OrderedEnumerator(this);
        var newInstructions = new List<Instruction>();
        while (sorted.MoveNext())
        {
            newInstructions.Add(sorted.Current);
        }

        Instructions.Clear();
        Instructions.AddRange(newInstructions);
    }

    private Instruction CreateInstruction(Span<int> instructionData)
    {
        var instructionBuffer = pool.Rent(instructionData.Length).AsMemory(0, instructionData.Length);
        instructionData.CopyTo(instructionBuffer.Span);
        return new Instruction(instructionBuffer);
    }

    public Instruction Add(Span<int> instructionData)
    {
        var instruction = CreateInstruction(instructionData);

        Instructions.Add(instruction);
        if (instruction.ResultId is int resultId && resultId >= Header.Bound)
            Header = Header with { Bound = resultId + 1 };

        return instruction;
    }

    public Instruction Insert(int position, Span<int> instructionData)
    {
        var instruction = CreateInstruction(instructionData);

        Instructions.Insert(position, instruction);
        if (instruction.ResultId is int resultId && resultId >= Header.Bound)
            Header = Header with { Bound = resultId + 1 };

        return instruction;
    }

    internal void Add<TBuff>(TBuff buffer)
        where TBuff : ISpirvBuffer
    {
        Instructions.AddRange(buffer.InstructionsSpan);
    }

    public static SpirvBuffer Merge<T1, T2>(T1 left, T2 right)
        where T1 : ISpirvBuffer
        where T2 : ISpirvBuffer
    {
        var buff = new SpirvBuffer();
        buff.Add(left);
        buff.Add(right);
        foreach (var e in buff.Instructions)
            if (e.ResultId is int r && buff.Header.Bound < r + 1)
                buff.Header = buff.Header with { Bound = r + 1 };
        return buff;
    }
}
