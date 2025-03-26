namespace Stride.Shaders.Parsing.SDSL;

public record struct OptionalParser<TParser, TResult>(TParser Parser) : IParser<TResult>
    where TParser : IParser<TResult>
    where TResult : Node
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out TResult parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        Parser.Match(ref scanner, result, out parsed, orError);
        return true;
    }
}