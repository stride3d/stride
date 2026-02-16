namespace Stride.Shaders.Parsing.SDSL.PreProcessing.Macros;

public record struct CodeFrameSnippets(CodeFrame Frame, Range Location)
{
    public readonly Span<char> Span => Frame.Code.Span[Location];
    public readonly Memory<char> Memory => Frame.Code.Memory[Location];

    public readonly int Line 
    {
        get 
        {
            (int offset, int length) = Location.GetOffsetAndLength(Frame.Code.Length);
            return Frame.Code.Span[..(offset + length)].Count('\n');
        }
    }
    public readonly int Column 
    {
        get 
        {
            (int offset, int length) = Location.GetOffsetAndLength(Frame.Code.Length);
            return  Frame.Code.Span[..(offset + length)].Length -  Frame.Code.Span[..(offset + length)].LastIndexOf('\n');
        }
    }
    public static implicit operator CodeFrameSnippets((CodeFrame frame, Range range) tuple) => new(tuple.frame, tuple.range);
}
