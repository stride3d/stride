using CommunityToolkit.HighPerformance.Buffers;

namespace Stride.Shaders.Spirv.Core;


public record struct PairLiteralIntegerIdRef((int, int) Value) : ISpirvElement, IFromSpirv<PairLiteralIntegerIdRef>
{
    public readonly int WordCount => 2;

    public static implicit operator (int, int)(PairLiteralIntegerIdRef r) => r.Value;
    public static implicit operator PairLiteralIntegerIdRef((int, int) v) => new(v);
    public static implicit operator LiteralInteger(PairLiteralIntegerIdRef v) => new((ulong)(v.Value.Item1 << 16 | v.Value.Item2));
    public static PairLiteralIntegerIdRef From(Span<int> words) => new() { Value = (words[0], words[1]) };

    public static PairLiteralIntegerIdRef From(string value)
    {
        throw new NotImplementedException();
    }

    public readonly SpanOwner<int> AsSpanOwner()
    {
        return new LiteralInteger(Value.Item1 << 32 | Value.Item2).AsSpanOwner();
    }
}
public static class PairLiteralIntegerIdRefExtensions
{
    public static SpanOwner<int> AsSpanOwner(this PairLiteralIntegerIdRef? value)
    {
        if (value is null)
            return SpanOwner<int>.Empty;
        else
            return value.Value.AsSpanOwner();
    }
}