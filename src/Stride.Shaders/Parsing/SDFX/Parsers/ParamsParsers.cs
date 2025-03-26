using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDFX.AST;

namespace Stride.Shaders.Parsing.SDFX.Parsers;


public record struct ParamsParsers : IParser<EffectParameters>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out EffectParameters parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Tokens.Literal("params", ref scanner, advance: true) && SDSL.Parsers.Spaces1(ref scanner, result, out _))
        {
            if (LiteralsParser.TypeName(ref scanner, result, out var paramsName))
            {
                parsed = new(paramsName, new());
                SDSL.Parsers.Spaces0(ref scanner, result, out _);
                if (Tokens.Char('{', ref scanner, advance: true))
                {
                    SDSL.Parsers.Spaces0(ref scanner, result, out _);
                    while (!scanner.IsEof)
                    {
                        if (Parameter(ref scanner, result, out var p))
                            parsed.Parameters.Add(p);
                        else if (SDSL.Parsers.FollowedBy(ref scanner, Tokens.Char('}'), withSpaces: true, advance: true))
                        {
                            SDSL.Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true, advance: true);
                            parsed.Info = scanner[position..scanner.Position];
                            return true;
                        }
                        else
                            SDSL.Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0012, scanner[scanner.Position], scanner.Memory));
                        SDSL.Parsers.Spaces0(ref scanner, result, out _);
                    }
                }
            }
        }
        return SDSL.Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
    public static bool Params<TScanner>(ref TScanner scanner, ParseResult result, out EffectParameters parsed, in ParseError? orError = null) where TScanner : struct, IScanner
            => new ParamsParsers().Match(ref scanner, result, out parsed, orError);
    public static bool Parameter<TScanner>(ref TScanner scanner, ParseResult result, out EffectParameter parsed, in ParseError? orError = null) where TScanner : struct, IScanner
            => new ParameterParser().Match(ref scanner, result, out parsed, orError);
}

public record struct ParameterParser : IParser<EffectParameter>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out EffectParameter parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;

        if (LiteralsParser.TypeName(ref scanner, result, out var typename) && SDSL.Parsers.Spaces1(ref scanner, result, out _))
        {
            if (LiteralsParser.Identifier(ref scanner, result, out var identifier))
            {
                if (SDSL.Parsers.FollowedBy(ref scanner, Tokens.Char('='), withSpaces: true, advance: true))
                {
                    SDSL.Parsers.Spaces0(ref scanner, result, out _);
                    if (ExpressionParser.Expression(ref scanner, result, out var expression) && SDSL.Parsers.Spaces0(ref scanner, result, out _))
                    {
                        if (!Tokens.Char(';', ref scanner, advance: true))
                            return SDSL.Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0013, scanner[scanner.Position], scanner.Memory));
                        parsed = new(typename, identifier, scanner[position..scanner.Position], expression);
                        return true;
                    }
                }
                else if (SDSL.Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true, advance: true))
                {
                    parsed = new(typename, identifier, scanner[position..scanner.Position]);
                    return true;
                }
                else return SDSL.Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0014, scanner[scanner.Position], scanner.Memory));
            }
        }
        return SDSL.Parsers.Exit(ref scanner, result, out parsed, position, orError);

    }
}