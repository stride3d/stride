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
    public static bool GenericsDefinition<TScanner>(ref TScanner scanner, ParseResult result, out ShaderGenerics parsed)
        where TScanner : struct, IScanner
        => new ShaderGenericsDefinitionParser().Match(ref scanner, result, out parsed);
    public static bool Mixin<TScanner>(ref TScanner scanner, ParseResult result, out Mixin parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
        => new ShaderMixinParser().Match(ref scanner, result, out parsed);
}

public record struct SimpleShaderClassParser : IParser<ShaderClass>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderClass parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;

        if (
            Terminals.Literal("shader", ref scanner, advance: true)
            && CommonParsers.Spaces1(ref scanner, result, out _, new(SDSLParsingMessages.SDSL0016, scanner.GetErrorLocation(scanner.Position), scanner.Memory))
            && LiteralsParser.Identifier(ref scanner, result, out var className, new(SDSLParsingMessages.SDSL0017, scanner.GetErrorLocation(scanner.Position), scanner.Memory))
            && CommonParsers.Spaces0(ref scanner, result, out _)
            && Terminals.Char('{', ref scanner, advance: true)
            && CommonParsers.Spaces0(ref scanner, result, out _)

        )
        {
            var c = new ShaderClass(className, scanner.GetLocation(position, scanner.Position - position));
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

public record struct ShaderClassParser : IParser<ShaderClass>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out ShaderClass parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        var tmp = position;
        if (Terminals.Literal("internal", ref scanner, advance: true) && CommonParsers.Spaces1(ref scanner, result, out _))
            tmp = scanner.Position;
        if (
            (
                Terminals.Literal("shader", ref scanner, advance: true) 
                || Terminals.Literal("class", ref scanner, advance: true) 
            )
            && CommonParsers.Spaces1(ref scanner, result,out _))
        {
            if (
                LiteralsParser.Identifier(ref scanner, result, out var identifier, new(SDSLParsingMessages.SDSL0017, scanner.GetErrorLocation(scanner.Position), scanner.Memory))
                && CommonParsers.Spaces0(ref scanner, result, out _)
            )
            {
                parsed = new ShaderClass(identifier, scanner.GetLocation(..));
                if (Terminals.Char('<', ref scanner, advance: true))
                {
                    ParameterParsers.Declarations(ref scanner, result, out var generics);
                    CommonParsers.Spaces0(ref scanner, result, out _);
                    if (!Terminals.Char('>', ref scanner, advance: true))
                        return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0034, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
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
                        return CommonParsers.Exit(ref scanner, result, out parsed, position, new("Expecting at least one mixin", scanner.GetErrorLocation(scanner.Position), scanner.Memory));
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
                else return CommonParsers.Exit(ref scanner, result, out parsed, position, new("Expecting shader body", scanner.GetErrorLocation(position), scanner.Memory));

            }
        }
        return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}


public record struct ShaderMixinParser : IParser<Mixin>
{
    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Mixin parsed, in ParseError? orError = null) where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (LiteralsParser.Identifier(ref scanner, result, out var identifier))
        {
            parsed = new Mixin(identifier, scanner.GetLocation(..));
            var tmpPos = scanner.Position;
            CommonParsers.Spaces0(ref scanner, result, out _);
            if (Terminals.Char('<', ref scanner, advance: true))
            {
                ParameterParsers.GenericsList(ref scanner, result, out var values, new("Expecting constant generics", scanner.GetErrorLocation(position), scanner.Memory));
                parsed.Generics = values;
                CommonParsers.Spaces0(ref scanner, result, out _);
                if (!Terminals.Char('>', ref scanner, advance: true))
                    return CommonParsers.Exit(ref scanner, result, out parsed, position, new(SDSLParsingMessages.SDSL0034, scanner.GetErrorLocation(scanner.Position), scanner.Memory));
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
            && CommonParsers.Spaces1(ref scanner, result, out _, new(SDSLParsingMessages.SDSL0016, scanner.GetErrorLocation(scanner.Position), scanner.Memory))
            && LiteralsParser.Identifier(ref scanner, result, out var identifier)
        )
        {
            parsed = new ShaderGenerics(typename, identifier, scanner.GetLocation(position, scanner.Position - position));
            return true;
        }
        else return CommonParsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}