using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;



public record struct CompositionParser() : IParser<ShaderCompose>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderCompose parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;

        var hasAttributes = ShaderAttributeListParser.AttributeList(ref scanner, result, out var attributes) && CommonParsers.Spaces0(ref scanner, result, out _);
        var isStaged = Terminals.Literal("stage", ref scanner, advance: true) && CommonParsers.Spaces1(ref scanner, result, out _);
        
        if (Terminals.Literal("compose", ref scanner, advance: true) && CommonParsers.Spaces1(ref scanner, result, out _))
        {
            var tmp = scanner.Position;
            if (CommonParsers.MixinIdentifierArraySizeValue(ref scanner, result, out var mixin, out var name, out var arraysize, out var value, advance: true))
            {
                CommonParsers.Spaces0(ref scanner, result, out _);
                if (!Terminals.Char(';', ref scanner, advance: true))
                    return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0033, scanner.GetErrorLocation(position), scanner.Memory));
                parsed = new(name, mixin, true, scanner.GetLocation(position..))
                {
                    Attributes = hasAttributes ? attributes.Attributes : null!,
                    IsStaged = isStaged
                };
                return true;
            }
            else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0032, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
        }
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}