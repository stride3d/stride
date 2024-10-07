namespace Stride.Shaders.Spirv.Core.Buffers;

/// <summary>
/// A spirv buffer object
/// </summary>
public interface ISpirvBuffer
{
    Span<int> Span { get; }
    Memory<int> Memory { get; }
    Span<int> InstructionSpan { get; }
    Memory<int> InstructionMemory { get; }

    bool HasHeader { get; }

    public Instruction this[int index] {get;}

    public SpirvSpan AsSpan();
    public SpirvMemory AsMemory();
}