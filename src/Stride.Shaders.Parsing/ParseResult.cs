using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing;


public record struct ParseError(string Message, ErrorLocation Location)
{
    public override readonly string ToString()
    {
        return $"{Message} at : {Location}";
    }
}


public class ParseResult<T>
    where T : Node
{
    public T? AST { get; set; }
    public List<ParseError> Errors { get; internal set; } = [];
}
public class ParseResult : ParseResult<Node>;