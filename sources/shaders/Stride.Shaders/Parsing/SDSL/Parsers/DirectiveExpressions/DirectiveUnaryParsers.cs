using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;


public record struct DirectiveUnaryParsers
{
    internal static bool Not<TScanner>(ref TScanner scanner, ParseResult result, out Expression cast, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new DirectiveNotExpressionParser().Match(ref scanner, result, out cast, in orError);
    internal static bool Signed<TScanner>(ref TScanner scanner, ParseResult result, out Expression cast, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new DirectiveSignExpressionParser().Match(ref scanner, result, out cast, in orError);
    internal static bool PrefixIncrement<TScanner>(ref TScanner scanner, ParseResult result, out Expression cast, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new DirectivePrefixIncrementParser().Match(ref scanner, result, out cast, in orError);
    internal static bool Cast<TScanner>(ref TScanner scanner, ParseResult result, out Expression cast, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new DirectiveCastExpressionParser().Match(ref scanner, result, out cast, in orError);
    public static bool Prefix<TScanner>(ref TScanner scanner, ParseResult result, out Expression prefix, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new DirectivePrefixParser().Match(ref scanner, result, out prefix, in orError);
    public static bool Primary<TScanner>(ref TScanner scanner, ParseResult result, out Expression postfix, in ParseError? orError = null)
        where TScanner : struct, IScanner
       => new DirectivePrimaryParsers().Match(ref scanner, result, out postfix, in orError);
}
