namespace Stride.Shaders.Parsing;

public interface IScannableCode
{
    ReadOnlySpan<char> Span { get; }
    ReadOnlyMemory<char> Memory { get; }
}




