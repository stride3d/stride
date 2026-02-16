using CommunityToolkit.HighPerformance.Buffers;

namespace Stride.Shaders.Spirv.Core;




public record struct IdScope : ISpirvElement, IFromSpirv<IdScope>
{
    public readonly int WordCount => 1;

    public readonly int Value => Word.Span[0];
    public MemoryOwner<int> Word { get; set; }
    public readonly ReadOnlySpan<int> Words => Word.Span;
    
    public IdScope(int value)
    {
        Word = MemoryOwner<int>.Allocate(1);
        Word.Span[0] = value;
    }

    public static implicit operator int(IdScope r) => r.Value;
    public static implicit operator IdScope(int v) => new(v);
    public static implicit operator LiteralInteger(IdScope v) => new(v);
    public static implicit operator IdResult(IdScope v) => new(v);
    public static implicit operator IdResultType(IdScope v) => new(v);
    public static IdScope From(Span<int> words) => new(words[0]);

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

    public void Dispose()
    {
        Word.Dispose();
    }
}