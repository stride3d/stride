using System.Reflection.Emit;
using Stride.Shaders.Spirv.Core.Buffers;

namespace Stride.Shaders.Spirv.Core.Parsing;

/// <summary>
/// Instruction enumerator returning RefInstruction
/// </summary>
public ref struct RefMutableFunctionInstructionEnumerator
{
    int wordIndex;
    int index;
    readonly SpirvBuffer buffer;

    public readonly RefInstruction Current => 
        RefInstruction.ParseRef(
            buffer.Span.Slice(wordIndex, buffer.Span[wordIndex] >> 16), 
            wordIndex
        );

    public RefMutableFunctionInstructionEnumerator(SpirvBuffer buffer, int methodStart)
    {
        wordIndex = methodStart;
        index = -1;
        this.buffer = buffer;
    }

    public bool MoveNext()
    {
        if (index == -1)
        {
            index = 0;
            return true;
        }
        else
        {
            if (index >= 0 && buffer.Span[wordIndex] == (int)SDSLOp.OpFunctionEnd)
                return false;
            if (wordIndex + (buffer.Span[wordIndex] >> 16) >= buffer.Span.Length)
                return false;
            wordIndex += buffer.Span[wordIndex] >> 16;
            index += 1;
            return true;
        }
    }
}