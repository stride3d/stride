using Stride.Shaders.Parsing.SDFX.AST;
using Stride.Shaders.Parsing.SDFX.Parsers;
using Stride.Shaders.Parsing.SDSL;

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
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
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
        if (Terminals.Literal("foreach", ref scanner, advance: true) && CommonParsers.Spaces0(ref scanner, result, out _))
        {
            if (Terminals.Char('(', ref scanner, advance: true) && CommonParsers.Spaces0(ref scanner, result, out _))
            {
                if (
                    LiteralsParser.TypeName(ref scanner, result, out var typeName, new(SDSLParsingMessages.SDSL0017, scanner.GetErrorLocation(scanner.Position), scanner.Memory))
                    && CommonParsers.Spaces1(ref scanner, result, out _)
                    && LiteralsParser.Identifier(ref scanner, result, out var identifier, new(SDSLParsingMessages.SDSL0032, scanner.GetErrorLocation(scanner.Position), scanner.Memory))
                    && CommonParsers.Spaces1(ref scanner, result, out _)
                )
                {
                    if (Terminals.Literal("in", ref scanner, advance: true) && CommonParsers.Spaces1(ref scanner, result, out _))
                    {
                        if (
                            ExpressionParser.Expression(ref scanner, result, out var collection, new(SDSLParsingMessages.SDSL0032, scanner.GetErrorLocation(scanner.Position), scanner.Memory))
                            && CommonParsers.Spaces0(ref scanner, result, out _)
                        )
                        {
                            if (Terminals.Char(')', ref scanner, advance: true) && CommonParsers.Spaces0(ref scanner, result, out _))
                            {
                                if (EffectStatementParsers.Statement(ref scanner, result, out var statement, new(SDSLParsingMessages.SDSL0010, scanner.GetErrorLocation(scanner.Position), scanner.Memory)))
                                {
                                    parsed = new(typeName, identifier, collection, statement, scanner.GetLocation(position..scanner.Position));
                                    return true;
                                }
                            }
                            else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0018, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
                        }
                    }
                    else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0001, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
                }
            }
            else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0035, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}