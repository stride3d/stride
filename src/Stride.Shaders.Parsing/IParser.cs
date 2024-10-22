using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing;

public interface IParser;

public interface IParser<TResult> : IParser
    where TResult : Node
{
    public bool Match<TScanner>(ref TScanner scanner, ParseResult result, out TResult parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner;
}