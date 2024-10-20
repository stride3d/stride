using System.Runtime.CompilerServices;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL;


public record struct ShaderClassParsers : IParser<ShaderMixin>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderMixin parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        if (ComplexClass(ref scanner, result, out parsed, in orError))
            return true;
        else
            return false;
    }
    public static bool Class<TScanner>(ref TScanner scanner, ParseResult result, out ShaderMixin parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new ShaderClassParsers().Match(ref scanner, result, out parsed, in orError);
    public static bool ComplexClass<TScanner>(ref TScanner scanner, ParseResult result, out ShaderMixin parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new ShaderClassParser().Match(ref scanner, result, out parsed, in orError);
    public static bool GenericsDefinition<TScanner>(ref TScanner scanner, ParseResult result, out ShaderGenerics parsed)
        where TScanner : struct, IScanner
        => new ShaderGenericsDefinitionParser().Match(ref scanner, result, out parsed);
    public static bool Mixin<TScanner>(ref TScanner scanner, ParseResult result, out InheritedMixin parsed)
        where TScanner : struct, IScanner
        => new ShaderMixinParser().Match(ref scanner, result, out parsed);
}

public record struct SimpleShaderClassParser : IParser<ShaderMixin>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderMixin parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;

        if (
            Terminals.Literal("shader", ref scanner, advance: true)
            && CommonParsers.Spaces1(ref scanner, result, out _, new("Expected at least one space", scanner.GetErrorLocation(scanner.Position)))
            && LiteralsParser.Identifier(ref scanner, result, out var className, new("Expected class name", scanner.GetErrorLocation(scanner.Position)))
            && CommonParsers.Spaces0(ref scanner, result, out _)
            && Terminals.Char('{', ref scanner, advance: true)
            && CommonParsers.Spaces0(ref scanner, result, out _)

        )
        {
            var c = new ShaderMixin(className, scanner.GetLocation(position, scanner.Position - position));
            while (!scanner.IsEof && !Terminals.Char('}', ref scanner, advance: true))
            {
                if (ShaderElementParsers.ShaderElement(ref scanner, result, out var e))
                {
                    c.Elements.Add(e);
                }
                else
                    break;
                CommonParsers.Spaces0(ref scanner, result, out _);
            }
            parsed = c;
            return true;
        }
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}

public record struct ShaderClassParser : IParser<ShaderMixin>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderMixin parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (Terminals.Literal("shader ", ref scanner, advance: true))
        {
            if (
                LiteralsParser.Identifier(ref scanner, result, out var identifier, new("Expected identifier here", scanner.GetErrorLocation(scanner.Position)))
                && CommonParsers.Spaces0(ref scanner, result, out _)
            )
            {
                parsed = new ShaderMixin(identifier, scanner.GetLocation(..));
                if (Terminals.Char('<', ref scanner, advance: true))
                {
                    ParameterParsers.Declarations(ref scanner, result, out var generics);
                    CommonParsers.Spaces0(ref scanner, result, out _);
                    if (!Terminals.Char('>', ref scanner, advance: true))
                        return CommonParsers.Exit(ref scanner, result, out parsed, position, new("Expected closing chevron", scanner.GetErrorLocation(scanner.Position)));
                    parsed.Generics = generics;
                    CommonParsers.Spaces0(ref scanner, result, out _);
                }
                if (Terminals.Char(':', ref scanner, advance: true))
                {
                    CommonParsers.Spaces0(ref scanner, result, out _);
                    while (ShaderClassParsers.Mixin(ref scanner, result, out var mixin))
                    {
                        parsed.Mixins.Add(mixin);
                        CommonParsers.Spaces0(ref scanner, result, out _);
                        if (Terminals.Char(',', ref scanner, advance: true))
                            CommonParsers.Spaces0(ref scanner, result, out _);
                        else
                            break;
                    }
                    if (parsed.Mixins.Count == 0)
                        return CommonParsers.Exit(ref scanner, result, out parsed, position, new("Expecting at least one mixin", scanner.GetErrorLocation(scanner.Position)));
                    CommonParsers.Spaces0(ref scanner, result, out _);
                }
                if (Terminals.Char('{', ref scanner, advance: true)
                    && CommonParsers.Spaces0(ref scanner, result, out _)
                )
                {
                    while (!scanner.IsEof && !Terminals.Char('}', ref scanner, advance: true))
                    {
                        if (ShaderElementParsers.ShaderElement(ref scanner, result, out var e))
                        {
                            parsed.Elements.Add(e);
                        }
                        else
                            break;
                        CommonParsers.Spaces0(ref scanner, result, out _);
                    }
                    CommonParsers.FollowedBy(ref scanner, Terminals.Char(';'), withSpaces: true, advance: true);
                    parsed.Info = scanner.GetLocation(position..scanner.Position);
                    return true;
                }
                else return CommonParsers.Exit(ref scanner, result, out parsed, position, new("Expecting shader body", scanner.GetErrorLocation(position)));

            }
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}


public record struct ShaderMixinParser : IParser<InheritedMixin>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out InheritedMixin parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (LiteralsParser.Identifier(ref scanner, result, out var identifier))
        {
            parsed = new InheritedMixin(identifier, scanner.GetLocation(..));
            var tmpPos = scanner.Position;
            CommonParsers.Spaces0(ref scanner, result, out _);
            if (Terminals.Char('<', ref scanner, advance: true))
            {
                ParameterParsers.GenericsList(ref scanner, result, out var values, new("Expecting constant generics", scanner.GetErrorLocation(position)));
                parsed.Generics = values;
                CommonParsers.Spaces0(ref scanner, result, out _);
                if (!Terminals.Char('>', ref scanner, advance: true))
                    return CommonParsers.Exit(ref scanner, result, out parsed, position, new("Expected closing chevron", scanner.GetErrorLocation(scanner.Position)));
                return true;
            }
            else
            {
                scanner.Position = tmpPos;
                return true;
            }
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
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
            && CommonParsers.Spaces1(ref scanner, result, out _, new("Expected at least one space", scanner.GetErrorLocation(scanner.Position)))
            && LiteralsParser.Identifier(ref scanner, result, out var identifier)
        )
        {
            parsed = new ShaderGenerics(typename, identifier, scanner.GetLocation(position, scanner.Position - position));
            return true;
        }
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}