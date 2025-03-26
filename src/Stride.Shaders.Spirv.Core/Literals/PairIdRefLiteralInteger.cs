using CommunityToolkit.HighPerformance.Buffers;

namespace Stride.Shaders.Spirv.Core;


public record struct PairIdRefLiteralInteger((int,int) Value) : ISpirvElement, IFromSpirv<PairIdRefLiteralInteger>
{
    public readonly int WordCount => 2;

    public static implicit operator (int,int)(PairIdRefLiteralInteger r) => r.Value;
    public static implicit operator PairIdRefLiteralInteger((int,int) v) => new(v);
    public static implicit operator LiteralInteger(PairIdRefLiteralInteger v) => new((ulong)(v.Value.Item1 << 16 | v.Value.Item2));
    public static PairIdRefLiteralInteger From(Span<int> words) => new() { Value = (words[0], words[1]) };

    public static PairIdRefLiteralInteger From(string value)
    {
        throw new NotImplementedException();
    }
    public readonly SpanOwner<int> AsSpanOwner()
    {
        return new LiteralInteger(Value.Item1 << 32 | Value.Item2).AsSpanOwner();
    }
}

public static class PairIdRefLiteralIntegerExtensions
{
    public static SpanOwner<int> AsSpanOwner(this PairIdRefLiteralInteger? value)
    {
        if(value is null)
            return SpanOwner<int>.Empty;
        else 
            return value.Value.AsSpanOwner();
    }
}