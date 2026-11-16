namespace Stride.Shaders.Spirv.Core.Buffers;

public record struct OpDataIndex(int Index, SpirvBuffer Buffer)
{
    public readonly Specification.Op Op => (Specification.Op)(Words[0] & 0xFFFF);

    /// <summary>
    /// A view of the words of the instruction, valid until words are added to the buffer.
    /// </summary>
    public readonly OpData Data => Buffer.GetData(Index);

    /// <summary>
    /// The words of the instruction. Fetch them again after the buffer changes.
    /// </summary>
    public readonly Span<int> Words => Buffer.GetWords(Index);

    /// <summary>
    /// Turns the instruction into <see cref="Specification.Op.OpNop"/> words, keeping its size.
    /// </summary>
    public readonly void SetOpNop() => SpirvBuffer.SetOpNop(Words);
}
