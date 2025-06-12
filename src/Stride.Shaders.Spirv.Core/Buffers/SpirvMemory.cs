using Stride.Shaders.Spirv.Core.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders.Spirv.Core.Buffers;

/// <summary>
/// A SPIR-V buffer memory slice
/// </summary>
public readonly struct SpirvMemory(Memory<int> memory) : ISpirvBuffer
{
    public readonly Instruction this[int index]
    {
        get
        {
            int id = 0;
            int wid = 5;
            while (id < index)
            {
                wid += Span[wid] >> 16;
                id++;
            }
            return new Instruction(Memory.Slice(wid, Span[wid] >> 16));
        }
    }
    public readonly Span<int> Span => Memory.Span;

    public readonly Memory<int> Memory { get; } = memory;

    public readonly Span<int> InstructionSpan => Span[(HasHeader ? 5 : 0)..];

    public readonly Memory<int> InstructionMemory => Memory[(HasHeader ? 5 : 0)..];

    public readonly int InstructionCount => new SpirvReader(Memory).Count;

    public readonly int Length => Memory.Length;

    public readonly bool HasHeader => Span[0] == Spv.Specification.MagicNumber;

    public readonly RefHeader Header
    {
        get => HasHeader ? new(Span[..5]) : throw new Exception("No header for this buffer");
        set
        {
            if (HasHeader) value.Words.CopyTo(Header.Words);
        }
    }

    public readonly SpirvMemory AsMemory() => this;

    public readonly SpirvSpan AsSpan() => new(Span);

    public InstructionEnumerator GetEnumerator() => new(InstructionMemory, HasHeader);
}
