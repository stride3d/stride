using Stride.Shaders.Spirv.Core.Buffers;


namespace Stride.Shaders.Spirv.Core.Parsing;

/// <summary>
/// A simple SPIR-V instruction enumerator without sorting
/// </summary>
public ref struct InstructionEnumerator(Memory<int> InstructionMemory, bool HasHeader)
{
    int wordIndex = 0;
    int index;
    bool started = false;

    public int ResultIdReplacement { get; set; } = 0;

    public Instruction Current => ParseCurrentInstruction();

    public bool MoveNext()
    {
        if (!started)
        {
            started = true;
            return true;
        }
        else
        {
            if (wordIndex >= InstructionMemory.Span.Length)
                return false;
            var sizeToStep = InstructionMemory.Span[wordIndex] >> 16;
            wordIndex += sizeToStep;
            index += 1;
            if (wordIndex >= InstructionMemory.Span.Length)
                return false;
            return true;
        }

    }


    public readonly Instruction ParseCurrentInstruction()
    {
        var count = InstructionMemory.Span[wordIndex] >> 16;
        return new Instruction(InstructionMemory[wordIndex..(wordIndex + count)]);
    }
}
