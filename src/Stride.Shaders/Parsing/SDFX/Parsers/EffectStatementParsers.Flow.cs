using Stride.Shaders.Parsing.SDFX.AST;
using Stride.Shaders.Parsing.SDFX.Parsers;
using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDFX;



public record struct FlowParsers : IParser<EffectFlow>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out EffectFlow parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (ForEach(ref scanner, result, out var fe, orError))
        {
            parsed = fe;
            return true;
        }
        else return SDSL.Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool ForEach<TScanner>(ref TScanner scanner, ParseResult result, out EffectForEach parsed, ParseError? orError = null)
        where TScanner : struct, IScanner
        => new EffectForEachParser().Match(ref scanner, result, out parsed, orError);
}




public record struct EffectForEachParser : IParser<EffectForEach>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out EffectForEach parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Tokens.Literal("foreach", ref scanner, advance: true) && SDSL.Parsers.Spaces0(ref scanner, result, out _))
        {
            if (Tokens.Char('(', ref scanner, advance: true) && SDSL.Parsers.Spaces0(ref scanner, result, out _))
            {
                if (
                    LiteralsParser.TypeName(ref scanner, result, out var typeName, new(SDSLErrorMessages.SDSL0017, scanner[scanner.Position], scanner.Memory))
                    && SDSL.Parsers.Spaces1(ref scanner, result, out _)
                    && LiteralsParser.Identifier(ref scanner, result, out var identifier, new(SDSLErrorMessages.SDSL0032, scanner[scanner.Position], scanner.Memory))
                    && SDSL.Parsers.Spaces1(ref scanner, result, out _)
                )
                {
                    if (Tokens.Literal("in", ref scanner, advance: true) && SDSL.Parsers.Spaces1(ref scanner, result, out _))
                    {
                        if (
                            ExpressionParser.Expression(ref scanner, result, out var collection, new(SDSLErrorMessages.SDSL0032, scanner[scanner.Position], scanner.Memory))
                            && SDSL.Parsers.Spaces0(ref scanner, result, out _)
                        )
                        {
                            if (Tokens.Char(')', ref scanner, advance: true) && SDSL.Parsers.Spaces0(ref scanner, result, out _))
                            {
                                if (EffectStatementParsers.Statement(ref scanner, result, out var statement, new(SDSLErrorMessages.SDSL0010, scanner[scanner.Position], scanner.Memory)))
                                {
                                    parsed = new((TypeName)typeName, identifier, collection, statement, scanner[position..scanner.Position]);
                                    return true;
                                }
                            }
                            else return SDSL.Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0018, scanner[scanner.Position], scanner.Memory));
                        }
                    }
                    else return SDSL.Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0001, scanner[scanner.Position], scanner.Memory));
                }
            }
            else return SDSL.Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0035, scanner[scanner.Position], scanner.Memory));
        }
        return SDSL.Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}