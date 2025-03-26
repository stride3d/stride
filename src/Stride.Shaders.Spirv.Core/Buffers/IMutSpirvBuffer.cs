using Stride.Shaders.Spirv.Core.Buffers;

namespace Stride.Shaders.Spirv.Core;

/// <summary>
/// SPIR-V buffer that can add instructions to itself
/// </summary> 
public interface IMutSpirvBuffer : ISpirvBuffer
{
    public Instruction Add(Span<int> instruction);
}

public static class IMutSpirvBufferExtensions
{
    internal static int GetWordLength<TBuffer, TValue>(this TBuffer _, Span<TValue> values)
        where TBuffer : IMutSpirvBuffer
        where TValue : ISpirvElement
    {
        int length = 0;
        foreach (var value in values)
            length += value.WordCount;
        return length;
    }
    internal static int GetWordLength<TBuffer, TValue>(this TBuffer _, TValue? value)
        where TBuffer : IMutSpirvBuffer
    {
        if (value is null) return 0;

        return value switch
        {
            LiteralInteger i => i.WordCount,
            LiteralFloat i => i.WordCount,
            int _ => 1,
            IdRef _ => 1,
            IdResultType _ => 1,
            IdResult _ => 1,
            string v => new LiteralString(v).WordCount,
            LiteralString v => v.WordCount,
            int[] a => a.Length,
            Enum _ => 1,
            _ => throw new NotImplementedException()
        };
    }
}