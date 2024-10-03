using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDFX.AST;

namespace Stride.Shaders.Parsing.SDFX.Parsers;


public record struct ParamsParsers : IParser<EffectParameters>
{
    public bool Match<TScanner>(ref TScanner scanner, ParseResult result, out EffectParameters parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Terminals.Literal("params", ref scanner, advance: true) && CommonParsers.Spaces1(ref scanner, result, out _))
        {
            if(LiteralsParser.TypeName(ref scanner, result, out var paramsName))
            {
                CommonParsers.Spaces0(ref scanner, result, out _);
                if(Terminals.Char('{', ref scanner, advance: true))
                {
                    CommonParsers.Spaces0(ref scanner, result, out _);
                    // while()
                }
            }
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct ParameterParser : IParser<EffectParameter>
{
    public bool Match<TScanner>(ref TScanner scanner, ParseResult result, out EffectParameter parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;

        if(LiteralsParser.TypeName(ref scanner, result, out var typename) && CommonParsers.Spaces1(ref scanner, result, out _))
        {
            if(LiteralsParser.Identifier(ref scanner, result, out var identifier))
            {

            }
        }

        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);

    }
}