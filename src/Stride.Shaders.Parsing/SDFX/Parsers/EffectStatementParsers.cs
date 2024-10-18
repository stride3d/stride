using Stride.Shaders.Parsing.SDFX.AST;
using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDFX.Parsers;


public record struct EffectStatementParsers : IParser<EffectStatement>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out EffectStatement parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (UsingParams(ref scanner, result, out var p1, orError) && CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true, advance: true))
        {
            parsed = p1;
            return true;
        }
        else if (MixinCompose(ref scanner, result, out var p2, orError) && CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true, advance: true))
        {
            parsed = p2;
            return true;
        }
        else if (MixinUse(ref scanner, result, out var p3, orError) && CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true, advance: true))
        {
            parsed = p3;
            return true;
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool Statement<TScanner>(ref TScanner scanner, ParseResult result, out EffectStatement parsed, in ParseError? orError = null) where TScanner : struct, IScanner
        => new EffectStatementParsers().Match(ref scanner, result, out parsed, orError);

    public static bool UsingParams<TScanner>(ref TScanner scanner, ParseResult result, out UsingParams parsed, in ParseError? orError = null) where TScanner : struct, IScanner
        => new UsingParamsParser().Match(ref scanner, result, out parsed, orError);

    public static bool MixinCompose<TScanner>(ref TScanner scanner, ParseResult result, out MixinCompose parsed, in ParseError? orError = null) where TScanner : struct, IScanner
        => new MixinComposeParser().Match(ref scanner, result, out parsed, orError);
    public static bool MixinUse<TScanner>(ref TScanner scanner, ParseResult result, out AST.MixinUse parsed, in ParseError? orError = null) where TScanner : struct, IScanner
        => new MixinUseParser().Match(ref scanner, result, out parsed, orError);
}


public record struct UsingParamsParser : IParser<UsingParams>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out UsingParams parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (CommonParsers.SequenceOf(ref scanner, ["using", "params"], advance: true))
        {
            if (LiteralsParser.Identifier(ref scanner, result, out var identifier))
            {
                parsed = new(identifier, scanner.GetLocation(position..scanner.Position));
                return true;
            }

        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct MixinComposeParser : IParser<MixinCompose>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out MixinCompose parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            CommonParsers.SequenceOf(ref scanner, ["mixin", "compose"], advance: true)
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
                parsed = new(name, mixin, scanner.GetLocation(position..scanner.Position));
                return true;
            }
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct MixinUseParser : IParser<MixinUse>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out MixinUse parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Terminals.Literal("mixin", ref scanner, advance: true)
            && CommonParsers.Spaces1(ref scanner, result, out _)
            && ShaderClassParsers.Mixin(ref scanner, result, out var mixin)
        )
        {
            parsed = new(mixin, scanner.GetLocation(position..scanner.Position));
            return true;
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}