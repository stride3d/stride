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
        List<Expression> arraySizes = null!;
        Expression? value = null!;

        var hasAttributes = ShaderAttributeListParser.AttributeList(ref scanner, result, out var attributes) && CommonParsers.Spaces0(ref scanner, result, out _);

        if (Terminals.Literal("compose", ref scanner))
            return CommonParsers.Exit(ref scanner, result, out parsed, position);


        var hasModifier =
            CommonParsers.VariableModifiers(ref scanner, result, out var isStaged, out var streamKind, out var interpolation, out var typeModifier, out var storageClass, advance: true)
            && CommonParsers.Spaces0(ref scanner, result, out _);

        if (CommonParsers.TypeNameIdentifierArraySizeValue(ref scanner, result, out typeName, out var identifier, out arraySizes, out value))
        {
            if (
                CommonParsers.FollowedBy(ref scanner, Terminals.Char(':'), withSpaces: true, advance: true)
                && CommonParsers.FollowedByDel(ref scanner, result, LiteralsParser.Identifier, out Identifier semantic, withSpaces: true, advance: true)
            )
            {
                if (CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true, advance: true))
                {
                    typeName.ArraySize = arraySizes;
                    parsed = new(typeName, identifier, value, arraySizes != null, scanner.GetLocation(position..scanner.Position), semantic: semantic, arraySizes: arraySizes)
                    {
                        Attributes = hasAttributes ? attributes.Attributes : null!,
                        IsStaged = isStaged,
                        Interpolation = interpolation,
                        StreamKind = streamKind,
                        TypeModifier = typeModifier,
                    };
                    return true;
                }
                else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0001, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
            }
            else if (CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true, advance: true))
            {
                parsed = new(typeName, identifier, value, arraySizes != null, scanner.GetLocation(position..scanner.Position), arraySizes: arraySizes)
                {
                    Attributes = hasAttributes ? attributes.Attributes : null!,
                    IsStaged = isStaged,
                    Interpolation = interpolation,
                    StreamKind = streamKind,
                    TypeModifier = typeModifier,
                };
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
            CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true, advance: true);
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

public record struct ShaderSamplerStateParser : IParser<ShaderSamplerState>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderSamplerState parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Terminals.Literal("SamplerState", ref scanner, advance: true)
            && CommonParsers.Spaces1(ref scanner, result, out _)
            && LiteralsParser.Identifier(ref scanner, result, out var identifier)

        )
        {
            if (
                CommonParsers.FollowedBy(ref scanner, Terminals.Char('{'), withSpaces: true, advance: true)
                && CommonParsers.Spaces0(ref scanner, result, out _)
                && CommonParsers.Repeat(ref scanner, result, SamplerStateValueAssignment, out List<SamplerStateAssign> assignments, 0, withSpaces: true)
                && CommonParsers.FollowedBy(ref scanner, Terminals.Char('}'), withSpaces: true, advance: true)
                && CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true, advance: true)
            )
            {
                parsed = new(identifier, scanner.GetLocation(position..scanner.Position))
                {
                    Members = assignments
                };
                return true;
            }
            else if (CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true, advance: true))
            {
                parsed = new(identifier, scanner.GetLocation(position..scanner.Position));
                return true;
            }
            else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0019, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool SamplerStateValueAssignment<TScanner>(ref TScanner scanner, ParseResult result, out SamplerStateAssign parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            CommonParsers.FollowedBy(ref scanner, result, LiteralsParser.Identifier, out Identifier identifier, withSpaces: true, advance: true)
            && CommonParsers.FollowedBy(ref scanner, Terminals.Char('='), withSpaces: true, advance: true)
            && CommonParsers.FollowedByDel(ref scanner, result, ExpressionParser.Expression, out Expression expression, withSpaces: true, advance: true)
            && CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true, advance: true)
        )
        {
            parsed = new SamplerStateAssign(identifier, expression, scanner.GetLocation(position..scanner.Position));
            return true;
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct ShaderSamplerComparisonStateParser : IParser<ShaderSamplerComparisonState>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderSamplerComparisonState parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Terminals.Literal("SamplerComparisonState", ref scanner, advance: true)
            && CommonParsers.Spaces1(ref scanner, result, out _)
            && LiteralsParser.Identifier(ref scanner, result, out var identifier)

        )
        {
            if (
                CommonParsers.FollowedBy(ref scanner, Terminals.Char('{'), withSpaces: true, advance: true)
                && CommonParsers.Spaces0(ref scanner, result, out _)
                && CommonParsers.Repeat(ref scanner, result, SamplerStateValueAssignment, out List<SamplerStateAssign> assignments, 0, withSpaces: true)
                && CommonParsers.FollowedBy(ref scanner, Terminals.Char('}'), withSpaces: true, advance: true)
                && CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true, advance: true)
            )
            {
                parsed = new(identifier, scanner.GetLocation(position..scanner.Position))
                {
                    Members = assignments
                };
                return true;
            }
            else if (CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true, advance: true))
            {
                parsed = new(identifier, scanner.GetLocation(position..scanner.Position));
                return true;
            }
            else return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0019, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool SamplerStateValueAssignment<TScanner>(ref TScanner scanner, ParseResult result, out SamplerStateAssign parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            CommonParsers.FollowedBy(ref scanner, result, LiteralsParser.Identifier, out Identifier identifier, withSpaces: true, advance: true)
            && CommonParsers.FollowedBy(ref scanner, Terminals.Char('='), withSpaces: true, advance: true)
            && CommonParsers.FollowedByDel(ref scanner, result, ExpressionParser.Expression, out Expression expression, withSpaces: true, advance: true)
            && CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true, advance: true)
        )
        {
            parsed = new SamplerStateAssign(identifier, expression, scanner.GetLocation(position..scanner.Position));
            return true;
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}
public record struct ShaderStructMemberParser : IParser<ShaderStructMember>
{

    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderStructMember parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        var hasAttributes = ShaderAttributeListParser.AttributeList(ref scanner, result, out var attributes);
        if (
            CommonParsers.FollowedBy(ref scanner, result, LiteralsParser.TypeName, out TypeName typename, withSpaces: true, advance: true)
            && CommonParsers.Spaces1(ref scanner, result, out _)
            && CommonParsers.FollowedBy(ref scanner, result, LiteralsParser.Identifier, out Identifier identifier, withSpaces: true, advance: true)
            && CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true)
        )
        {
            parsed = new ShaderStructMember(typename, identifier, scanner.GetLocation(position..scanner.Position));
            if (hasAttributes)
                parsed.Attributes = attributes.Attributes;
            return true;
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

