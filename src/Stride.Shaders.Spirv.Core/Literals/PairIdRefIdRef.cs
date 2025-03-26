using CommunityToolkit.HighPerformance.Buffers;

namespace Stride.Shaders.Spirv.Core;


public record struct PairIdRefIdRef((int,int) Value) : ISpirvElement, IFromSpirv<PairIdRefIdRef>
{
    public readonly int WordCount => 2;

    public static implicit operator (int,int)(PairIdRefIdRef r) => r.Value;
    public static implicit operator PairIdRefIdRef((int,int) v) => new(v);
    public static implicit operator LiteralInteger(PairIdRefIdRef v) => new((ulong)(v.Value.Item1 << 16 | v.Value.Item2));
    public static PairIdRefIdRef From(Span<int> words) => new() { Value = (words[0], words[1]) };

    public static PairIdRefIdRef From(string value)
    {
        throw new NotImplementedException();
    }

    public readonly SpanOwner<int> AsSpanOwner()
    {
        return new LiteralInteger(Value.Item1 << 32 | Value.Item2).AsSpanOwner();
    }
}

public static class PairIdRefIdRefExtensions
{
    public static SpanOwner<int> AsSpanOwner(this PairIdRefIdRef? value)
    {
        if(value is null)
            return SpanOwner<int>.Empty;
        else 
            return value.Value.AsSpanOwner();
    }
}