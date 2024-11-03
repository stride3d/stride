using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;


public record struct DirectivePrimaryParsers : IParser<Expression>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        if (Parenthesis(ref scanner, result, out parsed))
            return true;
        else if (LiteralsParser.Identifier(ref scanner, result, out var lit))
        {
            parsed = lit;
            return true;
        }
        else if (LiteralsParser.Integer(ref scanner, result, out var integer))
        {
            parsed = integer;
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
            => new DirectivePrimaryParsers().Match(ref scanner, result, out parsed, in orError);
    public static bool Identifier<TScanner>(ref TScanner scanner, ParseResult result, out Identifier parsed)
        where TScanner : struct, IScanner
            => new IdentifierParser().Match(ref scanner, result, out parsed);
    public static bool Parenthesis<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new DirectiveParenthesisExpressionParser().Match(ref scanner, result, out parsed, in orError);
}


public record struct DirectiveParenthesisExpressionParser : IParser<Expression>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Terminals.Char('(', ref scanner, advance: true)
            && CommonParsers.Spaces0(ref scanner, result, out _)
            && ExpressionParser.Expression(ref scanner, result, out parsed, new(SDSLParsingMessages.SDSL0015, scanner.GetErrorLocation(position), scanner.Memory))
            && CommonParsers.Spaces0(ref scanner, result, out _)
            && Terminals.Char(')', ref scanner, advance: true)
        )
            return true;
        else
        {
            if (orError != null)
                result.Errors.Add(orError.Value);
            parsed = null!;
            scanner.Position = position;
            return false;
        }
    }
}

public record struct DirectiveMethodCallParser : IParser<Expression>
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
            CommonParsers.Spaces0(ref scanner, result, out _);
            ParameterParsers.Values(ref scanner, result, out var parameters);
            var pos2 = scanner.Position;
            if (Terminals.Char(')', ref scanner, advance: true))
            {
                parsed = new MethodCall(identifier, parameters, scanner.GetLocation(position, scanner.Position - position));
                return true;
            }
            else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0018, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
        }
        
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}