using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;


public record struct BufferParsers : IParser<ShaderBuffer>
{
    public static bool Buffer<TScanner>(ref TScanner scanner, ParseResult result, out ShaderBuffer parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new BufferParsers().Match(ref scanner, result, out parsed, orError);

    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderBuffer parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        return Parsers.Alternatives(
            ref scanner, result, out parsed, in orError,
            CBuffer,
            TBuffer,
            RGroup
        );
    }

    public static bool TBuffer<TScanner>(ref TScanner scanner, ParseResult result, out ShaderBuffer parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Tokens.Literal("tbuffer", ref scanner, advance: true) && Parsers.Spaces1(ref scanner, result, out _))
        {
            if (
                BufferName(ref scanner, result, out var identifiers, new(SDSLErrorMessages.SDSL0017, scanner[scanner.Position], scanner.Memory))
                && Parsers.Spaces0(ref scanner, result, out _)
            )
            {
                if (Tokens.Char('{', ref scanner))
                {
                    List<ShaderMember> members = [];
                    Parsers.Spaces0(ref scanner, result, out _);
                    do
                    {
                        if (Member(ref scanner, result, out var member) && Parsers.Spaces0(ref scanner, result, out _))
                            members.Add(member);
                    }
                    while (!(Tokens.Letter(ref scanner) || Tokens.Char('_', ref scanner)));
                    if (Tokens.Char('}', ref scanner, advance: true))
                    {
                        parsed = new TBuffer(identifiers, scanner[position..scanner.Position])
                        {
                            Members = members
                        };
                        return true;
                    }
                }
                else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
            }
        }
        return Parsers.Exit(ref scanner, result, out parsed, position, orError); ;
    }

    public static bool CBuffer<TScanner>(ref TScanner scanner, ParseResult result, out ShaderBuffer parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Tokens.Literal("cbuffer", ref scanner, advance: true) && Parsers.Spaces1(ref scanner, result, out _))
        {
            if (
                BufferName(ref scanner, result, out var identifiers, new(SDSLErrorMessages.SDSL0017, scanner[scanner.Position], scanner.Memory))
                && Parsers.Spaces0(ref scanner, result, out _)
            )
            {
                if (Tokens.Char('{', ref scanner, advance: true))
                {
                    List<ShaderMember> members = [];
                    Parsers.Spaces0(ref scanner, result, out _);
                    while (!scanner.IsEof && !Tokens.Char('}', ref scanner, advance: true))
                    {
                        if (
                            Member(ref scanner, result, out var member)
                            && Parsers.Spaces0(ref scanner, result, out _)
                        )
                            members.Add(member);
                        else return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0001, scanner[scanner.Position], scanner.Memory));
                    }
                    if (scanner.IsEof)
                        return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0043, scanner[scanner.Position], scanner.Memory));
                    parsed = new CBuffer(identifiers, scanner[position..scanner.Position])
                    {
                        Members = members
                    };
                    return true;

                }
                else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
            }
        }
        return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool RGroup<TScanner>(ref TScanner scanner, ParseResult result, out ShaderBuffer parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Tokens.Literal("rgroup", ref scanner, advance: true) && Parsers.Spaces1(ref scanner, result, out _))
        {
            if (
                BufferName(ref scanner, result, out var identifiers)
                && Parsers.Spaces0(ref scanner, result, out _)
            )
            {
                if (Tokens.Char('{', ref scanner, advance: true))
                {
                    List<ShaderMember> members = [];
                    Parsers.Spaces0(ref scanner, result, out _);
                    while (!scanner.IsEof && !Tokens.Char('}', ref scanner, advance: true))
                    {
                        if (
                            Member(ref scanner, result, out var member)
                            && Parsers.Spaces0(ref scanner, result, out _)
                        )
                            members.Add(member);
                        else return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0001, scanner[scanner.Position], scanner.Memory));
                    }
                    if (scanner.IsEof)
                        return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0043, scanner[scanner.Position], scanner.Memory));
                    parsed = new RGroup(identifiers, scanner[position..scanner.Position])
                    {
                        Members = members
                    };
                    return true;

                }
                else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
            }
        }
        return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool Member<TScanner>(ref TScanner scanner, ParseResult result, out ShaderMember parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        Parsers.Spaces0(ref scanner, result, out _);
        var position = scanner.Position;
        var isStage = false;
        StreamKind streamKind = StreamKind.None;
        bool hasAttributes = ShaderAttributeListParser.AttributeList(ref scanner, result, out var attributes, orError);
        var tmp = scanner.Position;
        if (Tokens.Literal("stage", ref scanner, advance: true) && Parsers.Spaces1(ref scanner, result, out _))
        {
            isStage = true;
            tmp = scanner.Position;
        }
        else
            scanner.Position = tmp;
        if (
            Parsers.TypeNameIdentifierArraySizeValue(ref scanner, result, out var typeName, out var identifier, out var value, advance: true)
            && Parsers.FollowedBy(ref scanner, Tokens.Set(";"), withSpaces: true, advance: true)
        )
        {
            parsed = new ShaderMember(typeName, identifier, value, scanner[position..scanner.Position], isStage, streamKind);
            if (hasAttributes)
                parsed.Attributes = attributes.Attributes;
            return true;
        }
        return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool BufferName<TScanner>(ref TScanner scanner, ParseResult result, out Identifier parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        parsed = null!;
        if(Parsers.Repeat(ref scanner, result, LiteralsParser.Identifier, out List<Identifier> identifiers, 1, true, ".", orError))
        {
            parsed = new Identifier(string.Join(".", identifiers.Select(i => i.Name)), scanner[identifiers[0].Info.Range.Start..identifiers[^1].Info.Range.End]);
            return true;
        }
        else return false;
    }
}