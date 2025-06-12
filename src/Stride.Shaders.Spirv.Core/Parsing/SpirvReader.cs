using CommunityToolkit.HighPerformance.Buffers;
using System.Runtime.InteropServices;
using Stride.Shaders.Spirv.Core.Buffers;

namespace Stride.Shaders.Spirv.Core.Parsing;

/// <summary>
/// Simple Spirv parser for external buffers
/// </summary>
public ref struct SpirvReader
{
    public static void ParseToList(byte[] byteCode, List<Instruction> instructions)
    {
        
        var span = MemoryMarshal.Cast<byte, int>(byteCode.AsSpan());
        var data = new SpirvBuffer(span);
        foreach (var instruction in data)
            instructions.Add(instruction);
    }




    SpirvSpan buffer;
    public int Count => GetInstructionCount();
    public int WordCount => buffer.Length;
    public bool HasHeader { get; init; }

    public SpirvReader(byte[] byteCode, bool hasHeader = false)
    {
        buffer = new(MemoryMarshal.Cast<byte, int>(byteCode.AsSpan()));
        HasHeader = hasHeader;
    }
    public SpirvReader(MemoryOwner<int> slice, bool hasHeader = false)
    {
        buffer = new(slice.Span);
        HasHeader = hasHeader;
    }
    public SpirvReader(Memory<int> slice, bool hasHeader = false)
    {
        buffer = new(slice.Span[(hasHeader ? 5 : 0)..]);
    }
    public SpirvReader(Memory<int> slice)
    {
        buffer = new(slice.Span);
        //data = slice;
    }
    public SpirvReader(SpirvSpan span)
    {
        buffer = span;
        //data = slice;
    }


    public readonly RefInstructionEnumerator GetEnumerator() => new(buffer.Span, HasHeader);

    public readonly int GetInstructionCount()
    {
        var count = 0;
        var index = 0;
        while(index < buffer.Length) 
        {
            count += 1;
            index += buffer.Span[index] >> 16;
        }
        return count;
    }
}
