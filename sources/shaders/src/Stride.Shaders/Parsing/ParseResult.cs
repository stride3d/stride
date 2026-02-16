using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing;

/// <summary>
/// Represents a parsing error
/// </summary>
public record struct ParseError(string Message, ErrorLocation Location, ReadOnlyMemory<char> Code)
{

    readonly ReadOnlySpan<char> GetNextToken()
    {
        var operators = "'+-*/%=!<>&|^~?:".AsSpan();
        var pos = Location.Position;
        if (pos >= Code.Span.Length)
            return [];
        if(operators.Contains(Code.Span[pos]))
        {
            while(operators.Contains(Code.Span[pos]))
                pos++;
            return Code.Span[Location.Position..pos];
        }
        else if(char.IsDigit(Code.Span[pos]))
        {
            while(char.IsDigit(Code.Span[pos]))
                pos++;
            return Code.Span[Location.Position..pos];
        }
        else if(char.IsLetter(Code.Span[pos]) || Code.Span[pos] == '_' )
        {
            while(char.IsLetterOrDigit(Code.Span[pos]) || Code.Span[pos] == '_')
                pos++;
            return Code.Span[Location.Position..pos];
        }
        else return Code.Span[Location.Position..(Location.Position+1)];
    }
    public override readonly string ToString()
    {
        return $"{Location} {Message} : {GetNextToken()}";
    }
}

/// <summary>
/// Represents the result of the parser
/// </summary>
/// <typeparam name="T"></typeparam>
public class ParseResult<T>
    where T : Node
{
    public T? AST { get; set; }
    public List<ParseError> Errors { get; internal set; } = [];
}

/// <summary>
/// Default parser result
/// </summary>
public class ParseResult : ParseResult<Node>;