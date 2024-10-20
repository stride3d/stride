using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;


public record struct BufferParsers : IParser<ShaderBuffer>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderBuffer parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        parsed = null!;
        if (CBuffer(ref scanner, result, out var cbuff, orError))
        {
            parsed = cbuff;
            return true;
        }
        else if (TBuffer(ref scanner, result, out var tbuff, orError))
        {
            parsed = tbuff;
            return true;
        }
        return false;
    }

    public static bool Buffer<TScanner>(ref TScanner scanner, ParseResult result, out ShaderBuffer parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new BufferParsers().Match(ref scanner, result, out parsed, orError);

    public static bool TBuffer<TScanner>(ref TScanner scanner, ParseResult result, out TBuffer parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new TBufferParser().Match(ref scanner, result, out parsed, orError);

    public static bool CBuffer<TScanner>(ref TScanner scanner, ParseResult result, out CBuffer parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new CBufferParser().Match(ref scanner, result, out parsed, orError);

    public static bool Member<TScanner>(ref TScanner scanner, ParseResult result, out ShaderMember parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new BufferMemberParser().Match(ref scanner, result, out parsed, orError);
}


public record struct CBufferParser : IParser<CBuffer>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out CBuffer parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Terminals.Literal("cbuffer ", ref scanner, advance: true))
        {
            if (
                LiteralsParser.Identifier(ref scanner, result, out var identifier, new(SDSLErrors.SDSL0017, scanner.GetErrorLocation(scanner.Position), scanner.Memory))
                && CommonParsers.Spaces0(ref scanner, result, out _)
            )
            {
                if (Terminals.Char('{', ref scanner))
                {
                    List<ShaderMember> members = [];
                    CommonParsers.Spaces0(ref scanner, result, out _);
                    do
                    {
                        if (BufferParsers.Member(ref scanner, result, out var member) && CommonParsers.Spaces0(ref scanner, result, out _))
                            members.Add(member);
                    }
                    while (!(Terminals.Letter(ref scanner) || Terminals.Char('_', ref scanner)));
                    if (Terminals.Char('}', ref scanner, advance: true))
                    {
                        parsed = new(identifier, scanner.GetLocation(position..scanner.Position))
                        {
                            Members = members
                        };
                        return true;
                    }
                }
                else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
            }
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);

    }
}
public record struct TBufferParser : IParser<TBuffer>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out TBuffer parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Terminals.Literal("tbuffer ", ref scanner, advance: true))
        {
            if (
                LiteralsParser.Identifier(ref scanner, result, out var identifier, new(SDSLErrors.SDSL0017, scanner.GetErrorLocation(scanner.Position), scanner.Memory))
                && CommonParsers.Spaces0(ref scanner, result, out _)
            )
            {
                if (Terminals.Char('{', ref scanner))
                {
                    List<ShaderMember> members = [];
                    CommonParsers.Spaces0(ref scanner, result, out _);
                    do
                    {
                        if (BufferParsers.Member(ref scanner, result, out var member) && CommonParsers.Spaces0(ref scanner, result, out _))
                            members.Add(member);
                    }
                    while (!(Terminals.Letter(ref scanner) || Terminals.Char('_', ref scanner)));
                    if (Terminals.Char('}', ref scanner, advance: true))
                    {
                        parsed = new(identifier, scanner.GetLocation(position..scanner.Position))
                        {
                            Members = members
                        };
                        return true;
                    }
                }
                else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
            }
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError); ;

    }
}

public record struct BufferMemberParser : IParser<ShaderMember>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderMember parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        var isStage = false;
        var isStream = false;
        if (Terminals.Literal("stage", ref scanner, advance: true) && CommonParsers.Spaces1(ref scanner, result, out _))
            isStage = true;
        else
            scanner.Position = position;
        if (LiteralsParser.TypeName(ref scanner, result, out var typename)
            && CommonParsers.Spaces1(ref scanner, result, out _)
            && LiteralsParser.Identifier(ref scanner, result, out Identifier name)
            && CommonParsers.FollowedBy(ref scanner, Terminals.Set("=;"), withSpaces: true)
        )
        {
            if (CommonParsers.FollowedBy(ref scanner, Terminals.Set(";"), withSpaces: true))
            {
                CommonParsers.Until(ref scanner, ';', advance: true);
                parsed = new ShaderMember(typename, name, null, scanner.GetLocation(position..scanner.Position), isStage, isStream);
                return true;
            }
            else if (CommonParsers.FollowedBy(ref scanner, Terminals.Set("="), withSpaces: true))
            {
                CommonParsers.Until(ref scanner, '=', advance: true);
                CommonParsers.Spaces0(ref scanner, result, out _);
                if (ExpressionParser.Expression(ref scanner, result, out var expression, orError: orError ?? new(SDSLErrors.SDSL0015, scanner.GetErrorLocation(scanner.Position), scanner.Memory)))
                {
                    if (CommonParsers.Spaces0(ref scanner, result, out _) && Terminals.Char(';', ref scanner))
                    {
                        parsed = new ShaderMember(typename, name, expression, scanner.GetLocation(position..scanner.Position), isStage, isStream);
                        return true;
                    }
                    else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
                }
            }
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}
