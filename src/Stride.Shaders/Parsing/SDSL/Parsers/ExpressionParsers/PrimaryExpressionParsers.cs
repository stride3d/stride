using System.Security.AccessControl;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv;

namespace Stride.Shaders.Parsing.SDSL;


public record struct PrimaryParsers : IParser<Expression>
{
    public static bool Primary<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
            => new PrimaryParsers().Match(ref scanner, result, out parsed, in orError);


    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        return Parsers.Alternatives(
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
        else return Parsers.Exit(ref scanner, result, out parsed, position);
    }
    
    
    public static bool Method<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            LiteralsParser.Identifier(ref scanner, result, out var identifier)
            && Parsers.Spaces0(ref scanner, result, out _)
            && Tokens.Char('(', ref scanner, advance: true)
        )
        {
            ParameterParsers.Values(ref scanner, result, out var parameters);
            Parsers.Spaces0(ref scanner, result, out _);
            if (Tokens.Char(')', ref scanner, advance: true))
            {
                if (IntrinsicsDefinitions.Intrinsics.TryGetValue(identifier.Name, out var intrinsicDefinitions))
                {
                    parsed = new IntrinsicCall(identifier, parameters, scanner[position..scanner.Position]);
                }
                else
                {
                    parsed = new MethodCall(identifier, parameters, scanner[position..scanner.Position]);
                }
                return true;
            }
            else return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0018, scanner[scanner.Position], scanner.Memory));
        }
        return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool Parenthesis<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Tokens.Char('(', ref scanner, advance: true)
            && Parsers.Spaces0(ref scanner, result, out _)
            && ExpressionParser.Expression(ref scanner, result, out parsed, new(SDSLErrorMessages.SDSL0015, scanner[position], scanner.Memory))
            && Parsers.Spaces0(ref scanner, result, out _)
            && Tokens.Char(')', ref scanner, advance: true)
        )
            return true;
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool ArrayLiteral<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Tokens.Char('{', ref scanner, advance: true)
            && Parsers.FollowedByDel(ref scanner, result, ParameterParsers.Values, out ShaderExpressionList values, withSpaces: true, advance: true)
            && Parsers.FollowedBy(ref scanner, Tokens.Char('}'), withSpaces: true, advance: true)
        )
        {
            parsed = new ArrayLiteral(scanner[position..scanner.Position])
            {
                Values = values.Values
            };
            return true;
        }
        else return Parsers.Exit(ref scanner, result, out parsed, position);
    }
    
    public static bool MixinAccess<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            ShaderClassParsers.Mixin(ref scanner, result, out var mixin)
            && Parsers.FollowedBy(ref scanner, Tokens.Char('.'), withSpaces: true)
        )
        {
            parsed = new MixinAccess(mixin, scanner[position..scanner.Position]);
            return true;
        }
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}