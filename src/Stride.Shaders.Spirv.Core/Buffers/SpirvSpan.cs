using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Shaders.Spirv.Core.Parsing;

namespace Stride.Shaders.Spirv.Core.Buffers;

/// <summary>
/// A SPIR-V buffer span slice
/// </summary>
public readonly ref struct SpirvSpan(Span<int> words) : ISpirvBuffer
{
    public readonly Instruction this[int index] => throw new NotImplementedException();

    public readonly Span<int> Span { get; } = words;

    public readonly Memory<int> Memory => throw new Exception("Can't get Memory from Span");

    public readonly Span<int> InstructionSpan => Span[(HasHeader ? 5 : 0)..];

    public readonly Memory<int> InstructionMemory => throw new Exception("Can't get Memory from Span");

    public readonly int InstructionCount => new SpirvReader(this).Count;

    public readonly int Length => Span.Length;

    public readonly bool HasHeader => Span[0] == Spv.Specification.MagicNumber;

    public readonly RefHeader Header
    {
        get => HasHeader ? new(Span[..5]) : throw new Exception("No header for this buffer");
        set
        {
            if (HasHeader) value.Words.CopyTo(Header.Words);
        }
    }

    public readonly SpirvMemory AsMemory() => throw new Exception("Can't get Memory from Span");

    public readonly SpirvSpan AsSpan() => this;

    public InstructionEnumerator GetEnumerator() => new(InstructionMemory, HasHeader);
}
