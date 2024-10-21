using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;



public record struct CompositionParser() : IParser<ShaderCompose>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderCompose parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Terminals.Literal("compose", ref scanner, advance: true) && CommonParsers.Spaces1(ref scanner, result, out _))
        {
            var tmp = scanner.Position;
            if (
                ShaderClassParsers.Mixin(ref scanner, result, out var mixin2)
                && CommonParsers.Spaces1(ref scanner, result, out _)
                && LiteralsParser.Identifier(ref scanner, result, out var identifier2)
                && 
                    (
                        Terminals.Literal("[]", ref scanner, advance: true) 
                        || CommonParsers.SequenceOf(ref scanner, ["[", "]"], advance: true)
                    )
            )
            {
                CommonParsers.Spaces0(ref scanner, result, out _);
                if (!Terminals.Char(';', ref scanner, advance: true))
                    return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrors.SDSL0033, scanner.GetErrorLocation(position), scanner.Memory));
                parsed = new(identifier2, mixin2, true, scanner.GetLocation(position..));
                return true;
            }
            scanner.Position = tmp;
            if(
                ShaderClassParsers.Mixin(ref scanner, result, out var mixin)
                && CommonParsers.Spaces1(ref scanner, result, out _)
                && LiteralsParser.Identifier(ref scanner, result, out var identifier)
            )
            {
                CommonParsers.Spaces0(ref scanner, result, out _);
                if (!Terminals.Char(';', ref scanner, advance: true))
                    return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrors.SDSL0033, scanner.GetErrorLocation(position), scanner.Memory));
                parsed = new(identifier, mixin, false, scanner.GetLocation(position..));
                return true;
            }
            else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrors.SDSL0032, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
        }
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}