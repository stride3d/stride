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
        foreach (var instruction in data.Instructions)
            instructions.Add(instruction);
    }




    SpirvBuffer buffer;
    public int Count => GetInstructionCount();
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
    public SpirvReader(SpirvBuffer span)
    {
        buffer = span;
        //data = slice;
    }


    public readonly int GetInstructionCount() => buffer.Instructions.Count;
}
