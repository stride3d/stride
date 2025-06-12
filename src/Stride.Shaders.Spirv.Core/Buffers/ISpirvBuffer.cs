using Stride.Shaders.Spirv.Core.Parsing;

namespace Stride.Shaders.Spirv.Core.Buffers;

/// <summary>
/// A SPIR-V buffer object
/// </summary>
public interface ISpirvBuffer
{
    /// <summary>
    /// Span of the buffer
    /// </summary>
    Span<int> Span { get; }
    /// <summary>
    /// Memory of the buffer
    /// </summary>
    Memory<int> Memory { get; }
    /// <summary>
    /// Span of the buffer without the header
    /// </summary>
    Span<int> InstructionSpan { get; }
    /// <summary>
    /// Memory of the buffer without the header
    /// </summary>
    Memory<int> InstructionMemory { get; }
    /// <summary>
    /// Count of instructions 
    /// </summary>
    public int InstructionCount { get; }
    /// <summary>
    /// Length of the buffer
    /// </summary>
    int Length { get; }

    /// <summary>
    /// Wether the buffer has a header 
    /// </summary>
    bool HasHeader { get; }
    /// <summary>
    /// Header of the buffer
    /// </summary>
    RefHeader Header { get; set; }

    /// <summary>
    /// Get instruction from the instruction index
    /// </summary>
    public Instruction this[int index] { get; }

    /// <summary>
    /// Convert to a SpirvSpan
    /// </summary>
    /// <returns>Buffer as a Span</returns>
    public SpirvSpan AsSpan();
    /// <summary>
    /// Convert to a SpirvMemory
    /// </summary>
    /// <returns>Buffer as a memory</returns>
    public SpirvMemory AsMemory();

    /// <summary>
    /// Gets Instruction enumerator
    /// </summary>
    /// <returns>Instruction enumerator</returns>
    public InstructionEnumerator GetEnumerator();
}