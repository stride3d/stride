namespace Stride.Shaders.Parsing;


public interface IScanner<TScannable> : IScanner
    where TScannable : IScannableCode
{
    TScannable Code { get; init; }
}


public interface IScanner
{
    public ReadOnlySpan<char> Span { get; }
    public ReadOnlyMemory<char> Memory { get; }
    public int Position { get; set; }

    public int Line { get; }
    public int Column { get; }



    public int End { get; }
    public bool IsEof { get; }

    public int ReadChar();

    public int Peek();
    public ReadOnlySpan<char> Peek(int size);

    public int Advance(int length);

    public bool ReadString(string matchString, bool caseSensitive);

    public ReadOnlySpan<char> Slice(int index, int length);

    public int LineAtIndex(int index);

    public TextLocation GetLocation(int position, int length);
    public TextLocation GetLocation(Range range);
    public ErrorLocation GetErrorLocation(int position);
}




