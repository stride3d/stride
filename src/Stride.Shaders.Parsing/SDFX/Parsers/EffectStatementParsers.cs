using Stride.Shaders.Parsing.SDFX.AST;
using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDFX.Parsers;


public record struct EffectStatementParsers : IParser<EffectStatement>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out EffectStatement parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        throw new NotImplementedException();
    }
}


public record struct UsingParamsParser : IParser<UsingParams>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out UsingParams parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if(Terminals.Literal("using", ref scanner, advance: true) && CommonParsers.Spaces1(ref scanner, result, out _))
        {
            if(Terminals.Literal("params", ref scanner, advance: true) && CommonParsers.Spaces1(ref scanner, result, out _, orError : new("Expected space here", scanner.CreateError(scanner.Position))))
            {
                if(LiteralsParser.Identifier(ref scanner, result, out var identifier))
                {
                    parsed = new(identifier, scanner.GetLocation(position..scanner.Position));
                    return true;
                }
            }
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct MixinComposeParser : IParser<ComposeMixin>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ComposeMixin parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Terminals.Literal("mixin", ref scanner, advance: true)
            && CommonParsers.Spaces1(ref scanner, result, out _)
            && Terminals.Literal("compose", ref scanner, advance: true)
            && CommonParsers.Spaces1(ref scanner, result, out _)
            && LiteralsParser.Identifier(ref scanner, result, out var name)
            && CommonParsers.Spaces0(ref scanner, result, out _)
            && Terminals.Char('=', ref scanner, advance: true)
            && CommonParsers.Spaces0(ref scanner, result, out _)
            
        )
        {
            if (
                ShaderClassParsers.Mixin(ref scanner, result, out var mixin)
                && CommonParsers.Spaces0(ref scanner, result, out _)
                && Terminals.Char(';', ref scanner, advance: true)
            )
            {
                parsed = new(mixin, scanner.GetLocation(position..scanner.Position));
                return true;
            }
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct MixinParser : IParser<MixinUse>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out MixinUse parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Terminals.Literal("mixin", ref scanner, advance: true)
            && CommonParsers.Spaces1(ref scanner, result, out _)
            && ShaderClassParsers.Mixin(ref scanner, result, out var mixin)
            && CommonParsers.Spaces0(ref scanner, result, out _)
            && Terminals.Char(';', ref scanner, advance: true)
        )
        {
            parsed = new(mixin, scanner.GetLocation(position..scanner.Position));
            return true;
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}