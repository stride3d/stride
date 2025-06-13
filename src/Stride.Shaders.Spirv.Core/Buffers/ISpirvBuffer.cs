using Stride.Shaders.Spirv.Core.Parsing;

namespace Stride.Shaders.Spirv.Core.Buffers;

/// <summary>
/// A SPIR-V buffer object
/// </summary>
public interface ISpirvBuffer
{
    public bool HasHeader { get; }
    public ref SpirvHeader Header { get; }

    public Span<Instruction> InstructionsSpan { get; }

    /// <summary>
    /// Get instruction from the instruction index
    /// </summary>
    public Instruction this[int index] { get; }
}