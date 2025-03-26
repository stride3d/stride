namespace Stride.Shaders.Spirv.Core.Parsing;

/// <summary>
/// Instruction enumerator returning RefInstruction
/// </summary>
public ref struct RefInstructionEnumerator
{
    int wordIndex;
    int index;
    readonly Span<int> words;
    readonly bool hasHeader;

    public readonly RefInstruction Current => 
        RefInstruction.ParseRef(
            words.Slice(wordIndex, words[wordIndex] >> 16), 
            wordIndex + (hasHeader ? 5 : 0), 
            index
        );

    public RefInstructionEnumerator(Span<int> words, bool hasHeader)
    {
        wordIndex = -1;
        index = 0;
        this.words = words;
        this.hasHeader = hasHeader;
    }

    public bool MoveNext()
    {
        if (wordIndex == -1)
        {
            wordIndex = 0;
            return true;
        }
        else
        {
            if (wordIndex + (words[wordIndex] >> 16) >= words.Length)
                return false;
            wordIndex += words[wordIndex] >> 16;
            index += 1;
            return true;
        }
    }
}