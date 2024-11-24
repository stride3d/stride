using System.Security.AccessControl;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;


public record struct PrimaryParsers : IParser<Expression>
{
    public static bool Primary<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
            => new PrimaryParsers().Match(ref scanner, result, out parsed, in orError);


    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        return CommonParsers.Alternatives(
            ref scanner, result, out parsed, in orError,
            Parenthesis,
            ArrayLiteral,
            Method,
            MixinAccess,
            Literal
        );
    }

    public static bool Literal<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if(LiteralsParser.Literal(ref scanner, result, out var lit))
        {
            parsed = lit;
            return true;
        }
        else return CommonParsers.Exit(ref scanner, result, out parsed, position);
    }
    
    
    public static bool Method<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
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
            if (Terminals.Char(')', ref scanner, advance: true))
            {
                parsed = new MethodCall(identifier, parameters, scanner.GetLocation(position..scanner.Position));
                return true;
            }
            else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0018, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool Parenthesis<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
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
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool ArrayLiteral<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Terminals.Char('{', ref scanner, advance: true)
            && CommonParsers.FollowedByDel(ref scanner, result, ParameterParsers.Values, out ShaderExpressionList values, withSpaces: true, advance: true)
            && CommonParsers.FollowedBy(ref scanner, Terminals.Char('}'), withSpaces: true, advance: true)
        )
        {
            parsed = new ArrayLiteral(scanner.GetLocation(position..scanner.Position))
            {
                Values = values.Values
            };
            return true;
        }
        else return CommonParsers.Exit(ref scanner, result, out parsed, position);
    }
    
    public static bool MixinAccess<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            ShaderClassParsers.Mixin(ref scanner, result, out var mixin)
            && CommonParsers.FollowedBy(ref scanner, Terminals.Char('.'), withSpaces: true)
        )
        {
            parsed = new MixinAccess(mixin, scanner.GetLocation(position..scanner.Position));
            return true;
        }
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}