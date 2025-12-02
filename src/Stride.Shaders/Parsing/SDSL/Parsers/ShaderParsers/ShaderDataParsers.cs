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
        Expression? value = null!;

        var hasAttributes = ShaderAttributeListParser.AttributeList(ref scanner, result, out var attributes) && Parsers.Spaces0(ref scanner, result, out _);

        var hasModifier =
            Parsers.VariableModifiers(ref scanner, result, out var isStaged, out var isCompose, out var streamKind, out var interpolation, out var typeModifier, out var storageClass, advance: true)
            && Parsers.Spaces0(ref scanner, result, out _);

        if (Parsers.TypeNameIdentifierArraySizeValue(ref scanner, result, out var typeName, out var identifier, out value))
        {
            if (
                Parsers.FollowedBy(ref scanner, Tokens.Char(':'), withSpaces: true, advance: true)
                && Parsers.FollowedByDel(ref scanner, result, LiteralsParser.Identifier, out Identifier semantic, withSpaces: true, advance: true)
            )
            {
                if (Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true, advance: true))
                {
                    parsed = new(typeName, identifier, value, scanner[position..scanner.Position], semantic: semantic)
                    {
                        Attributes = hasAttributes ? attributes.Attributes : null!,
                        IsStaged = isStaged,
                        IsCompose = isCompose,
                        Interpolation = interpolation,
                        StreamKind = streamKind,
                        TypeModifier = typeModifier,
                    };
                    return true;
                }
                else return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0001, scanner[scanner.Position], scanner.Memory));
            }
            else if (Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true, advance: true))
            {
                parsed = new(typeName, identifier, value, scanner[position..scanner.Position])
                {
                    Attributes = hasAttributes ? attributes.Attributes : null!,
                    IsStaged = isStaged,
                    Interpolation = interpolation,
                    StreamKind = streamKind,
                    TypeModifier = typeModifier,
                };
                return true;
            }
            else return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0013, scanner[scanner.Position], scanner.Memory));
        }
        return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}
public record struct ShaderStructParser : IParser<ShaderStruct>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderStruct parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Tokens.Literal("struct", ref scanner, advance: true)
            && Parsers.Spaces1(ref scanner, result, out _)
            && LiteralsParser.Identifier(ref scanner, result, out var identifier)
            && Parsers.FollowedBy(ref scanner, Tokens.Char('{'), withSpaces: true, advance: true)
        )
        {
            Parsers.Spaces0(ref scanner, result, out _);
            parsed = new ShaderStruct(identifier, scanner[position..]);
            Parsers.Repeat<TScanner, ShaderStructMemberParser, ShaderStructMember>(ref scanner, new ShaderStructMemberParser(), result, out var members, 0, withSpaces: true, separator: ";");
            Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true, advance: true);
            parsed.Members = members;
            if (Parsers.FollowedBy(ref scanner, Tokens.Char('}'), withSpaces: true, advance: true))
            {
                Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true, advance: true);
                parsed.Info = scanner[position..scanner.Position];
                return true;
            }
            else return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0019, scanner[scanner.Position], scanner.Memory));
        }
        return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct ShaderSamplerStateParser : IParser<ShaderSamplerState>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderSamplerState parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Tokens.Literal("SamplerState", ref scanner, advance: true)
            && Parsers.Spaces1(ref scanner, result, out _)
            && LiteralsParser.Identifier(ref scanner, result, out var identifier)

        )
        {
            if (
                Parsers.FollowedBy(ref scanner, Tokens.Char('{'), withSpaces: true, advance: true)
                && Parsers.Spaces0(ref scanner, result, out _)
                && Parsers.Repeat(ref scanner, result, SamplerStateValueAssignment, out List<SamplerStateParameter> assignments, 0, withSpaces: true)
                && Parsers.FollowedBy(ref scanner, Tokens.Char('}'), withSpaces: true, advance: true)
                && Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true, advance: true)
            )
            {
                parsed = new(identifier, scanner[position..scanner.Position])
                {
                    Parameters = assignments
                };
                return true;
            }
            else if (Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true, advance: true))
            {
                parsed = new(identifier, scanner[position..scanner.Position]);
                return true;
            }
            else return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0019, scanner[scanner.Position], scanner.Memory));
        }
        return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool SamplerStateValueAssignment<TScanner>(ref TScanner scanner, ParseResult result, out SamplerStateParameter parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Parsers.FollowedBy(ref scanner, result, LiteralsParser.Identifier, out Identifier identifier, withSpaces: true, advance: true)
            && Parsers.FollowedBy(ref scanner, Tokens.Char('='), withSpaces: true, advance: true)
            && Parsers.FollowedByDel(ref scanner, result, ExpressionParser.Expression, out Expression expression, withSpaces: true, advance: true)
            && Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true, advance: true)
        )
        {
            parsed = new SamplerStateParameter(identifier, expression, scanner[position..scanner.Position]);
            return true;
        }
        return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct ShaderSamplerComparisonStateParser : IParser<ShaderSamplerComparisonState>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderSamplerComparisonState parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Tokens.Literal("SamplerComparisonState", ref scanner, advance: true)
            && Parsers.Spaces1(ref scanner, result, out _)
            && LiteralsParser.Identifier(ref scanner, result, out var identifier)

        )
        {
            if (
                Parsers.FollowedBy(ref scanner, Tokens.Char('{'), withSpaces: true, advance: true)
                && Parsers.Spaces0(ref scanner, result, out _)
                && Parsers.Repeat(ref scanner, result, SamplerStateValueAssignment, out List<SamplerStateParameter> assignments, 0, withSpaces: true)
                && Parsers.FollowedBy(ref scanner, Tokens.Char('}'), withSpaces: true, advance: true)
                && Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true, advance: true)
            )
            {
                parsed = new(identifier, scanner[position..scanner.Position])
                {
                    Members = assignments
                };
                return true;
            }
            else if (Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true, advance: true))
            {
                parsed = new(identifier, scanner[position..scanner.Position]);
                return true;
            }
            else return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0019, scanner[scanner.Position], scanner.Memory));
        }
        return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool SamplerStateValueAssignment<TScanner>(ref TScanner scanner, ParseResult result, out SamplerStateParameter parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Parsers.FollowedBy(ref scanner, result, LiteralsParser.Identifier, out Identifier identifier, withSpaces: true, advance: true)
            && Parsers.FollowedBy(ref scanner, Tokens.Char('='), withSpaces: true, advance: true)
            && Parsers.FollowedByDel(ref scanner, result, ExpressionParser.Expression, out Expression expression, withSpaces: true, advance: true)
            && Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true, advance: true)
        )
        {
            parsed = new SamplerStateParameter(identifier, expression, scanner[position..scanner.Position]);
            return true;
        }
        return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}
public record struct ShaderStructMemberParser : IParser<ShaderStructMember>
{

    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderStructMember parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        var hasAttributes = ShaderAttributeListParser.AttributeList(ref scanner, result, out var attributes);
        if (
            Parsers.FollowedBy(ref scanner, result, LiteralsParser.TypeName, out TypeName typename, withSpaces: true, advance: true)
            && Parsers.Spaces1(ref scanner, result, out _)
            && Parsers.FollowedBy(ref scanner, result, LiteralsParser.Identifier, out Identifier identifier, withSpaces: true, advance: true)
            && Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true)
        )
        {
            parsed = new ShaderStructMember(typename, identifier, scanner[position..scanner.Position]);
            if (hasAttributes)
                parsed.Attributes = attributes.Attributes;
            return true;
        }
        return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

