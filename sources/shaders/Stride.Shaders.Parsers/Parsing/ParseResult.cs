using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing;

/// <summary>
/// Represents a parsing error
/// </summary>
public record struct ParseError(string Message, ErrorLocation Location, ReadOnlyMemory<char> Code)
{
    /// <summary>
    /// The error is about the token at <see cref="Location"/>, even when another parser reached further.
    /// </summary>
    public bool IsAboutToken { get; init; }

    readonly ReadOnlySpan<char> GetNextToken()
    {
        var operators = "'+-*/%=!<>&|^~?:".AsSpan();
        var pos = Location.Position;
        if (pos >= Code.Span.Length)
            return [];
        if (operators.Contains(Code.Span[pos]))
        {
            while (pos < Code.Span.Length && operators.Contains(Code.Span[pos]))
                pos++;
            return Code.Span[Location.Position..pos];
        }
        else if (char.IsDigit(Code.Span[pos]))
        {
            while (pos < Code.Span.Length && char.IsDigit(Code.Span[pos]))
                pos++;
            return Code.Span[Location.Position..pos];
        }
        else if (char.IsLetter(Code.Span[pos]) || Code.Span[pos] == '_')
        {
            while (pos < Code.Span.Length && (char.IsLetterOrDigit(Code.Span[pos]) || Code.Span[pos] == '_'))
                pos++;
            return Code.Span[Location.Position..pos];
        }
        else return Code.Span[Location.Position..(Location.Position + 1)];
    }
    public override readonly string ToString()
    {
        return $"{Location} {Message} : {GetNextToken()}";
    }

    /// <summary>
    /// file(line,col): the form build tools and editors link to, as for semantic errors.
    /// </summary>
    public readonly string ToString(string? filename)
    {
        if (filename == null)
            return ToString();

        var token = GetNextToken().Trim();
        var context = token.Length > 0 ? $" (at `{token}`)" : "";
        return $"{MessageFileName.Of(filename)}({Location.Line},{Location.Column}): {Message}{context}";
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

    /// <summary>
    /// The furthest position any parser reached before it gave up: where the code stops making sense.
    /// </summary>
    internal int FurthestFailure { get; set; }
}

/// <summary>
/// Default parser result
/// </summary>
public class ParseResult : ParseResult<Node>;
