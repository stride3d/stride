using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing;

/// <summary>
/// Parser interface
/// </summary>
public interface IParser;

/// <summary>
/// Parser with a Match method to parse a specific node.
/// </summary>
/// <typeparam name="TResult">Output type of the parser</typeparam>
public interface IParser<TResult> : IParser
    where TResult : Node
{
    /// <summary>
    /// Parsing method
    /// </summary>
    /// <param name="scanner">Scanner containing information on the position in the shader text</param>
    /// <param name="result">Result of the parser</param>
    /// <param name="parsed">Element parsed</param>
    /// <param name="orError">The error to use in case of a parse error</param>
    /// <typeparam name="TScanner">Type of the scanner</typeparam>
    /// <returns></returns>
    public bool Match<TScanner>(ref TScanner scanner, ParseResult result, out TResult parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner;
}