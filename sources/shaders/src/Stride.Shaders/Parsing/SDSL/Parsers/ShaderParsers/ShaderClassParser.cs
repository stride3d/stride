using System.Runtime.CompilerServices;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;


public record struct ShaderClassParsers : IParser<ShaderClass>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderClass parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        if (ComplexClass(ref scanner, result, out parsed, in orError))
            return true;
        else
            return false;
    }
    public static bool Class<TScanner>(ref TScanner scanner, ParseResult result, out ShaderClass parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new ShaderClassParsers().Match(ref scanner, result, out parsed, in orError);
    public static bool ComplexClass<TScanner>(ref TScanner scanner, ParseResult result, out ShaderClass parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new ShaderClassParser().Match(ref scanner, result, out parsed, in orError);
}

public record struct SimpleShaderClassParser : IParser<ShaderClass>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderClass parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;

        if (
            Tokens.Literal("shader", ref scanner, advance: true)
            && Parsers.Spaces1(ref scanner, result, out _, new(SDSLErrorMessages.SDSL0016, scanner[scanner.Position], scanner.Memory))
            && LiteralsParser.Identifier(ref scanner, result, out var className, new(SDSLErrorMessages.SDSL0017, scanner[scanner.Position], scanner.Memory))
            && Parsers.Spaces0(ref scanner, result, out _)
            && Tokens.Char('{', ref scanner, advance: true)
            && Parsers.Spaces0(ref scanner, result, out _)

        )
        {
            var c = new ShaderClass(className, scanner[position..scanner.Position]);
            while (!scanner.IsEof && !Tokens.Char('}', ref scanner, advance: true))
            {
                if (ShaderElementParsers.ShaderElement(ref scanner, result, out var e))
                {
                    c.Elements.Add(e);
                }
                else
                    break;
                Parsers.Spaces0(ref scanner, result, out _);
            }
            parsed = c;
            return true;
        }
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct ShaderClassParser : IParser<ShaderClass>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderClass parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        var tmp = position;
        var @internal = false;
        if (Tokens.Literal("internal", ref scanner, advance: true) && Parsers.Spaces1(ref scanner, result, out _))
        {
            @internal = true;
            tmp = scanner.Position;
        }
        if(Parsers.FollowedBy(ref scanner, Tokens.Literal("partial"), withSpaces: true, advance: true) && Parsers.Spaces1(ref scanner, result, out _))
            tmp = scanner.Position;
        if (
            (
                Tokens.Literal("shader", ref scanner, advance: true) 
                || Tokens.Literal("class", ref scanner, advance: true) 
            )
            && Parsers.Spaces1(ref scanner, result,out _))
        {
            if (
                LiteralsParser.Identifier(ref scanner, result, out var identifier, new(SDSLErrorMessages.SDSL0017, scanner[scanner.Position], scanner.Memory))
                && Parsers.Spaces0(ref scanner, result, out _)
            )
            {
                parsed = new ShaderClass(identifier, scanner[..]);
                parsed.Internal = @internal;
                if (Tokens.Char('<', ref scanner, advance: true))
                {
                    ParameterParsers.Declarations(ref scanner, result, out var generics);
                    Parsers.Spaces0(ref scanner, result, out _);
                    if (!Tokens.Char('>', ref scanner, advance: true))
                        return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0034, scanner[scanner.Position], scanner.Memory));
                    parsed.Generics = generics;
                    Parsers.Spaces0(ref scanner, result, out _);
                }
                if (Tokens.Char(':', ref scanner, advance: true))
                {
                    Parsers.Spaces0(ref scanner, result, out _);
                    while (LiteralsParser.IdentifierBase(ref scanner, result, out var mixin))
                    {
                        parsed.Mixins.Add(mixin);
                        Parsers.Spaces0(ref scanner, result, out _);
                        if (Tokens.Char(',', ref scanner, advance: true))
                            Parsers.Spaces0(ref scanner, result, out _);
                        else
                            break;
                    }
                    if (parsed.Mixins.Count == 0)
                        return Parsers.Exit(ref scanner, result, out parsed, position, new("Expecting at least one mixin", scanner[scanner.Position], scanner.Memory));
                    Parsers.Spaces0(ref scanner, result, out _);
                }
                if (Tokens.Char('{', ref scanner, advance: true)
                    && Parsers.Spaces0(ref scanner, result, out _)
                )
                {
                    while (!scanner.IsEof && !Tokens.Char('}', ref scanner, advance: true))
                    {
                        if (ShaderElementParsers.ShaderElement(ref scanner, result, out var e))
                        {
                            parsed.Elements.Add(e);
                        }
                        else
                            break;
                        Parsers.Spaces0(ref scanner, result, out _);
                    }
                    Parsers.FollowedBy(ref scanner, Tokens.Char(';'), withSpaces: true, advance: true);
                    parsed.Info = scanner[position..scanner.Position];
                    return true;
                }
                else return Parsers.Exit(ref scanner, result, out parsed, position, new("Expecting shader body", scanner[position], scanner.Memory));

            }
        }
        return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct GenericIdentifierParser : IParser<GenericIdentifier>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out GenericIdentifier parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (LiteralsParser.Identifier(ref scanner, result, out var typename)
            && Parsers.Spaces0(ref scanner, result, out _))
        {
            var tmpPos = scanner.Position;
            Parsers.Spaces0(ref scanner, result, out _);
            if (
                Tokens.Char('<', ref scanner, advance: true)
                && Parsers.Spaces0(ref scanner, result, out _)
            )
            {
                ParameterParsers.GenericsList(ref scanner, result, out var values);
                Parsers.Spaces0(ref scanner, result, out _);
                if (!Tokens.Char('>', ref scanner, advance: true))
                    return Parsers.Exit(ref scanner, result, out parsed, position);
                parsed = new GenericIdentifier(typename, values, scanner[position..scanner.Position]);
                return true;
            }
            scanner.Position = tmpPos;
        }
        return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}


public record struct ShaderGenericsDefinitionParser : IParser<ShaderGenerics>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderGenerics parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            LiteralsParser.Identifier(ref scanner, result, out var typename)
            && Parsers.Spaces1(ref scanner, result, out _, new(SDSLErrorMessages.SDSL0016, scanner[scanner.Position], scanner.Memory))
            && LiteralsParser.Identifier(ref scanner, result, out var identifier)
        )
        {
            parsed = new ShaderGenerics(typename, identifier, scanner[position..scanner.Position]);
            return true;
        }
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}