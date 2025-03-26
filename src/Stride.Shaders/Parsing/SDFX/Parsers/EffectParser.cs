using Stride.Shaders.Parsing.SDFX.AST;
using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDFX.Parsers;


public record struct EffectParser : IParser<ShaderEffect>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderEffect parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;

        var isPartial = Tokens.Literal("partial", ref scanner, advance: true) && SDSL.Parsers.Spaces1(ref scanner, result, out _);
        if(!isPartial)
            scanner.Position = position;

        if (Tokens.Literal("effect", ref scanner, advance: true) && SDSL.Parsers.Spaces1(ref scanner, result, out _))
        {
            if (LiteralsParser.TypeName(ref scanner, result, out var effectName) && SDSL.Parsers.Spaces0(ref scanner, result, out _))
            {
                parsed = new((TypeName)effectName, isPartial, new());
                if (Tokens.Char('{', ref scanner, advance: true) && SDSL.Parsers.Spaces0(ref scanner, result, out _))
                {
                    while(
                       !scanner.IsEof
                       && !Tokens.Char('}', ref scanner)
                    )
                    {
                        if (EffectStatementParsers.Statement(ref scanner, result, out var s) && SDSL.Parsers.Spaces0(ref scanner, result, out _))
                        {
                            parsed.Members.Add(s);
                        }
                        else return SDSL.Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0009, scanner[scanner.Position], scanner.Memory));
                    }
                    if(scanner.IsEof)
                        return SDSL.Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0011, scanner[scanner.Position], scanner.Memory));
                    else if(Tokens.Char('}', ref scanner, advance: true))
                    {
                        parsed.Info = scanner[position..scanner.Position];
                        SDSL.Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true, advance: true);
                        return true;
                    }
                }
            }
        }
        return SDSL.Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool Effect<TScanner>(ref TScanner scanner, ParseResult result, out ShaderEffect parsed, in ParseError? orError = null) where TScanner : struct, IScanner
            => new EffectParser().Match(ref scanner, result, out parsed, orError);
    public static bool EffectStatement<TScanner>(ref TScanner scanner, ParseResult result, out EffectStatement parsed, in ParseError? orError = null) where TScanner : struct, IScanner
            => new EffectStatementParsers().Match(ref scanner, result, out parsed, orError);
}