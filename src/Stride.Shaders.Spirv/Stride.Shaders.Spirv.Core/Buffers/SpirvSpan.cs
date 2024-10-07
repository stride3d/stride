using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders.Spirv.Core.Buffers;

/// <summary>
/// A buffer slice
/// </summary>
public ref struct SpirvSpan
{
    Span<int> words;

    public int Length => words.Length - (HasHeader ? 5 : 0);
    public bool HasHeader => words[0] == Spv.Specification.MagicNumber;

    public Span<int> Span => HasHeader ? words[5..] : words;


    public int this[int index] { get => words[index]; set => words[index] = value; }

    public SpirvSpan(Span<int> words)
    {
        this.words = words;
    }

    public Enumerator GetEnumerator() => new(words);

    public ref struct Enumerator
    {
        int wordIndex;
        Span<int> words;

        public RefInstruction Current => RefInstruction.ParseRef(words.Slice(wordIndex, words[wordIndex] >> 16));

        public Enumerator(Span<int> words)
        {
            wordIndex = -1;
            this.words = words;
        }

        public bool MoveNext()
        {
            if(wordIndex == -1)
            {
                wordIndex = 0;
                return true;
            }
            else
            {
                if (wordIndex + (words[wordIndex] >> 16) >= words.Length)
                    return false;
                wordIndex += words[wordIndex] >> 16;
                return true;
            }
        }
    }
}
