using CommunityToolkit.HighPerformance.Buffers;

namespace Stride.Shaders.Spirv.Core;


public record struct IdScope(int Value) : ISpirvElement, IFromSpirv<IdScope>
{
    public readonly int WordCount => 1;

    public static implicit operator int(IdScope r) => r.Value;
    public static implicit operator IdScope(int v) => new(v);
    public static implicit operator LiteralInteger(IdScope v) => new(v);
    public static IdScope From(Span<int> words) => new() { Value = words[0] };

    public static IdScope From(string value)
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

public static class IdScopeExtensions
{
    public static SpanOwner<int> AsSpanOwner(this IdScope? value)
    {
        if(value is null)
            return SpanOwner<int>.Empty;
        else 
            return new LiteralInteger(value.Value.Value).AsSpanOwner();
    }
}