using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;


public record struct PrimaryParsers : IParser<Expression>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        if (Parenthesis(ref scanner, result, out parsed))
            return true;
        else if (Method(ref scanner, result, out parsed))
            return true;
        else if (LiteralsParser.Literal(ref scanner, result, out var lit))
        {
            parsed = lit;
            return true;
        }
        else
        {
            if (orError is not null)
                result.Errors.Add(orError.Value);
            return false;
        }
    }
    public static bool Primary<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
            => new PrimaryParsers().Match(ref scanner, result, out parsed, in orError);
    public static bool Identifier<TScanner>(ref TScanner scanner, ParseResult result, out Identifier parsed)
        where TScanner : struct, IScanner
            => new IdentifierParser().Match(ref scanner, result, out parsed);
    public static bool Method<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new MethodCallParser().Match(ref scanner, result, out parsed, in orError);
    public static bool Parenthesis<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new ParenthesisExpressionParser().Match(ref scanner, result, out parsed, in orError);
}


public record struct ParenthesisExpressionParser : IParser<Expression>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Terminals.Char('(', ref scanner, advance: true)
            && CommonParsers.Spaces0(ref scanner, result, out _)
            && ExpressionParser.Expression(ref scanner, result, out parsed, new("Expected expression value", scanner.CreateError(position)))
            && CommonParsers.Spaces0(ref scanner, result, out _)
            && Terminals.Char(')', ref scanner, advance: true)
        )
            return true;
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct MethodCallParser : IParser<Expression>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            LiteralsParser.Identifier(ref scanner, result, out var identifier)
            && CommonParsers.Spaces0(ref scanner, result, out _)
            && Terminals.Char('(', ref scanner, advance: true)
        )
        {
            ParameterParsers.Values(ref scanner, result, out var parameters);
            CommonParsers.Spaces0(ref scanner, result, out _);
            if(Terminals.Char(')', ref scanner, advance: true))
            {
                parsed = new MethodCall(identifier, parameters, scanner.GetLocation(position..scanner.Position));
                return true;
            }
            else return CommonParsers.Exit(ref scanner, result, out parsed, position, new("Expected closing parenthesis", scanner.CreateError(scanner.Position)));
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}