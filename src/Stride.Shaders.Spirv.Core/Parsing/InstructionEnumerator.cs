using Stride.Shaders.Spirv.Core.Buffers;


namespace Stride.Shaders.Spirv.Core.Parsing;

/// <summary>
/// A simple SPIR-V instruction enumerator without sorting
/// </summary>
public ref struct InstructionEnumerator(ISpirvBuffer buffer)
{
    int wordIndex = 0;
    int index;
    bool started = false;
    readonly ISpirvBuffer buffer = buffer;

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
            if (wordIndex >= buffer.InstructionSpan.Length)
                return false;
            var sizeToStep = buffer.InstructionSpan[wordIndex] >> 16;
            wordIndex += sizeToStep;
            index += 1;
            if (wordIndex >= buffer.InstructionSpan.Length)
                return false;
            return true;
        }

    }


    public readonly Instruction ParseCurrentInstruction()
    {
        var count = buffer.InstructionSpan[wordIndex] >> 16;
        return new Instruction(buffer, buffer.InstructionMemory[wordIndex..(wordIndex + count)], index, wordIndex);
    }
}
