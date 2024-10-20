using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;



public record struct CompositionParser() : IParser<ShaderCompose>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderCompose parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Terminals.Literal("compose", ref scanner, advance: true) && CommonParsers.Spaces1(ref scanner, result, out _))
        {
            if (
                ShaderClassParsers.Mixin(ref scanner, result, out var mixin)
                && CommonParsers.Spaces1(ref scanner, result, out _)
                && LiteralsParser.Identifier(ref scanner, result, out var identifier, new("Expected variable name here", scanner.GetErrorLocation(scanner.Position)))
            )
            {
                CommonParsers.Spaces0(ref scanner, result, out _);
                if (!Terminals.Char(';', ref scanner, advance: true))
                    return CommonParsers.Exit(ref scanner, result, out parsed, position, new("Expected semi colon", scanner.GetErrorLocation(position)));
                parsed = new(identifier, mixin, scanner.GetLocation(position..));
                return true;
            }
            else return CommonParsers.Exit(ref scanner, result, out parsed, position, new("Expected Mixin variable", scanner.GetErrorLocation(scanner.Position)));
        }
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}