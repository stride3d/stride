using CommunityToolkit.HighPerformance.Buffers;

namespace Stride.Shaders.Spirv.Core;


public record struct IdResultType(int Value) : ISpirvElement, IFromSpirv<IdResultType>
{
    public readonly int WordCount => 1;

    public static implicit operator int(IdResultType r) => r.Value;
    public static implicit operator IdResultType(int v) => new(v);
    public static implicit operator LiteralInteger(IdResultType v) => new(v);
    public static IdResultType From(Span<int> words) => new() { Value = words[0] };

    public static IdResultType From(string value)
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

public static class IdResultTypeExtensions
{
    public static SpanOwner<int> AsSpanOwner(this IdResultType? value)
    {
        if(value is null)
            return SpanOwner<int>.Empty;
        else 
            return value.Value.AsSpanOwner();
    }
}