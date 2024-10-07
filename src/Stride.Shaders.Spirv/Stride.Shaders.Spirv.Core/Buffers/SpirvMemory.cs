using Stride.Shaders.Spirv.Core.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders.Spirv.Core.Buffers;

/// <summary>
/// A buffer slice
/// </summary>
public ref struct SpirvMemory
{
    ISpirvBuffer buffer;
    Span<int> words => buffer.Span;

    public int Length => words.Length - (HasHeader ? 5 : 0);
    public bool HasHeader => words[0] == Spv.Specification.MagicNumber;

    public Span<int> Span => HasHeader ? words[5..] : words;


    public int this[int index] { get => words[index]; set => words[index] = value; }

    public SpirvMemory(ISpirvBuffer buffer)
    {
        this.buffer = buffer;
    }

    public InstructionEnumerator GetEnumerator() => new(buffer);
}
