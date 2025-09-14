using CommunityToolkit.HighPerformance.Buffers;
using System.Runtime.InteropServices;
using Stride.Shaders.Spirv.Core.Buffers;

namespace Stride.Shaders.Spirv.Core.Parsing;

/// <summary>
/// Simple Spirv parser for external buffers
/// </summary>
public readonly ref struct SpirvReader(Span<int> words)
{
    public Span<int> Words { get; init; } = words;
    public SpirvReader(Span<byte> bytes) : this(MemoryMarshal.Cast<byte, int>(bytes)) { }

    public readonly Enumerator GetEnumerator() => new(Words);

    public ref struct Enumerator(Span<int> words)
    {
        int wid = 0;
        readonly Span<int> words = words;

        public readonly Span<int> Current => words[wid..(wid + (words[wid] >> 16))];

        public bool MoveNext()
        {
            if(wid == 0 && words[0] == Specification.MagicNumber)
            {
                wid = 5;
                return true;
            }
            else if (wid >= words.Length)
                return false;
            else
            {
                wid += words[wid] >> 16;
                if (wid >= words.Length)
                    return false;
                return true;
            }
        }
    }
}
