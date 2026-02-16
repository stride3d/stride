using CommunityToolkit.HighPerformance.Buffers;

namespace Stride.Shaders.Spirv.Core;


public record struct IdRef : ISpirvElement, IFromSpirv<IdRef>
{
    public readonly int WordCount => 1;

    public readonly int Value => Word.Span[0];
    public MemoryOwner<int> Word { get; set; }
    public readonly ReadOnlySpan<int> Words => Word.Span;
    
    public IdRef(int value)
    {
        Word = MemoryOwner<int>.Allocate(1);
        Word.Span[0] = value;
    }

    public static implicit operator int(IdRef r) => r.Value;
    public static implicit operator IdRef(int v) => new(v);
    public static implicit operator LiteralInteger(IdRef v) => new(v);
    public static implicit operator IdResult(IdRef v) => new(v);
    public static implicit operator IdResultType(IdRef v) => new(v);
    public static IdRef From(Span<int> words) => new(words[0]);

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

    public void Dispose()
    {
        Word.Dispose();
    }
}