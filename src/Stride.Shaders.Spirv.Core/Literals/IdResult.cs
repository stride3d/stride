using CommunityToolkit.HighPerformance.Buffers;

namespace Stride.Shaders.Spirv.Core;


public record struct IdResult(int Value) : ISpirvElement, IFromSpirv<IdResult>
{
    public readonly int WordCount => 1;

    public static implicit operator int(IdResult r) => r.Value;
    public static implicit operator IdResult(int v) => new(v);
    public static implicit operator LiteralInteger(IdResult v) => new(v);
    public static IdResult From(Span<int> words) => new() { Value = words[0] };

    public static IdResult From(string value)
    {
        throw new NotImplementedException();
    }
    public readonly SpanOwner<int> AsSpanOwner()
    {
        var owner = SpanOwner<int>.Allocate(1);
        owner.Span[0] = Value;
        return owner;
    }
}

public static class IdResultExtensions
{
    public static SpanOwner<int> AsSpanOwner(this IdResult? value)
    {
        if(value is null)
            return SpanOwner<int>.Empty;
        else 
            return value.Value.AsSpanOwner();
    }
}