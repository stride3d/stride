using CommunityToolkit.HighPerformance.Buffers;

namespace Stride.Shaders.Spirv.Core;




public record struct IdResult : ISpirvElement, IFromSpirv<IdResult>
{
    public readonly int WordCount => 1;

    public readonly int Value => Word.Span[0];
    public MemoryOwner<int> Word { get; set; }
    public readonly ReadOnlySpan<int> Words => Word.Span;
    
    public IdResult(int value)
    {
        Word = MemoryOwner<int>.Allocate(1);
        Word.Span[0] = value;
    }

    public static implicit operator int(IdResult r) => r.Value;
    public static implicit operator IdResult(int v) => new(v);
    public static implicit operator LiteralInteger(IdResult v) => new(v);
    public static implicit operator IdRef(IdResult v) => new(v);
    public static implicit operator IdResultType(IdResult v) => new(v);
    public static IdResult From(Span<int> words) => new(words[0]);

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

    public void Dispose()
    {
        Word.Dispose();
    }
}