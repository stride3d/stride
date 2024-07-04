using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;


public record struct UnaryParsers
{
    internal static bool Not<TScanner>(ref TScanner scanner, ParseResult result, out Expression cast, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new NotExpressionParser().Match(ref scanner, result, out cast, in orError);
    internal static bool Signed<TScanner>(ref TScanner scanner, ParseResult result, out Expression cast, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new SignExpressionParser().Match(ref scanner, result, out cast, in orError);
    internal static bool PrefixIncrement<TScanner>(ref TScanner scanner, ParseResult result, out Expression cast, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new PrefixIncrementParser().Match(ref scanner, result, out cast, in orError);
    internal static bool Cast<TScanner>(ref TScanner scanner, ParseResult result, out Expression cast, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new CastExpressionParser().Match(ref scanner, result, out cast, in orError);
    public static bool Prefix<TScanner>(ref TScanner scanner, ParseResult result, out Expression prefix, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new PrefixParser().Match(ref scanner, result, out prefix, in orError);
    public static bool Postfix<TScanner>(ref TScanner scanner, ParseResult result, out Expression postfix, in ParseError? orError = null)
        where TScanner : struct, IScanner
       => new PostfixParser().Match(ref scanner, result, out postfix, in orError);
}
