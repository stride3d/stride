using System.Runtime;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;

public record struct ShaderMemberParser : IParser<ShaderMember>
{
    public static bool Member<TScanner>(ref TScanner scanner, ParseResult result, out ShaderMember parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new ShaderMemberParser().Match(ref scanner, result, out parsed, in orError);

    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderMember parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        parsed = null!;
        var position = scanner.Position;

        TypeName? typeName = null!;
        Expression? arraySize = null!;
        Expression? value = null!;


        if (!Terminals.Literal("compose", ref scanner) && CommonParsers.TypeNameIdentifierArraySizeValue(ref scanner, result, out typeName, out var identifier, out arraySize, out value))
        {
            if (
                CommonParsers.FollowedBy(ref scanner, Terminals.Char(':'), withSpaces: true, advance: true)
                && CommonParsers.FollowedByDel(ref scanner, result, LiteralsParser.Identifier, out Identifier semantic, withSpaces: true, advance: true)
            )
            {
                if (CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true, advance: true))
                {
                    parsed = new(typeName, identifier, value, arraySize != null, scanner.GetLocation(position..scanner.Position), semantic: semantic, arraySize: arraySize);
                    return true;
                }
                else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0001, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
            }
            else if (CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true, advance: true))
            {
                parsed = new(typeName, identifier, value, arraySize != null, scanner.GetLocation(position..scanner.Position), arraySize: arraySize);
                return true;
            }
            else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0013, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}
public record struct ShaderStructParser : IParser<ShaderStruct>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderStruct parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Terminals.Literal("struct", ref scanner, advance: true)
            && CommonParsers.Spaces1(ref scanner, result, out _)
            && LiteralsParser.Identifier(ref scanner, result, out var identifier)
            && CommonParsers.FollowedBy(ref scanner, Terminals.Char('{'), withSpaces: true, advance: true)
        )
        {
            CommonParsers.Spaces0(ref scanner, result, out _);
            parsed = new ShaderStruct(identifier, scanner.GetLocation(position..));
            CommonParsers.Repeat<TScanner, ShaderStructMemberParser, ShaderStructMember>(ref scanner, new ShaderStructMemberParser(), result, out var members, 0, withSpaces: true, separator: ";");
            parsed.Members = members;
            if (CommonParsers.FollowedBy(ref scanner, Terminals.Char('}'), withSpaces: true, advance: true))
            {
                CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true, advance: true);
                parsed.Info = scanner.GetLocation(position..scanner.Position);
                return true;
            }
            else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0019, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}
public record struct ShaderStructMemberParser : IParser<ShaderStructMember>
{

    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderStructMember parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            LiteralsParser.TypeName(ref scanner, result, out var typename)
            && CommonParsers.Spaces1(ref scanner, result, out _)
            && LiteralsParser.Identifier(ref scanner, result, out var identifier)
            && CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true)
        )
        {
            parsed = new ShaderStructMember(typename, identifier, scanner.GetLocation(position..scanner.Position));
            return true;
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

