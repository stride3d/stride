using CommunityToolkit.HighPerformance.Buffers;

namespace Stride.Shaders.Spirv.Core;


public record struct IdRef(int Value) : ISpirvElement, IFromSpirv<IdRef>
{
    public readonly int WordCount => 1;

    public static implicit operator int(IdRef r) => r.Value;
    public static implicit operator IdRef(int v) => new(v);
    public static implicit operator LiteralInteger(IdRef v) => new(v);
    public static IdRef From(Span<int> words) => new() { Value = words[0] };

    public static IdRef From(string value)
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