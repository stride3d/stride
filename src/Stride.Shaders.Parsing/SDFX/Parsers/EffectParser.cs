using Stride.Shaders.Parsing.SDFX.AST;
using Stride.Shaders.Parsing.SDSL;

namespace Stride.Shaders.Parsing.SDFX.Parsers;


public record struct EffectParser : IParser<ShaderEffect>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderEffect parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Terminals.Literal("effect", ref scanner, advance: true) && CommonParsers.Spaces1(ref scanner, result, out _))
        {
            if (LiteralsParser.TypeName(ref scanner, result, out var effectName) && CommonParsers.Spaces0(ref scanner, result, out _))
            {
                parsed = new(effectName, new());
                if (Terminals.Char('{', ref scanner, advance: true) && CommonParsers.Spaces0(ref scanner, result, out _))
                {
                    while(
                       !scanner.IsEof
                       && !Terminals.Char('}', ref scanner)
                    )
                    {
                        if (EffectStatementParsers.Statement(ref scanner, result, out var s) && CommonParsers.Spaces0(ref scanner, result, out _))
                        {
                            parsed.Members.Add(s);
                        }
                        else return CommonParsers.Exit(ref scanner, result, out parsed, position, new("Expected statement here", scanner.CreateError(scanner.Position)));
                    }
                    if(scanner.IsEof)
                        return CommonParsers.Exit(ref scanner, result, out parsed, position, new("Expected statement or end of block", scanner.CreateError(scanner.Position)));
                    else if(Terminals.Char('}', ref scanner, advance: true))
                    {
                        parsed.Info = scanner.GetLocation(position..scanner.Position);
                        CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true, advance: true);
                        return true;
                    }
                }
            }
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool Effect<TScanner>(ref TScanner scanner, ParseResult result, out ShaderEffect parsed, in ParseError? orError = null) where TScanner : struct, IScanner
            => new EffectParser().Match(ref scanner, result, out parsed, orError);
    public static bool EffectStatement<TScanner>(ref TScanner scanner, ParseResult result, out EffectStatement parsed, in ParseError? orError = null) where TScanner : struct, IScanner
            => new EffectStatementParsers().Match(ref scanner, result, out parsed, orError);
}