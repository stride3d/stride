namespace Stride.Shaders.Spirv.Core.Buffers;

public record struct OpDataIndex(int Index, SpirvBuffer Buffer)
{
    public readonly Specification.Op Op => Data.Op;
    public readonly ref OpData Data => ref Buffer.GetRef(Index);

    /// <summary>
    /// The words of the instruction. Fetch them again after the buffer changes.
    /// </summary>
    public readonly Span<int> Words => Data.Memory.Span;

    /// <summary>
    /// Turns the instruction into <see cref="Specification.Op.OpNop"/> words, keeping its size.
    /// </summary>
    public readonly void SetOpNop() => SpirvBuffer.SetOpNop(Words);
}
