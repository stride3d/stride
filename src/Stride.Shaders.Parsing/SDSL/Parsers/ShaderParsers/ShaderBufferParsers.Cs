using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;


public record struct BufferParsers : IParser<ShaderBuffer>
{
    public static bool Buffer<TScanner>(ref TScanner scanner, ParseResult result, out ShaderBuffer parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new BufferParsers().Match(ref scanner, result, out parsed, orError);

    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderBuffer parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        return CommonParsers.Alternatives(
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
        if (Terminals.Literal("tbuffer", ref scanner, advance: true) && CommonParsers.Spaces1(ref scanner, result, out _))
        {
            if (
                BufferName(ref scanner, result, out var identifiers, new(SDSLParsingMessages.SDSL0017, scanner.GetErrorLocation(scanner.Position), scanner.Memory))
                && CommonParsers.Spaces0(ref scanner, result, out _)
            )
            {
                if (Terminals.Char('{', ref scanner))
                {
                    List<ShaderMember> members = [];
                    CommonParsers.Spaces0(ref scanner, result, out _);
                    do
                    {
                        if (Member(ref scanner, result, out var member) && CommonParsers.Spaces0(ref scanner, result, out _))
                            members.Add(member);
                    }
                    while (!(Terminals.Letter(ref scanner) || Terminals.Char('_', ref scanner)));
                    if (Terminals.Char('}', ref scanner, advance: true))
                    {
                        parsed = new TBuffer(identifiers, scanner.GetLocation(position..scanner.Position))
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

    public static bool CBuffer<TScanner>(ref TScanner scanner, ParseResult result, out ShaderBuffer parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Terminals.Literal("cbuffer", ref scanner, advance: true) && CommonParsers.Spaces1(ref scanner, result, out _))
        {
            if (
                BufferName(ref scanner, result, out var identifiers, new(SDSLParsingMessages.SDSL0017, scanner.GetErrorLocation(scanner.Position), scanner.Memory))
                && CommonParsers.Spaces0(ref scanner, result, out _)
            )
            {
                if (Terminals.Char('{', ref scanner, advance: true))
                {
                    List<ShaderMember> members = [];
                    CommonParsers.Spaces0(ref scanner, result, out _);
                    while (!scanner.IsEof && !Terminals.Char('}', ref scanner, advance: true))
                    {
                        if (
                            Member(ref scanner, result, out var member)
                            && CommonParsers.Spaces0(ref scanner, result, out _)
                        )
                            members.Add(member);
                        else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0001, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
                    }
                    if (scanner.IsEof)
                        return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0043, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
                    parsed = new CBuffer(identifiers, scanner.GetLocation(position..scanner.Position))
                    {
                        Members = members
                    };
                    return true;

                }
                else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
            }
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool RGroup<TScanner>(ref TScanner scanner, ParseResult result, out ShaderBuffer parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Terminals.Literal("rgroup", ref scanner, advance: true) && CommonParsers.Spaces1(ref scanner, result, out _))
        {
            if (
                BufferName(ref scanner, result, out var identifiers)
                && CommonParsers.Spaces0(ref scanner, result, out _)
            )
            {
                if (Terminals.Char('{', ref scanner, advance: true))
                {
                    List<ShaderMember> members = [];
                    CommonParsers.Spaces0(ref scanner, result, out _);
                    while (!scanner.IsEof && !Terminals.Char('}', ref scanner, advance: true))
                    {
                        if (
                            Member(ref scanner, result, out var member)
                            && CommonParsers.Spaces0(ref scanner, result, out _)
                        )
                            members.Add(member);
                        else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0001, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
                    }
                    if (scanner.IsEof)
                        return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0043, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
                    parsed = new RGroup(identifiers, scanner.GetLocation(position..scanner.Position))
                    {
                        Members = members
                    };
                    return true;

                }
                else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
            }
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool Member<TScanner>(ref TScanner scanner, ParseResult result, out ShaderMember parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        CommonParsers.Spaces0(ref scanner, result, out _);
        var position = scanner.Position;
        var isStage = false;
        StreamKind streamKind = StreamKind.None;
        bool hasAttributes = ShaderAttributeListParser.AttributeList(ref scanner, result, out var attributes, orError);
        var tmp = scanner.Position;
        if (Terminals.Literal("stage", ref scanner, advance: true) && CommonParsers.Spaces1(ref scanner, result, out _))
        {
            isStage = true;
            tmp = scanner.Position;
        }
        else
            scanner.Position = tmp;
        if (
            CommonParsers.TypeNameIdentifierArraySizeValue(ref scanner, result, out var typename, out var identifier, out var arraySizes, out var value, advance: true)
            && CommonParsers.FollowedBy(ref scanner, Terminals.Set(";"), withSpaces: true, advance: true)
        )
        {
            parsed = new ShaderMember(typename, identifier, null, true, scanner.GetLocation(position..scanner.Position), isStage, streamKind, arraySizes: arraySizes);
            return true;
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool BufferName<TScanner>(ref TScanner scanner, ParseResult result, out List<Identifier> parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        return CommonParsers.Repeat(ref scanner, result, LiteralsParser.Identifier, out parsed, 1, withSpaces: true, separator: ".");
    }
}