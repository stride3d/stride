using CommunityToolkit.HighPerformance.Buffers;

namespace Stride.Shaders.Parsing;

public record struct TextLocation(ReadOnlyMemory<char> Original, Range Range)
{
    public ReadOnlyMemory<char> Text { get; } = Original[Range];
    public readonly ReadOnlySpan<char> TextSpan => Text.Span;
    public readonly int Length => Range.GetOffsetAndLength(Original.Length).Length;

    public readonly int Line => Original.Span[..Range.StartsAt(Original.Length)].Count('\n') + 1;
    public readonly int Column => Range.StartsAt(Original.Length) - Original.Span[..Range.StartsAt(Original.Length)].LastIndexOf('\n');

    public readonly int EndLine => Original.Span[..Range.EndsAt(Original.Length)].Count('\n') + 1;
    public readonly int EndColumn => Range.EndsAt(Original.Length) - Original.Span[..Range.EndsAt(Original.Length)].LastIndexOf('\n');
    public readonly override string ToString()
    {
        return $"[l{Line}-c{Column}]\n{Text.Span}";
    }
}

public static class SpanCharExtensions
{
    public static int Sum(this (int offset, int length) ol) => ol.offset + ol.length;

    public static int EndsAt(this Range range, int originalLength)
    {
        var (o, l) =range.GetOffsetAndLength(originalLength);
        return o + l;
    }
    public static int StartsAt(this Range range, int originalLength)
    {
        var (o, _) =range.GetOffsetAndLength(originalLength);
        return o;
    }
}