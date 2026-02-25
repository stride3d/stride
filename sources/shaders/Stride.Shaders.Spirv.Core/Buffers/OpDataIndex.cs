namespace Stride.Shaders.Spirv.Core.Buffers;

public record struct OpDataIndex(int Index, SpirvBuffer Buffer)
{
    public readonly Specification.Op Op => Data.Op;
    public readonly ref OpData Data => ref Buffer.GetRef(Index);
}
