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
        if (LiteralsParser.TypeName(ref scanner, result, out var typename)
            && CommonParsers.Spaces1(ref scanner, result, out _)
        )
        {
            if (
                LiteralsParser.Identifier(ref scanner, result, out Identifier name)
                && CommonParsers.FollowedBy(ref scanner, Terminals.Set("=;"), withSpaces: true)
            )
            {
                if (CommonParsers.FollowedBy(ref scanner, Terminals.Set(";"), withSpaces: true))
                {
                    CommonParsers.Until(ref scanner, ';', advance: true);
                    parsed = new ShaderMember(typename, name, null, false, scanner.GetLocation(position..scanner.Position));
                    return true;
                }
                else if (CommonParsers.FollowedBy(ref scanner, Terminals.Set("="), withSpaces: true))
                {
                    CommonParsers.Until(ref scanner, '=', advance: true);
                    CommonParsers.Spaces0(ref scanner, result, out _);
                    if (ExpressionParser.Expression(ref scanner, result, out var expression, orError: orError ?? new(SDSLErrors.SDSL0015, scanner.GetErrorLocation(scanner.Position), scanner.Memory)))
                    {
                        if (CommonParsers.FollowedBy(ref scanner, Terminals.Char(':')))
                        {
                            CommonParsers.Until(ref scanner, ':', advance: true);
                            CommonParsers.Spaces0(ref scanner, result, out _);
                            if (LiteralsParser.Identifier(ref scanner, result, out var semantic, orError ?? new(SDSLErrors.SDSL0017, scanner.GetErrorLocation(scanner.Position), scanner.Memory)))
                            {
                                if (CommonParsers.Spaces0(ref scanner, result, out _) && Terminals.Char(';', ref scanner))
                                {
                                    parsed = new ShaderMember(typename, name, expression, false, scanner.GetLocation(position..scanner.Position), semantic: semantic);
                                    return true;
                                }
                                else return CommonParsers.Exit(ref scanner, result, out parsed, position, new("Missing semi colon here", scanner.GetErrorLocation(scanner.Position), scanner.Memory));

                            }
                            else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
                        }
                        parsed = new ShaderMember(typename, name, expression, false, scanner.GetLocation(position..scanner.Position));
                        return true;
                    }
                }
            }
            else if (
                LiteralsParser.Identifier(ref scanner, result, out Identifier name2)
                && CommonParsers.Spaces0(ref scanner, result, out _)
                && Terminals.Char('[', ref scanner, advance: true)
                && CommonParsers.Spaces0(ref scanner, result, out _)
                && CommonParsers.Optional(ref scanner, new ExpressionParser(), result, out var arraySize)
                && CommonParsers.Spaces0(ref scanner, result, out _)
                && Terminals.Char(']', ref scanner, advance: true)
                && CommonParsers.FollowedBy(ref scanner, Terminals.Set("=;"), withSpaces: true)
            )
            {
                typename.IsArray = true;
                typename.ArraySize = arraySize;
                if (CommonParsers.FollowedBy(ref scanner, Terminals.Set(";"), withSpaces: true))
                {
                    CommonParsers.Until(ref scanner, ';', advance: true);
                    parsed = new ShaderMember(typename, name2, null, true, scanner.GetLocation(position..scanner.Position), arraySize: arraySize);
                    return true;
                }
                else if (CommonParsers.FollowedBy(ref scanner, Terminals.Set("="), withSpaces: true))
                {
                    CommonParsers.Until(ref scanner, '=', advance: true);
                    CommonParsers.Spaces0(ref scanner, result, out _);
                    if (ExpressionParser.Expression(ref scanner, result, out var expression, orError: orError ?? new(SDSLErrors.SDSL0015, scanner.GetErrorLocation(scanner.Position), scanner.Memory)))
                    {
                        if (CommonParsers.FollowedBy(ref scanner, Terminals.Char(':')))
                        {
                            CommonParsers.Until(ref scanner, ':', advance: true);
                            CommonParsers.Spaces0(ref scanner, result, out _);
                            if (LiteralsParser.Identifier(ref scanner, result, out var semantic, orError ?? new(SDSLErrors.SDSL0017, scanner.GetErrorLocation(scanner.Position), scanner.Memory)))
                            {
                                if (CommonParsers.Spaces0(ref scanner, result, out _) && Terminals.Char(';', ref scanner))
                                {
                                    parsed = new ShaderMember(typename, name2, expression, true, scanner.GetLocation(position..scanner.Position), semantic: semantic, arraySize: arraySize);
                                    return true;
                                }
                                else return CommonParsers.Exit(ref scanner, result, out parsed, position, new("Missing semi colon here", scanner.GetErrorLocation(scanner.Position), scanner.Memory));

                            }
                            else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
                        }
                        parsed = new ShaderMember(typename, name2, expression, true, scanner.GetLocation(position..scanner.Position), arraySize: arraySize);
                        return true;
                    }
                }
            }

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
            else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrors.SDSL0019, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
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

