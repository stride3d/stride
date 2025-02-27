using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;



public record struct CompositionParser() : IParser<ShaderCompose>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderCompose parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;

        var hasAttributes = ShaderAttributeListParser.AttributeList(ref scanner, result, out var attributes) && Parsers.Spaces0(ref scanner, result, out _);
        var isStaged = Tokens.Literal("stage", ref scanner, advance: true) && Parsers.Spaces1(ref scanner, result, out _);
        
        if (Tokens.Literal("compose", ref scanner, advance: true) && Parsers.Spaces1(ref scanner, result, out _))
        {
            var tmp = scanner.Position;
            if (Parsers.MixinIdentifierArraySizeValue(ref scanner, result, out var mixin, out var name, out var arraysize, out var value, advance: true))
            {
                Parsers.Spaces0(ref scanner, result, out _);
                if (!Tokens.Char(';', ref scanner, advance: true))
                    return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0033, scanner[position], scanner.Memory));
                parsed = new(name, mixin, true, scanner[position..])
                {
                    Attributes = hasAttributes ? attributes.Attributes : null!,
                    IsStaged = isStaged
                };
                return true;
            }
            else return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0032, scanner[scanner.Position], scanner.Memory));
        }
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}